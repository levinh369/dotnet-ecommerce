let notifPageOrderOrderOrderOrder = 1;
var pageSize = 6;
var currentPage = 1;
var order = {
    loadData: function (pageIndex) {
        if (typeof showGlobalLoading === "function") {
            showGlobalLoading(true);
        }
        var data = $('#filterForm').serializeArray();
        if (pageIndex !== undefined) {
            // dùng pageIndex truyền vào
        } else {
            pageIndex = 1;
        }
        data.push({ name: "page", value: pageIndex });
        currentPage = pageIndex;
        $.ajax({
            url: "/Order/ListData",
            type: "get",
            data: data,
            success: function (result) {
                setTimeout(function () {
                $("#gridData").html(result);
                var totalPages = $("#pagination").data("total-pages");
                if (totalPages > 1) {
                    order.showPaging(totalPages, pageIndex);
                    } if (typeof showGlobalLoading === "function") {
                        showGlobalLoading(false);
                    }
                }, 1000); // delay 1000ms
            },
            error: function () {
                alert("Lỗi tải dữ liệu");
                if (typeof showGlobalLoading === "function") {
                    showGlobalLoading(false);
                }
            }
        });
    },
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

    showPaging: function (totalPages, currentPage) {
        if (totalPages > 1) {
            $('#paging-ul').twbsPagination({
                startPage: currentPage,
                totalPages: totalPages,
                visiblePages: 5,
                first: '<i class="fa fa-fast-backward"></i>',
                prev: '<i class="fa fa-step-backward"></i>',
                next: '<i class="fa fa-step-forward"></i>',
                last: '<i class="fa fa-fast-forward"></i>',
                onPageClick: function (event, page) {
                    // chỉ load khi KHÔNG phải lần khởi tạo
                    if ($('#paging-ul').data('init-complete')) {
                        order.loadData(page);
                    }
                }
            });
            $('#paging-ul').data('init-complete', true); // đánh dấu đã init
        }
    },
    getOrderStatus: function (status) {
        $.ajax({
            url: "/Order/GetOrder",
            type: "post",
            data: {
                status: status
            },
            success: function (res) {
                $(".load-orders").removeClass("active");
                $(this).addClass("active");
                $("#order-container").html(res);
            },
            error: function () {
                alert("Lỗi tải dữ liệu");
            }
        });
    },
    changeStatus: function (orderId,value) {
        $.ajax({
            url: "/Order/Status",
            type: "get",
            data: {
                value: value,
                orderId: orderId
            },
            success: function (res) {
                toastr.success(res.message);
                order.loadData(currentPage);
            },
            error: function () {
                alert("Lỗi tải dữ liệu");
            }
        });
    },
    detail: function (id) {
        debugger;
        $.ajax({
            url: '/Order/Detail',
            type: 'GET',
            data: { id: id },
            success: function (result) {
                $('#modal-placeholder').html(result);
                $('#orderDetailModal').modal('show');
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });

    },
    remove: function (id) {
        Swal.fire({
            title: "Bạn có chắc muốn xóa?",
            text: "Thao tác này sẽ không thể hoàn tác!",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#3085d6",
            confirmButtonText: "Xóa",
            cancelButtonText: "Hủy"
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: "/Order/Delete",
                    type: "GET",
                    data: { id: id },
                    success: function (res) {
                        toastr.success(res.message);
                        order.loadData(currentPage);
                    },
                    error: function () {
                        toastr.error("Lỗi tải trang");
                    }
                });
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
                        <a href="${n.url}" onclick="order.readNotif(${n.id})"
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
$('#filterForm').on('submit', function (e) {
    e.preventDefault();
    // ngăn form submit reload trang
    order.loadData(1);
});
order.loadData(currentPage);
order.getUnreadCount();

$('#orderNotificationModal').on('shown.bs.modal', function () {
    order.currentPage = 1;
    order.loadNotif(1, order.pageSize);
});


