var currentPage = 1;
var cart = {
    loadData: function (pageIndex) {
        if (pageIndex !== undefined) {
            // dùng pageIndex truyền vào
        } else {
            pageIndex = 1;
        }
        currentPage = pageIndex;
        $.ajax({
            url: "/Cart/ListData",
            type: "get",
            data: { page: pageIndex },
            success: function (result) {
                $("#gridData").html(result);
                var totalPages = $("#pagination").data("total-pages");
                if (totalPages > 1) {
                    cart.showPaging(totalPages, pageIndex);
                }
            },
            error: function () {
                alert("Lỗi tải dữ liệu");
            }
        });
    },
    addCart: function () {
        debugger;
        var form = $('#cartForm');
        if (!form.valid()) {
            return;
        } 
        var productVariantId = form.find('input[name="ProductVariantId"]').val();
        var quantity = form.find('input[name="Quantity"]').val();
        if (!productVariantId || !quantity || quantity <= 0) {
            toastr.error("Vui lòng chọn size, màu và số lượng hợp lệ");
            return;
        } 
        $.ajax({
            url: '/Cart/AddCart',
            type: 'Post',
            data: form.serialize(),
            success: function (result) {
                if (result.status) {
                    toastr.success(result.message);
                    cart.getCount();
                } else {
                    toastr.error(result.message);
                }
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    getCount: function () {
        $.ajax({
            url: '/Cart/GetCartCount',
            type: 'Get',

            success: function (result) {
                $('#spanCount').text(result.count);

            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    copyQuantity: function (form) {
        form.querySelector('[name="Quantity"]').value =
            document.getElementById("quantityInput").value;
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
                        cart.loadData(page);
                    }
                }
            });
            $('#paging-ul').data('init-complete', true); // đánh dấu đã init
        }
    },
    changeQuantity: function (action, id) {
        
        $.ajax({
            url: '/Cart/changeQuantity',
            type: 'Get',
            data: { operation: action, productVariantId: id },
            success: function (res) {
                toastr.success(res.message);
                cart.loadData(currentPage);
            },
            error: function () {
                toastr.error(result.message);
            }
        })
    },
    delete: function (id) {
        console.log(id)
        $.ajax({
            url: '/Cart/deleteCart',
            type: 'Get',
            data: { productVariantId: id },
            success: function (res) {
                toastr.success(res.message);
                cart.loadData(currentPage);
            },
            error: function () {
                toastr.error(result.message);
            }
        })
    },


}
$(document).on('submit', '#cartForm', function (e) {
    e.preventDefault(); // Ngăn form submit mặc định
    cart.copyQuantity(this);
    cart.addCart(); // Gọi hàm ajax
});
$(function () {
    cart.loadData(1);
});