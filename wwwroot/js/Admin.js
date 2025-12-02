let notifPage = 1;
let notifPageSize = 7;
var admin = {
    Notification: function () {
        $.ajax({
            url: '/Admin/OrderNotificationModal',
            type: 'GET',
            success: function (result) {
                $('#modal-placeholder-notif').html(result);
                $('#orderNotificationModal').modal('show');
                admin.loadNotifications();
            },
            error: function () {
                toastr.error('Không thể tải thông báo');
            }
        });
    },
    loadNotifications: function () {
        notifPage = 1;
        $.get('/Admin/GetNotifications', { page: notifPage, pageSize: notifPageSize },
            function (html) { 
            $('#notifList').html(html);
        });
    },
    loadNotifications: function () {
        notifPage = 1;
        $.get('/Admin/GetNotifications', { page: notifPage, pageSize: notifPageSize },
            function (html) {
            $('#notifList').html(html);
        });
    },
    loadNotifCount: function () {
        $.get('/Admin/GetUnreadCount', function (res) {
            if (res.success) {
                $('#notifCount').text(res.count);
            }
        }).fail(function () {
            toastr.error("Không thể tải số thông báo chưa đọc");
        });
    },

    detail: function (orderId, id) {
        debugger;
        $.ajax({
            url: '/Order/Detail',
            type: 'GET',
            data: { id: orderId },
            success: function (result) {
                $('#orderNotificationModal').modal('hide');
                $('#modal-placeholder').html(result);
                $('#orderDetailModal').modal('show');
                admin.readNotif(id);
            },
            error: function () {
                toastr.error("Lỗi tải chi tiết đơn hàng");
            }
        });
    },

    readNotif: function (id) {
        $.ajax({
            url: '/Order/ReadNotif',
            type: 'POST',
            data: { id: id },
            success: function (res) {
                if (res.success && !res.alreadyRead) {
                    $('#notif-dot-' + id).remove();
                    let countElem = $('#notifCount');
                    let count = parseInt(countElem.text());
                    if (!isNaN(count) && count > 0) {
                        countElem.text(count - 1);
                    }
                }
            },
            error: function () {
                toastr.error('Không thể cập nhật trạng thái thông báo');
            }
        });
    },
    loadMore: function () {
        notifPage++;
        $.ajax({
            url: '/Admin/GetNotifications',
            type: 'GET',
            data: { page: notifPage, pageSize: notifPageSize },
            success: function (html) {
                const newItems = $(html).filter('li');
                const count = newItems.length;
                if (count > 0) {
                    // Thêm các item mới vào trước nút Xem thêm
                    $('#notifList li:last').before(newItems);

                    // Nếu ít hơn pageSize => đã hết data -> ẩn nút
                    if (count < notifPageSize) {
                        $('#loadMoreBtn').hide();
                    }
                } else {
                    // Không có dữ liệu mới => ẩn nút luôn
                    $('#loadMoreBtn').hide();
                }
            },
            error: function () {
                toastr.error("Lỗi khi tải thêm thông báo.");
            }
        });
    }

    
};

// Hàm chạy animation số
function animateValue(id, start, end, duration, isCurrency = false) {
    let obj = document.getElementById(id);
    if (!obj) return;

    let current = start;
    let steps = 100;
    let increment = (end - start) / steps;
    let stepTime = duration / steps;

    let timer = setInterval(function () {
        current += increment;
        if ((increment > 0 && current >= end) || (increment < 0 && current <= end)) {
            current = end;
            clearInterval(timer);
        }

        obj.innerText = isCurrency
            ? Math.round(current).toLocaleString('vi-VN')
            : Math.floor(current).toLocaleString('vi-VN');
    }, stepTime);
}

// SignalR kết nối
const orderConnection = new signalR.HubConnectionBuilder()
    .withUrl("/orderHub")
    .build();

orderConnection.on("ReceiveOrderNotification", (orderId, time, unreadCount, notifiId, totalPrice, totalProduct,title,message) => {
    console.log("Nhận sự kiện:", orderId, time, unreadCount, notifiId, totalPrice, totalProduct);

    let notifCount = document.getElementById("notifCount");
    if (notifCount) notifCount.innerText = unreadCount;

    let revenueElem = document.getElementById('totalRevenue');
    let ordersElem = document.getElementById('totalOrders');
    let productsElem = document.getElementById('totalProductsSold');

    if (revenueElem) {
        let currentRevenue = parseInt(revenueElem.innerText.replace(/\./g, '')) || 0;
        let newRevenue = currentRevenue + totalPrice;
        animateValue('totalRevenue', currentRevenue, newRevenue, 3000, true);
    }

    if (ordersElem) {
        let currentOrders = parseInt(ordersElem.innerText.replace(/\./g, '')) || 0;
        animateValue('totalOrders', currentOrders, currentOrders + 1, 1500);
    }

    if (productsElem) {
        let currentProducts = parseInt(productsElem.innerText.replace(/\./g, '')) || 0;
        animateValue('totalProductsSold', currentProducts, currentProducts + totalProduct, 1500);
    }

    const notification = document.getElementById('notification');
    if (notification) {
        notification.innerHTML = `
        <div class="notif-content d-flex align-items-start border-bottom pb-2 mb-2">
            <div class="notif-text flex-grow-1">
                <div class="d-flex justify-content-between align-items-center">
                    <strong>${title}</strong>
                    <small class="text-muted">${time}</small>
                </div>
                <p class="mb-1">${message}</p>
                <button id="notif-btn-${notifiId}" class="btn btn-sm btn-primary py-0 px-2">Xem</button>
            </div>
        </div>
    `;
        notification.classList.remove('hidden');

        // Gắn sự kiện click cho nút vừa tạo
        document.getElementById(`notif-btn-${notifiId}`)
            .addEventListener("click", () => admin.detail(orderId, notifiId));

        setTimeout(() => {
            notification.classList.add('hidden');
        }, 10000);
    }

});

orderConnection.start()
    .then(() => console.log('Connected to OrderHub'))
    .catch(err => console.error('SignalR connection error:', err));

// Load số notif khi mở dashboard
$(function () {
    admin.loadNotifCount();
});