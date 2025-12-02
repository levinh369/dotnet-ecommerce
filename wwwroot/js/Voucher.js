
var currentPageVoucher = 1;
var voucher = {
    loadData: function (pageIndex) {
        debugger;
        if (typeof showGlobalLoading === "function") {
            showGlobalLoading(true);
        }
        var data = $('#searchVoucherForm').serializeArray();
        if (pageIndex !== undefined) {
            // dùng pageIndex truyền vào
        } else {
            pageIndex = 1;
        }

        // Thêm pageIndex vào data (với tên "page" trùng tham số controller)
        data.push({ name: "page", value: pageIndex });
        currentPageVoucher = pageIndex;
        $.ajax({
            url: "/Voucher/ListData",
            type: "get",
            data: data,
            success: function (result) {
                setTimeout(function () {
                    $("#voucherList").html(result);
                    var totalPages = $("#pagination").data("total-pages");
                    if (!$('#paging-ul').data("twbs-pagination")) {
                        voucher.showPaging(totalPages, pageIndex);
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
    openCreateModal: function () {
        $.ajax({
            url: '/Voucher/Create',
            type: 'GET',
            success: function (result) {
                $('#modal-placeholder-voucher').html(result);
                $('#addVoucherModal').modal('show');
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    create: function () {
        debugger;
        var form = $('#addVoucherForm');
        // Kiểm tra form hợp lệ (nếu có jQuery Validate)
        var formData = form.serialize();
        $.ajax({
            url: '/Voucher/Create',
            type: 'POST',
            data: formData,
            headers: {
                "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() // Token chống giả mạo
            },
            success: function (res) {
                if (res.success) {
                    $('#addVoucherModal').modal('hide');
                    toastr.success(res.message);
                    voucher.loadData(currentPageVoucher);
                } else {
                    toastr.error(res.message);
                }
            },
            error: function () {
                showGlobalLoading(false);
                toastr.error("Lỗi tải trang");
            }
        });
    },
    openDetailModal: function (voucherId) {
        debugger;
        $.ajax({
            url: '/Voucher/Detail',
            type: 'GET',
            data: { voucherId: voucherId },
            success: function (result) {
                $('#modal-placeholder-voucher').html(result);
                $('#voucherDetailModal').modal('show');
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    openEditlModal: function (voucherId) {
        debugger;
        $.ajax({
            url: '/Voucher/Edit',
            type: 'GET',
            data: { voucherId: voucherId },
            success: function (result) {
                $('#modal-placeholder-voucher').html(result);
                $('#voucherEditModal').modal('show');
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    EditPost: function () {
        var data = $('#voucherEditForm').serialize();;
        $.ajax({
            url: "/Voucher/Edit",
            type: "POST",
            data: data,
            success: function (res) {
                $('#voucherEditModal').modal('hide');
                toastr.success(res.message);
                voucher.loadData(currentPageVoucher);
            },
            error: function (xhr, status, error) {
                toastr.error("Lỗi tải trang");
                console.error('Lỗi tải form:', error); // ✅ in ra lỗi từ server
                console.log('Chi tiết:', xhr.responseText);
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
                        voucher.loadData(page);
                    }
                }
            });
            $('#paging-ul').data('init-complete', true); // đánh dấu đã init
        }
    },
}
voucher.loadData();
$(document).on('submit', '#addVoucherForm', function (e) {
    e.preventDefault(); // ❗ Chặn submit mặc định
    voucher.create();   // Gọi hàm xử lý AJAX
});
$(document).on('change', '#Type', function () {
    const $pointCostContainer = $("#pointCostContainer");
    if ($(this).val() === "1") {
        $pointCostContainer.slideDown();
    } else {
        $pointCostContainer.slideUp();
        $pointCostContainer.find("input").val("");
    }
});

