
var currentPageUser = 1;
var currentCheckUser = true;
var user = {
    loadData: function (pageIndex,check) {
        debugger;
        if (typeof showGlobalLoading === "function") {
            showGlobalLoading(true);
        }
        var data = $('#searchForm').serializeArray();
        if (pageIndex !== undefined) {
            // dùng pageIndex truyền vào
        } else {
            pageIndex = 1;
        }
        if (check === undefined) {
            check = true;
        }
        currentCheckUser = check; // <--- thêm dòng này
        data.push({ name: "check", value: check });
        // Thêm pageIndex vào data (với tên "page" trùng tham số controller)
        data.push({ name: "page", value: pageIndex });
        currentPageUser = pageIndex;
        $.ajax({
            url: "/User/ListData",
            type: "get",
            data: data,
            success: function (result) {
                setTimeout(function () {
                    $("#userList").html(result);
                    var totalPages = $("#pagination").data("total-pages");
                    if (!$('#paging-ul').data("twbs-pagination")) {
                        user.showPaging(totalPages, pageIndex);
                    }
                    showGlobalLoading(false);// Gắn HTML trả về vào div
                    if (typeof showGlobalLoading === "function") {
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
    openDetailModal: function (userId) {
        debugger;
        $.ajax({
            url: '/User/Detail',
            type: 'GET',
            data: { userId: userId },
            success: function (result) {
                $('#modal-placeholder-user').html(result);
                $('#userDetailModal').modal('show');
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    changeStatus: function (userId) {
        $.ajax({
            url: "/User/ChangeStatus",
            type: "GET",
            data: { userId: userId },
            success: function (res) {
                toastr.success(res.message);
                user.loadData(currentPageUser, currentCheckUser);
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });
    },
    deleteSoft: function (userId) {
        Swal.fire({
            title: "Xác nhận xóa người dùng?",
            text: "Người dùng này sẽ bị vô hiệu hóa (xóa mềm) khỏi hệ thống.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Xóa",
            cancelButtonText: "Hủy"
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: "/User/DeleteUser",
                    type: "Post", // hoặc POST nếu bạn đổi bên controller
                    data: { userId: userId },
                    success: function (res) {
                        if (res.success) {
                            toastr.success(res.message);
                            user.loadData(currentPageUser);
                        } else {
                            toastr.error(res.message);
                        }
                    },
                    error: function () {
                        toastr.error("Lỗi khi xóa người dùng.");
                    }
                });
            }
        });
    },
    restoreUser: function (userId) {
        Swal.fire({
            title: "Xác nhận phục hồi người dùng?",
            text: "Tài khoản này sẽ được khôi phục và kích hoạt lại trong hệ thống.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#28a745", // xanh lá - màu phục hồi
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Phục hồi",
            cancelButtonText: "Hủy"
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: "/User/RestoreUser",
                    type: "GET", // hoặc "POST" nếu controller dùng POST
                    data: { userId: userId },
                    success: function (res) {
                        if (res.success) {
                            toastr.success(res.message);
                            user.loadData(currentPageUser, currentCheckUser);
                        } else {
                            toastr.error(res.message);
                        }
                    },
                    error: function () {
                        toastr.error("Đã xảy ra lỗi khi phục hồi người dùng.");
                    }
                });
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
                        user.loadData(page);
                    }
                }
            });
            $('#paging-ul').data('init-complete', true); // đánh dấu đã init
        }
    },
}
user.loadData();