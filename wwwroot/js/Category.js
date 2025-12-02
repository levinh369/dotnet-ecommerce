var currentPage = 1;
$(function () {
    category.loadCategoryData(1, 100, "");
});
var category = {
    openCreateModal: function () {
        
        $.ajax({
            url: '/Category/Create',
            type: 'GET',
            success: function (result) {
                $('#modal-placeholder').html(result);
                $('#categoryModal').modal('show');
                category.bindEvents();
            },
            error: function () {
                alert("Đã có lỗi xảy ra khi tải form.");
            }
        });
    },
    createCategory: function () {
        var form = $('#categoryForm');

        if (!form.valid()) return;

        $.ajax({
            url: '/Category/Create',
            type: 'POST',
            data: form.serialize(),
            success: function (res) {
                if (res.success) {
                    $('#categoryModal').modal('hide');
                    toastr.success(res.message);
                    category.loadCategoryData(); // Nếu có hàm reload danh sách thì gọi
                } else {
                    toastr.error(res.message);
                }
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });
    },
    loadCategoryData: function (pageIndex) {
        var data = $('#filterForm').serializeArray();
        if (pageIndex !== undefined) {
            // dùng pageIndex truyền vào
        } else {
            pageIndex = 1;
        }
        // Thêm pageIndex vào data (với tên "page" trùng tham số controller)
        data.push({ name: "page", value: pageIndex });
        debugger;
        currentPage = pageIndex;
        $.ajax({
            url: "/Category/ListData",
            type: "get",
            data: data,
            success: function (result) {
                $("#gridData").html(result);
                var totalPages = $("#pagination").data("total-pages");
                if (!$('#paging-ul').data("twbs-pagination")) {
                    category.showPaging(totalPages, pageIndex);
                }
            },
            error: function () {
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
                        category.loadCategoryData(page);
                    }
                }
            });
            $('#paging-ul').data('init-complete', true); // đánh dấu đã init
        }
    },
    changeStatus: function (id) {
        $.ajax({
            url: "/Category/Status",
            type: "GET",
            data: { id: id },
            success: function (res) {
                toastr.success(res.message);
                category.loadCategoryData(currentPage);
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
                    url: "/Category/Delete",
                    type: "GET",
                    data: { id: id },
                    success: function (res) {
                        toastr.success(res.message);
                        category.loadCategoryData();
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
            url: '/Category/Edit',
            type: 'GET',
            data: { id: id },
            success: function (result) {
                $('#modal-placeholder').html(result);
                $('#editModal').modal('show');
                category.bindEvents();
            },
            error: function () {
                console.error('Lỗi tải form:', error);
                alert('Lỗi khi tải form chỉnh sửa.');
            }
        });

    },
    detail: function (id) {
        $.ajax({
            url: '/Category/Detail',
            type: 'GET',
            data: { id: id },
            success: function (result) {
                $('#modal-placeholder').html(result);
                $('#categoryDetailModal').modal('show');
            },
            error: function () {
                toastr.error("Lỗi tải trang");
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
                    url: '/Category/deleteAll',
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
                            category.loadCategoryData(currentPage); // tải lại trang hiện tại
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
    EditPost: function () {
        debugger;
        var formData = $('#editForm').serialize();

        $.ajax({
            url: "/Category/EditPost",
            type: "POST",
            data: formData,
            success: function (res) {
                $('#editModal').modal('hide');
                toastr.success(res.message);
                category.loadCategoryData();
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });
    },
    bindEvents: function () {
        $(document).off("change", "#ParentLevel1");
        // Khi chọn cấp 1 → load cấp 2
        debugger;
        $(document).on("change", "#ParentLevel1", function () {
            var parentId = $(this).val();
            $("#ParentLevel2").html('<option value="">-- Chọn cấp 2 --</option>').prop("disabled", true);
            $("#ParentId").val(parentId || '');
            if (parentId) {
                $.getJSON('/Category/GetChildCategories', { parentId: parentId }, function (data) {
                    if (data.length > 0) {
                        $.each(data, function (i, item) {
                            $("#ParentLevel2").append($('<option>', {
                                value: item.categoryId,
                                text: item.categoryName
                            }));
                        });
                        $("#ParentLevel2").prop("disabled", false);
                    }
                });
            }
        });
    },

};

// BẮT SỰ KIỆN SUBMIT FORM (dùng delegation vì form được load động)
$(document).on('submit', '#categoryForm', function (e) {
    e.preventDefault(); // Ngăn form submit mặc định
    category.createCategory(); // Gọi hàm ajax
});
$(document).on('submit', '#editModal', function (e) {
    e.preventDefault(); // Ngăn form submit mặc định
    category.EditPost(); // Gọi hàm ajax
});
$('#filterForm').on('submit', function (e) {
    e.preventDefault();  // ngăn form submit reload trang
    category.loadCategoryData(1);
});
$(document).on("change", "#ParentLevel2", function () {
    var level2 = $(this).val();
    var level1 = $("#ParentLevel1").val();
    $("#ParentId").val(level2 || level1 || '');
});