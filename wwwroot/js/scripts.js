var currentPage = 1;
var pageSize = 6;
var orderUser = {
    getUnreadCount: function () {
        $.ajax({
            url: "/Order/getUnreadCount",
            type: "get",
            success: function (result) {
                const badge = document.getElementById('notifCount');
                if (badge) {
                    const count = result.count || 0;
                    badge.innerText = count > 0 ? count : '';
                    badge.style.display = count > 0 ? 'inline-block' : 'none';
                }
            },
            error: function () {
                alert("Lỗi tải dữ liệu");
            }
        });
    },
    loadNotif: function (page = 1, pageSize = 6) {
        $.ajax({
            url: '/Order/loadNotification',
            type: 'POST',
            data: { page, pageSize },
            success: function (result) {
                const list = document.getElementById('notifList');

                // Lần đầu mở modal => clear list
                if (page === 1) list.innerHTML = "";
                result.forEach(n => {
                    const li = document.createElement('li');
                    li.className = 'list-group-item p-0';
                    li.id = `notif-${n.id}`;
                    li.innerHTML = `
                        <a href="${n.url}" onclick="orderUser.readNotif(${n.id})"
                           class="notif-btn w-100 text-start text-decoration-none d-block p-2">
                            <div class="notif-info d-flex align-items-start">
                                <img src="${n.image || '/images/default.png'}" 
                                     width="50" height="50" class="me-2 rounded" />
                                <div>
                                    <strong>Đơn hàng #${n.orderId}</strong><br />
                                    <small class="text-muted">${n.createdAt}</small>
                                    <p class="mb-0">${n.message}</p>
                                </div>
                                ${!n.isRead ? '<span class="notif-dot ms-auto"></span>' : ''}
                            </div>
                        </a>
                    `;
                    list.appendChild(li);
                });

                // Ẩn nút nếu hết
                const loadMoreContainer = document.getElementById('loadMoreContainer');
                if (result.length < pageSize) {
                    loadMoreContainer.style.display = 'none';
                } else {
                    loadMoreContainer.style.display = 'block';
                }
            },
            error: function () {
                toastr.error('Không thể tải thông báo');
            }
        });
    },
    readNotif: function (notifId) {
        $.ajax({
            url: '/Order/ReadNotif',
            type: 'GET',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            data: { notifId: notifId },
            success: function (res) {
                if (res.success) {
                    const dot = document.querySelector(`#notif-${notifId} .notif-dot`);
                    if (dot) dot.remove();

                    const badge = document.getElementById('notifCount');
                    if (badge && res.unreadCount !== undefined) {
                        badge.textContent = res.unreadCount;
                        badge.style.display = res.unreadCount > 0 ? 'inline-block' : 'none';
                    }
                }
            },
            error: function () {
                //// Lỗi vẫn redirect
                //window.location.href = url;
            }
        });
    },
    loadMore: function () {
        this.currentPage++;
        this.loadNotif(this.currentPage, this.pageSize);
    }
}
const orderConnectionUser = new signalR.HubConnectionBuilder()
    .withUrl("/orderHub")
    .build();
orderConnectionUser.on("ReceiveOrderNotificationUser", function (notif) {
    console.log("Thông báo mới:", notif);
    const notification = document.getElementById('notification');
    const notifBell = document.getElementById('notifBell');
    if (!notification || !notifBell) return;
    // Cập nhật nội dung thông báo
    notification.innerHTML = `
        <div class="notif-content d-flex align-items-start border-bottom pb-2 mb-2">
            <img src="${notif.image || '/images/default.png'}" width="40" class="me-2 rounded-circle"/>
            <div class="notif-text flex-grow-1">
                <div class="d-flex justify-content-between">
                    <strong>${notif.title}</strong>
                    <small class="text-muted">${notif.createdAt}</small>
                </div>
                <p class="mb-1 small">${notif.message}</p>
                <button id="notif-btn-${notif.id}" class="btn btn-sm btn-primary py-0 px-2">Xem</button>
            </div>
        </div>
    `;
    notification.classList.remove('hidden');
    // Cập nhật badge
    const badge = notifBell.querySelector('.badge');
    if (badge) {
        let count = parseInt(badge.innerText) || 0;
        badge.innerText = count + 1;
    }

    // Tính vị trí popup ngay dưới nút chuông
    const rect = notifBell.getBoundingClientRect();
    notification.style.top = rect.bottom + window.scrollY + 10 + "px";
    notification.style.left = rect.right - 320 + "px"; // canh phải 320px (độ rộng popup)
    notification.style.display = "block";

    // Tự ẩn sau 6 giây
    setTimeout(() => {
        notification.style.display = "none";
    }, 6000);

    // Gắn nút xem chi tiết
    document.getElementById(`notif-btn-${notif.id}`)
        .addEventListener("click", () => admin.detail(notif.orderId, notif.id));
});

orderConnectionUser.start().then(() => console.log("✅ Kết nối SignalR thành công"))
    .catch(err => console.error("❌ Kết nối lỗi:", err));
$('#orderNotificationModal').on('shown.bs.modal', function () {
    orderUser.currentPage = 1;
    orderUser.loadNotif(1, orderUser.pageSize);
});
orderUser.getUnreadCount();