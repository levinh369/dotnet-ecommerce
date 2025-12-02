
var currentPage = 1;
var reviewAdmin = {
    loadData: function (pageIndex) { 
        showGlobalLoading(true);
        var data = $('#filterForm').serializeArray();
        data.push({ name: "page", value: pageIndex });
        currentPage = pageIndex;
        $.ajax({
            url: "/Review/GetReviews",
            type: "GET",
            data: data,
            success: function (res) {
                setTimeout(function () {
                    $("#commentList").html(res);
                    var totalPages = $("#pagination").data("total-pages");
                    if (!$('#paging-ul').data("twbs-pagination")) {
                        reviewAdmin.showPaging(totalPages, pageIndex);
                        }
                    showGlobalLoading(false);
                }, 1000); // delay 1000ms
            },
            error: function () {
                toastr.error("❌ Lỗi tải dữ liệu bình luận");
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
                        reviewAdmin.loadData(page);
                    }
                }
            });
            $('#paging-ul').data('init-complete', true); // đánh dấu đã init
        }
    },
    cancelReply: function (btn) {
        const box = $(btn).closest(".reply-box");
        box.slideUp(); // ẩn khung reply
        box.find(".reply-content").val(""); // xóa nội dung
    },
    adminReply: function (reviewId) {
        var replyText = $(`form[data-comment-id='${reviewId}'] input[name='reply']`).val().trim();
        if (!replyText) {
            toastr.warning("⚠️ Vui lòng nhập nội dung phản hồi.");
            return;
        }
        $.ajax({
            url: '/Review/AdminReply',
            type: 'POST',
            data: { reviewId: reviewId, reply: replyText },
            success: function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    reviewAdmin.loadData(currentPage); // reload trang hiện tại
                } else {
                    toastr.error(res.message);
                }
            },
            error: function () {
                toastr.error("Lỗi gửi phản hồi");
            }

        })
    },
    createReview: function () {
        var form = $('#commentForm');
        if (!form.valid()) return;

        $('#loadingOverlay').addClass('d-flex').show();
        var formData = new FormData(form[0]);
        //var variantId = $('#product-data').data('variant-id') || null;
        //var orderDetailId = $('#product-data').data('order-detail-id') || null;
        $.ajax({
            url: '/Review/CreateReview',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                $('#loadingOverlay').hide().removeClass('d-flex');
                if (res.success) {
                    $('#CommentContent p.text-muted').remove();
                    $('#CommentContent').prepend(res.html);

                    // Ẩn form và disable nó
                    $('#commentForm').hide().data('submitted', true);
                    $('#commentContent').val('');
                    toastr.success(res.message);
                    var total = parseInt($('#total-review').text()) || 0;
                    $('#total-review').text(total + 1);

                    // ✅ Xóa hash #commentForm khỏi URL
                    if (window.location.hash === "#commentForm") {
                        history.replaceState(null, null, window.location.pathname + window.location.search);
                    }
                } else {
                    toastr.error(res.message);
                }
            },
            error: function () {
                $('#loadingOverlay').hide();
                toastr.error("Lỗi gửi đánh giá");
            }
        });
    },
    changeVisible: function (id) {
        $.ajax({
            url: "/Review/changVisible",
            type: "Post",
            data: { reviewId: id },
            success: function (res) {
                toastr.success(res.message);
                reviewAdmin.loadData(currentPage);
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });
    },
    editReply: function (reviewId, reply) {
        const form = document.getElementById(`updateForm-${reviewId}`);
        const replyText = reply;
        if (form) {
            const input = form.querySelector('input[name="reply"]');
            input.value = replyText || "";
            form.style.display = "flex"; // Hiện form đúng ID
            input.focus();
        }
    },
    cancelUpdate: function (btn) {
        const formId = $(btn).data("form-id");
        const form = document.getElementById(formId);
        if (form) {
            form.style.display = "none";
        }
    },
    updateComment: function (reviewId) {
        var form = $(`#updateForm-${reviewId}`);
        var replyText = form.find("input[name='reply']").val().trim();
        if (!replyText) {
            toastr.warning("⚠️ Vui lòng nhập nội dung phản hồi.");
            return;
        }
        $.ajax({
            url: '/Review/UpdateReply',
            type: 'POST',
            data: { reviewId: reviewId, newReply: replyText },
            success: function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    reviewAdmin.loadData(currentPage); // reload trang hiện tại
                } else {
                    toastr.error(res.message);
                }
            },
            error: function () {
                toastr.error("Lỗi cập nhật phản hồi");
            }
        })
    }


};
reviewAdmin.loadData();
$('#filterForm').on('submit', function (e) {
    e.preventDefault();  // ngăn form submit reload trang
    reviewAdmin.loadData(1);
});
$(document).on('submit', '.replyForm', function (e) {
    e.preventDefault(); // Ngăn reload trang
    var reviewId = $(this).data('comment-id');
    reviewAdmin.adminReply(reviewId);
});
$(document).on("click", ".edit-reply-btn", function () {
    const id = $(this).data("id");
    const reply = $(this).data("reply");
    reviewAdmin.editReply(id, reply);
});
$(document).on('submit', '.updateForm', function (e) {
    e.preventDefault(); // Ngăn reload trang
    var reviewId = $(this).data('comment-id');
    reviewAdmin.updateComment(reviewId);
});