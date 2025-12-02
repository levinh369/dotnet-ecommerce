var currentPage = 1;

var review = {
    loadData: function (pageIndex) {
        var productId = $("#swatches").data("id"); // lấy từ ProductDetail
        var page = pageIndex || 1;
        currentPage = page;

        $.ajax({
            url: "/Review/LoadComments",
            type: "GET",
            data: {
                page: page,
                pageSize: 5,
                productId: productId
            },
            success: function (res) {
                
                let html = "";

                if (res.data != null && res.data.length > 0) {
                    res.data.forEach(r => {
                        html += `
                <div id="review-${r.id}" class="comment-item border-bottom pb-3 mb-3">
                    <strong>${r.reviewerName}</strong>
                    <span class="text-warning">
                        ${"★".repeat(r.rating)}${"☆".repeat(5 - r.rating)}
                    </span>
                    <p class="mb-1">${r.comment}</p>
                    <small class="text-muted">${new Date(r.createdAt).toLocaleString()}</small>
            `;

                        if (r.sellerReply) {
                            html += `
                    <div class="bg-light p-2 mt-2 rounded">
                        <strong class="text-danger">Phản hồi từ người bán:</strong>
                        <p class="mb-0">${r.sellerReply}</p>
                        <small class="text-muted">${r.sellerReplyAt || ""}</small>
                    </div>`;
                        } else if ((res.isAdmin || "").toLowerCase() === "admin") {
                            html += `
                    <div class="mt-2">
                        <button type="button" class="btn btn-sm btn-outline-secondary btn-reply">Phản hồi</button>
                        <div class="reply-box mt-2" style="display:none;">
                            <div class="input-group">
                                <input id="adminInput" type="text" class="form-control reply-content" placeholder="Nhập phản hồi..." />
                                <button type="button" class="btn btn-sm btn-primary"
                                    onclick="review.adminReply(this)">Gửi</button>
                                <button type="button" class="btn btn-sm btn-outline-danger"
                                    onclick="review.cancelReply(this)">Hủy</button>
                            </div>
                        </div>
                    </div>`;
                        }

                        html += `</div>`;
                    });
                } else {
                    html = "<p class='text-muted'>Chưa có đánh giá nào.</p>";
                }

                // ✅ Di chuyển ra ngoài — luôn thực hiện dù có hay không có dữ liệu
                $("#CommentContent").html(html);

                $("#total-review").text(res.total);
                review.showPaging(res.totalPages, res.currentPage);
                review.showCommentUser();
            },


            error: function () {
                toastr.error("Lỗi tải dữ liệu");
            }
        });
    },
    cancelReply: function (btn) {
        const box = $(btn).closest(".reply-box");
        box.slideUp(); // ẩn khung reply
        box.find(".reply-content").val(""); // xóa nội dung
    },
    adminReply: function (btn) {
        debugger;
        console.log(btn);
        const item = $(btn).closest(".comment-item"); // comment hiện tại
        const reviewId = item.attr("id").replace("review-", "");
        const replyText = item.find(".reply-content").val().trim();
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
                    review.loadData(currentPage); // reload trang hiện tại
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

        $("#globalLoading").show();
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
                $("#globalLoading").hide();
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
                $("#globalLoading").hide();
                toastr.error("Lỗi gửi đánh giá");
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
                        review.loadData(page);
                    }
                }
            });
            $('#paging-ul').data('init-complete', true); // đánh dấu đã init
        }
    },
    showCommentUser: function () {
        
        const urlParams = new URLSearchParams(window.location.search);
        const reviewId = urlParams.get("reviewId");
        if (reviewId) {
            const reviewElement = document.getElementById(`review-${reviewId}`);

            if (reviewElement) {
                // Cuộn mượt đến bình luận
                reviewElement.scrollIntoView({ behavior: 'smooth', block: 'center' });

                // Thêm hiệu ứng nổi bật
                reviewElement.classList.add('highlight-review');

                // Sau 3s thì bỏ highlight
                setTimeout(() => reviewElement.classList.remove('highlight-review'), 3000);
            } else {
                console.warn(`Không tìm thấy review có id review-${reviewId}`);
            }
        }
    },

};
// Submit form
$(document).on("submit", "#commentForm", function (e) {
    e.preventDefault();
    review.createReview();
});
$(function () {
    var variantId = $('#product-data').data('variant-id');
    var orderDetailId = $('#product-data').data('order-detail-id');
    var productName = $('#product-data').data('product-name');
    $('#commentProductId').val(variantId || productId);
    $('#commentOrderDetailId').val(orderDetailId);
    $('#commentProductName').val(productName || '');   
    review.loadData(1);
});
$(document).on("click", ".btn-reply", function () {
    const box = $(this).siblings(".reply-box");
    box.toggle(); // ẩn/hiện ô nhập
});
