
var voucherUser = {
    loadData: function () {
        $.ajax({
            url: "/UserVoucher/ListData",
            type: "get",
            success: function (result) {
                $("#voucherList").html(result);
            },
            error: function () {
                alert("Lỗi tải dữ liệu");
            }
        });
    },
    getVoucherByUser: function (voucherId) {
        Swal.fire({
            title: 'Xác nhận',
            text: "Bạn có chắc muốn lấy voucher này không?",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Có, lấy voucher',
            cancelButtonText: 'Hủy'
        }).then((result) => {
            if (result.isConfirmed) {
                // Nếu người dùng xác nhận
                $.ajax({
                    url: "/UserVoucher/userVoucher",
                    type: "POST",
                    data: { voucherId: voucherId },
                    success: function (res) {
                        Swal.fire(
                            'Thành công!',
                            res.message,
                            'success'
                        );
                        voucherUser.loadData(); // Tải lại dữ liệu sau khi lấy voucher
                    },
                    error: function () {
                        Swal.fire(
                            'Lỗi!',
                            'Đã có lỗi xảy ra khi lấy voucher',
                            'error'
                        );
                    }
                });
            }
        });
    },
    openDetailModal: function (voucherId) {
        debugger;
        $.ajax({
            url: '/UserVoucher/Detail',
            type: 'GET',
            data: { voucherId: voucherId },
            success: function (result) {
                // Gắn HTML trả về vào placeholder modal
                $('#modal-placeholder-voucher').html(result);
                $('#voucherUserDetailModal').modal('show');
            },
            error: function () {
                Swal.fire('Lỗi', 'Không tải được chi tiết voucher', 'error');
            }
        });
    },
    changeVoucherPoint: function (voucherId) {
        Swal.fire({
            title: 'Xác nhận',
            text: "Bạn có chắc muốn lấy voucher này không?",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Có, lấy voucher',
            cancelButtonText: 'Hủy'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: '/UserVoucher/ExchangePointsForVoucher',
                    type: 'POST',
                    data: { voucherId: voucherId },
                    success: function (res) {
                        Swal.fire(
                            'Thành công!',
                            res.message,
                            'success'
                        );
                        voucherUser.loadData();
                    },
                    error: function () {
                        Swal.fire('Lỗi', 'Không tải được chi tiết voucher', 'error');
                    }
                });
            }
        });
    },
    useVoucher: function (VoucherId) {
        debugger;
        $.ajax({
            url: "/UserVoucher/UseVoucher",
            type: "POST",
            data: { VoucherId: VoucherId },
            success: function (result) {
                if (result.success) {
                    console.log(result);
                    toastr.success(result.message);
                    const discount = result.discountAmount || 0;
                    const final = result.finalPrice;
                    $('#discountAmountDisplay').text(result.discountAmount.toLocaleString('vi-VN') + '₫');
                    $('#finalPriceDisplay').text(result.finalPrice.toLocaleString('vi-VN') + '₫');
                    // Cập nhật hidden input
                    $('#DiscountValue').val(discount);
                    $('#FinalAmount').val(final);
                    $('#VoucherId').val(VoucherId);
                    $('#discountAmountDiv').removeClass('d-none');
                    $('#finalPriceDiv').removeClass('d-none');
                } else {
                    toastr.warning(result.message);
                }
            },
            error: function (xhr, status, error) {
                toastr.error("Có lỗi xảy ra, vui lòng thử lại.");
                console.error(error);
            }
        });
    }

}
voucherUser.loadData(); // Tải lại trang đầu tiên
$('#SelectedVoucherId').on('change', function () {
    debugger;
    var voucherId = $(this).val();
    if (voucherId === "" || voucherId === null) {
        // Không chọn voucher → ẩn div
        $('#discountAmountDiv').addClass('d-none');
        $('#finalPriceDiv').addClass('d-none');        return;
    }
    voucherUser.useVoucher(voucherId);
});