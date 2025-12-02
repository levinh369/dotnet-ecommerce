
var currentPageVariant = 1;
var productVariant = {
    loadData: function () {
        debugger;
        const productId = $(".product-container").data("product-id");


        if (typeof showGlobalLoading === "function") {
            showGlobalLoading(true);
        }
        //var data = $('#filterForm').serializeArray();
        //if (pageIndex !== undefined) {
        //    // dùng pageIndex truyền vào
        //} else {
        //    pageIndex = 1;
        //}

        //// Thêm pageIndex vào data (với tên "page" trùng tham số controller)
        //data.push({ name: "page", value: pageIndex });
        //currentPage = pageIndex;

        $.ajax({
            url: "/ProductVariant/ListData",
            type: "get",
            data: { productId: productId },
            success: function (result) {
                setTimeout(function () {
                    $("#gridDataVariant").html(result); // Gắn HTML trả về vào div
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
    openCreateModal: function (id) {
        $.ajax({
            url: '/ProductVariant/Create',
            type: 'GET',
            data: { productId: id },
            success: function (result) {
                $('#modal-placeholder-variant').html(result);
                $('#addVariantModal').modal('show');
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    create: function () {
        var form = $('#addVariantForm');
        if (!form.valid()) return;
        var formData = new FormData(form[0]);
        $.ajax({
            url: '/ProductVariant/Create',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,

            success: function (res) {

                if (res.success) {
                    $('#addVariantModal').modal('hide');
                    toastr.success(res.message);
                    productVariant.loadData(currentPage);
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
    edit: function (id) {
        $.ajax({
            url: '/ProductVariant/Edit',
            type: 'GET',
            data: { productVariantId: id },
            success: function (result) {
                $('#modal-placeholder-variant').html(result);
                $('#editVariantModal').modal('show');
            },
            error: function (xhr, status, error) {
                console.error('Lỗi tải form:', error); // ✅ in ra lỗi từ server
                console.log('Chi tiết:', xhr.responseText); // ✅ in ra HTML hoặc message từ server
            }

        });
    },
    EditPost: function () {
        var data = $('#editVariantForm').serialize();
        $.ajax({
            url: "/ProductVariant/EditPost",
            type: "POST",
            data: data,
            
            success: function (res) {
                $('#editVariantModal').modal('hide');
                toastr.success(res.message);
                productVariant.loadData(currentPage);
            },
            error: function (xhr, status, error) {
                toastr.error("Lỗi tải trang");
                console.error('Lỗi tải form:', error); // ✅ in ra lỗi từ server
                console.log('Chi tiết:', xhr.responseText);
            }
        });
    },
    UploadImages: function (form) {
        var formData = new FormData(form);
        $.ajax({
            url: '/ProductVariant/UploadVariantImage', // đổi thành URL controller
            type: 'POST',
            data: formData,
            contentType: false, // quan trọng: để jQuery không tự set header
            processData: false, // quan trọng: để jQuery không convert formData sang string
            success: function (response) {
                toastr.success(response.message);
                productVariant.loadData(currentPage);
            },
            error: function (err) {
                toastr.error("Lỗi tải trang");
            }
        });
    },
};
$(function () {
    productVariant.loadData();
});
$(document).on('submit', '.uploadImageForm', function (e) {
    e.preventDefault(); // ngăn reload trang
    productVariant.UploadImages(this); // truyền form đang submit
});
