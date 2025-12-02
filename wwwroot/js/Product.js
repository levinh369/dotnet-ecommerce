
var currentPage = 1;
var product = {
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
        debugger;
        // Thêm pageIndex vào data (với tên "page" trùng tham số controller)
        data.push({ name: "page", value: pageIndex });
        currentPage = pageIndex;
        
        $.ajax({
            url: "/Product/ListData",
            type: "get",
            data: data,
            success: function (result) {
                // Giả lập delay 1 giây trước khi xử lý kết quả
                setTimeout(function () {
                    $("#gridData").html(result); // Gắn HTML trả về vào div
                    var totalPages = $("#pagination").data("total-pages");
                    if (!$('#paging-ul').data("twbs-pagination")) {
                        product.showPaging(totalPages, pageIndex);
                    }
                    if (typeof showGlobalLoading === "function") {
                        showGlobalLoading(false);
                    }
                }, 1000); // delay 1000ms
            },

            error: function () {
                alert("Lỗi tải dữ liệu");
            }
        });
    },
    Search: function () {
        debugger;
        var data = $('#priceFilterForm').serializeArray();

        let categoryId = $(".category-radio:checked").val();
        if (categoryId !== undefined) {
            data.push({ name: "categoryId", value: categoryId });
        }

        $.ajax({
            url: "/Product/SearchResultPartial",
            type: "get",
            data: data,
            success: function (result) {
                $("#product-list").html(result);
            },
            error: function () {
                alert("Lỗi tải dữ liệu");
            }
        });
    },
    deleteAll: function () {
        const selectedValues = [];
        document.querySelectorAll('.select-checkbox:checked').forEach(cb => {
            selectedValues.push(cb.value);
        });

        if (selectedValues.length === 0) {
            Swal.fire({
                icon: 'warning',
                title: 'Thông báo',
                text: "Bạn chưa chọn sản phẩm nào!"
            });
            return;
        }

        Swal.fire({
            title: 'Bạn có chắc?',
            text: "Các sản phẩm đã chọn sẽ bị xóa!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Có, xóa luôn!',
            cancelButtonText: 'Hủy'
        }).then((result) => {
            if (result.isConfirmed) {
                // Gửi mảng lên server
                $.ajax({
                    url: '/Product/deleteAll',
                    type: 'POST',
                    traditional: true,
                    data: { id: selectedValues },
                    success: function (res) {
                        if (res.status) {
                            Swal.fire(
                                'Đã xóa!',
                                res.message,
                                'success'
                            );
                            product.loadData(currentPage); // tải lại trang hiện tại
                        } else {
                            Swal.fire(
                                'Lỗi!',
                                res.message,
                                'error'
                            );
                        }
                    },
                    error: function () {
                        Swal.fire(
                            'Lỗi!',
                            'Không thể xóa sản phẩm',
                            'error'
                        );
                    }
                });
            }
        });
    },


    openCreateModal: function () {
        $.ajax({
            url: '/Product/Create',
            type: 'GET',
            success: function (result) {
                $('#modal-placeholder').html(result);
                $('#addProductModal').modal('show');
                product.bindCategoryEvents();
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    getQuantity: function (id) {
        const size = $('#selectSize').val();
        const color = $('input[name="productColor"]:checked').val();
        
        $.ajax({
            url: '/Product/getQuantity',
            type: 'GET',
            data: {
                id: id,
                sizeId: size,
                colorId: color
            },
            success: function (result) {
                var quantity = result.quantity; // luôn có giá trị int

                $('#spanQuantity').text(quantity + ' có sẵn');
                $('#main-img').attr('src', result.imageUrl);
                const $qtyInput = $('input[type="number"]');
                $qtyInput.attr('max', quantity);
                if (parseInt($qtyInput.val()) > quantity) {
                    $qtyInput.val(quantity);
                }
                $('input[name="ProductVariantId"]').val(result.productVariantId);
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },

    createProduct: function () {
        showGlobalLoading(true);
        var form = $('#createProductForm');
        if (!form.valid()) return;

        var formData = new FormData(form[0]);

        $.ajax({
            url: '/Product/Create',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,

            success: function (res) {
              
                if (res.success) {
                    $('#addProductModal').modal('hide');
                    toastr.success(res.message);
                    showGlobalLoading(false);
                    product.loadData(currentPage);
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
                        product.loadData(page);
                    }
                }
            });
            $('#paging-ul').data('init-complete', true); // đánh dấu đã init
        }
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
                    url: "/Product/Delete",
                    type: "GET",
                    data: { id: id },
                    success: function (res) {
                        toastr.success(res.message);
                        product.loadData(currentPage);
                    },
                    error: function () {
                        toastr.error("Lỗi tải trang");
                    }
                });
            }
        });
    },
    edit: function (id) {
        $.ajax({
            url: '/Product/Edit',
            type: 'GET',
            data: { id: id },
            success: function (result) {
                $('#modal-placeholder').html(result);
                $('#editProductModal').modal('show');
                product.bindCategoryEvents();
            },
            error: function (xhr, status, error) {
                $('#loadingOverlay').hide();
                console.error('Lỗi tải form:', error); // ✅ in ra lỗi từ server
                console.log('Chi tiết:', xhr.responseText); // ✅ in ra HTML hoặc message từ server
            }

        });

    },
    changeStatus: function (id) {
        $.ajax({
            url: "/Product/Status",
            type: "GET",
            data: { id: id },
            success: function (res) {
                toastr.success(res.message);
                product.loadData(currentPage);
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });
    },
    detail: function (id) {
        $.ajax({
            url: '/Product/Detail',
            type: 'GET',
            data: { id: id },
            success: function (result) {
                $('#modal-placeholder').html(result);
                $('#productDetailModal').modal('show');
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });

    },

    ProductDetail: function (id) {
        debugger;
        $('#loadingOverlay').addClass('d-flex').show();
        $.ajax({
            url: '/Product/ProductDetail',
            type: 'GET',
            data: { id: id },
            success: function (result) {
                $('#loadingOverlay').hide().removeClass('d-flex');
                
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });

    },
    EditPost: function () {
        var form = $('#editProductForm')[0]; // lấy element DOM thật
        var formData = new FormData(form);
        $('#loadingOverlay').addClass('d-flex').show();
        $.ajax({
            url: "/Product/EditPost",
            type: "POST",
            data: formData,
            processData: false, // ❌ không xử lý data thành query string
            contentType: false,
            success: function (res) {
                $('#loadingOverlay').hide().removeClass('d-flex');
                $('#editProductModal').modal('hide');
                toastr.success(res.message);
                product.loadData(currentPage);
            },
            error: function (xhr, status, error) {
                toastr.error("Lỗi tải trang");
                console.error('Lỗi tải form:', error); // ✅ in ra lỗi từ server
                console.log('Chi tiết:', xhr.responseText);
            }
        });
    },
    
    unbindCategoryEvents: function () {
        $(document).off("change", "#CategoryLevel1");
        $(document).off("change", "#CategoryLevel2");
    },
    bindCategoryEvents: function () {
        product.unbindCategoryEvents();
        // Load cấp 2 khi chọn cấp 1
        $(document).on("change", "#CategoryLevel1", function () {
            var parentId = $(this).val();
            // Reset CategoryLevel2 and CategoryLevel3
            $("#CategoryLevel2")
                .html('<option value="">-- Chọn cấp 2 --</option>')
                .prop("disabled", true);
            $("#CategoryLevel3")
                .html('<option value="">-- Chọn cấp 3 --</option>')
                .prop("disabled", true);
            $("#CategoryId").val(parentId || '');

            if (parentId) {
                $.getJSON('/Product/GetChildCategories', { parentId: parentId }, function (data) {
                    // Clear existing options except the default one (already cleared above)
                    
                    $.each(data, function (i, item) {
                        $("#CategoryLevel2").append($('<option>', {
                            value: item.categoryId,
                            text: item.categoryName
                        }));
                        
                    });
                    $("#CategoryLevel2").prop("disabled", false);
                });
                console.log($("#CategoryLevel2").html());
            }

        });

        $(document).on("change", "#CategoryLevel2", function () {
            var parentId = $(this).val();
            // Reset CategoryLevel3
            $("#CategoryLevel3")
                .html('<option value="">-- Chọn cấp 3 --</option>')
                .prop("disabled", true);
            $("#CategoryId").val(parentId || '');

            if (parentId) {
                $.getJSON('/Product/GetChildCategories', { parentId: parentId }, function (data) {

                    // Clear existing options except the default one (already cleared above)
                    $.each(data, function (i, item) {
                        $("#CategoryLevel3").append($('<option>', {
                            value: item.categoryId,
                            text: item.categoryName
                        }));
                    });
                    $("#CategoryLevel3").prop("disabled", false);
                });
            }
        });

        
    }

};
$(document).on("submit", "#priceFilterForm", function (e) {
    e.preventDefault();
    product.Search();
});

// khi đổi category
$(document).on("change", ".category-radio", function () {
    product.Search();
});
$(document).on('submit', '#createProductForm', function (e) {
    e.preventDefault(); // Ngăn form submit mặc định
    product.createProduct(); // Gọi hàm ajax
});

$(document).on("change", "#ParentLevel3", function () {
    var level3 = $(this).val();
    var level2 = $("#ParentLevel2").val();
    var level1 = $("#ParentLevel1").val();

    // Ưu tiên id cấp 3, nếu không có thì cấp 2, nếu vẫn không thì cấp 1
    $("#CategoryId").val(level3 || level2 || level1 || '');
});
$(document).on('submit', '#editProductModal', function (e) {
    e.preventDefault(); // Ngăn form submit mặc định
    product.EditPost(); // Gọi hàm ajax
});
$('#filterForm').on('submit', function (e) {
    e.preventDefault();  // ngăn form submit reload trang
    product.loadData(1);
});
// Khi click vào label (màu)
$(document).on('click', '.color-swatch', function (e) {
    // Bỏ viền cũ
    e.preventDefault();      // Ngăn hành động mặc định (click label = click radio)
    e.stopPropagation();
    $('.color-swatch').removeClass('border-primary border-3');

    // Thêm viền cho màu được chọn
    $(this).addClass('border-primary border-3');

    // Tick radio bên trong
    $(this).find('input[type="radio"]').prop('checked', true);

    const productId = $('#swatches').data('id'); // nếu gắn data-id ở ngoài
    product.getQuantity(productId);
    
});

$(function () {
    product.loadData(1);
});