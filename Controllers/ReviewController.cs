using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using ProjectTest1.Enums;
using ProjectTest1.Helpper;
using ProjectTest1.Migrations;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjectTest1.Controllers
{
    public class ReviewController : Controller
    {
        private readonly DataContext db;
        private readonly IHubContext<OrderHub> _hubContext;
        public ReviewController(DataContext db, CloudinaryHelper _cloudinaryService, IHubContext<OrderHub> _hubContext)
        {
            this.db = db;
            this._hubContext = _hubContext;
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateReview(ReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var userIdString = User.FindFirstValue("UserId");
            var fullName = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin User." });
            }

            // ✅ Thêm Transaction
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var review = new ReviewModel
                {
                    Comment = model.Comment,
                    OrderDetailId = model.OrderDetailId,
                    Rating = model.Rating,
                    ProductVariantId = model.ProductVariantId,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                db.Reviews.Add(review);
                await db.SaveChangesAsync();

                var reviewVm = new ReviewViewModel
                {
                    Id = review.Id,
                    ReviewerName = User.Identity.Name, // Hoặc r.User.FullName nếu Include
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt
                };

                var productInfo = await db.ProductVariants
                    .Include(pv => pv.Product)
                    .Where(pv => pv.ProductVariantId == model.ProductVariantId)
                    .Select(pv => new
                    {
                        ProductId = pv.Product.Id,
                        ProductName = pv.Product.Name,
                        ProductImage = pv.Product.Img
                    })
                    .FirstOrDefaultAsync();

                string productName = productInfo?.ProductName ?? "Sản phẩm";
                string image = productInfo?.ProductImage ?? "/images/default.png";
                int? productId = productInfo?.ProductId;

                var adminUser = await db.User
                    .Where(u => u.Role == "Admin")
                    .FirstOrDefaultAsync();

                var adminUserId = adminUser?.UserId;
                string title = "Bình luận mới";

                var hubContext = HttpContext.RequestServices.GetService(typeof(IHubContext<OrderHub>)) as IHubContext<OrderHub>;

                if (hubContext != null && adminUserId != null)
                {
                    var notif = new OrderNotificationModel
                    {
                        UserId = adminUserId,
                        Title = title,
                        Message = $"{fullName} đã bình luận sản phẩm {productName}",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        Type = NotificationType.Comment,
                        Url = $"/Product/ProductDetail/{productId}?reviewId={reviewVm.Id}",
                    };

                    db.OrderNotification.Add(notif);
                    await db.SaveChangesAsync();

                    int unreadCount = await db.OrderNotification
                        .CountAsync(n => !n.IsRead && n.UserId == adminUserId);

                    await hubContext.Clients.User(adminUserId.ToString())
                        .SendAsync("ReceiveOrderNotificationUser", new
                        {
                            Id = notif.Id,
                            OrderId = notif.OrderId,
                            Title = notif.Title,
                            Message = notif.Message,
                            CreatedAt = notif.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                            Image = image,
                            UnreadCount = unreadCount
                        });
                }

                var reviewHtml = await RenderPartialViewToStringAsync("ReviewItem", reviewVm);

                // ✅ Commit Transaction khi mọi thứ OK
                await transaction.CommitAsync();

                return Json(new { success = true, message = "✅ Cảm ơn bạn!", html = reviewHtml });
            }
            catch (Exception ex)
            {
                // ❌ Rollback khi lỗi
                await transaction.RollbackAsync();

                // log lỗi nếu cần: _logger.LogError(ex, "Lỗi khi tạo review");
                return Json(new { success = false, message = "⚠️ Có lỗi xảy ra: " + ex.Message });
            }
        }

        public IActionResult Index()
        {
            return View();
        }
        protected async Task<string> RenderPartialViewToStringAsync(string viewName, object model)
        {
            ViewData.Model = model;

            using var sw = new StringWriter();
            var viewEngine = HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;

            var viewResult = viewEngine.FindView(ControllerContext, viewName, false);

            if (!viewResult.Success)
            {
                throw new ArgumentNullException($"View '{viewName}' không tìm thấy. Hãy kiểm tra tên và đường dẫn view.");
            }

            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                ViewData,
                TempData,
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
        public async Task<IActionResult> LoadComments(int productId, int page = 1, int pageSize = 5)
        {
            var Role = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrEmpty(Role))
            {
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin User." });
            }
            try
            {
                var query = db.Reviews
                    .Where(r => r.ProductVariant.Product.Id == productId && r.IsVisible==true)
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .AsNoTracking();

                var total = await query.CountAsync();

                var reviews = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReviewViewModel
                    {
                        Id = r.Id,
                        ReviewerName = r.User.FullName,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        SellerReply = r.SellerReply,
                        SellerReplyAt = r.SellerReplyAt
                    })
                    .ToListAsync();

                var model = new ReviewListViewModel
                {
                    ProductVariantId = productId,
                    Reviews = reviews,
                };

                return Json(new
                {
                    data = reviews,
                    total,
                    totalPages = (int)Math.Ceiling((double)total / pageSize),
                    currentPage = page,
                    isAdmin = Role
                });
            }
            catch (Exception ex)
            {
                // Log the exception (replace with proper logging in production)
                Console.WriteLine($"Error loading comments: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> AdminReply(int reviewId, string reply)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrEmpty(role) || !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "❌ Bạn không có quyền thực hiện hành động này." });
            }

            if (reviewId == 0)
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin Review." });

            if (string.IsNullOrWhiteSpace(reply))
                return Json(new { success = false, message = "⚠️ Nội dung phản hồi không được để trống." });

            using var transaction = await db.Database.BeginTransactionAsync(); // ✅ thêm transaction

            try
            {
                var review = await db.Reviews
                    .Include(r => r.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == reviewId);

                if (review == null)
                    return Json(new { success = false, message = "❌ Không tìm thấy thông tin Review." });

                // ✅ cập nhật phản hồi
                review.SellerReply = reply;
                review.SellerReplyAt = DateTime.Now;
                await db.SaveChangesAsync();

                // ✅ tạo thông báo
                var notif = new OrderNotificationModel
                {
                    UserId = review.UserId, // người nhận là người viết review
                    Title = "Phản hồi bình luận",
                    Message = $"Admin đã phản hồi bình luận của bạn về sản phẩm {review.ProductVariant.Product.Name}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Type = NotificationType.Reply,
                    Url = $"/Product/ProductDetail/{review.ProductVariant.Product.Id}?reviewId={reviewId}",
                };

                db.OrderNotification.Add(notif);
                await db.SaveChangesAsync();

                // ✅ đếm số thông báo chưa đọc
                int unreadCount = await db.OrderNotification
                    .CountAsync(n => !n.IsRead && n.UserId == review.UserId);

                // ✅ gửi SignalR
                var hubContext = HttpContext.RequestServices.GetService(typeof(IHubContext<OrderHub>)) as IHubContext<OrderHub>;
                if (hubContext != null)
                {
                    await hubContext.Clients.User(review.UserId.ToString())
                        .SendAsync("ReceiveOrderNotificationUser", new
                        {
                            Id = notif.Id,
                            Title = notif.Title,
                            Message = notif.Message,
                            CreatedAt = notif.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                            Image = review.ProductVariant.Product.Img ?? "/images/default.png",
                            UnreadCount = unreadCount
                        });
                }

                await transaction.CommitAsync(); // ✅ commit transaction

                return Json(new { success = true, message = "✅ Phản hồi người dùng thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // ✅ rollback khi lỗi
                return Json(new { success = false, message = "⚠️ Có lỗi xảy ra khi phản hồi: " + ex.Message });
            }
        }
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetReviews(int page = 1, int pageSize = 5, string? keyword = "", int? status = 0, int? replyStatus = 0)
        {
            try
            {
                var query = db.Reviews
                    .Include(r => r.User)
                    .Include(r => r.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .AsNoTracking()
                    .AsQueryable();

                // 2️⃣ Bộ lọc theo keyword (comment hoặc tên user)
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(c =>
                        c.Comment.Contains(keyword) ||
                        (c.User != null && c.User.FullName.Contains(keyword)));
                }

                // 3️⃣ Bộ lọc theo trạng thái hiển thị
                if (status == 1) // Hiển thị
                {
                    query = query.Where(c => c.IsVisible == true);
                }
                else if (status == 2) // Đã ẩn
                {
                    query = query.Where(c => c.IsVisible == false);
                }

                // 4️⃣ Bộ lọc theo phản hồi của Admin
                if (replyStatus == 1) // Đã phản hồi
                {
                    query = query.Where(c => !string.IsNullOrEmpty(c.SellerReply));
                }
                else if (replyStatus == 2) // Chưa phản hồi
                {
                    query = query.Where(c => string.IsNullOrEmpty(c.SellerReply));
                }

                // 5️⃣ Tính tổng và phân trang
                var totalReviews = await query.CountAsync();

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReviewViewModel
                    {
                        Id = r.Id,
                        Comment = r.Comment,
                        Rating = r.Rating,
                        ReviewerName = r.User != null
                            ? r.User.FullName
                            : "(Người dùng đã xoá)",
                        ProductName = r.ProductVariant != null && r.ProductVariant.Product != null
                            ? r.ProductVariant.Product.Name
                            : "(Sản phẩm không tồn tại)",
                        ProductImage = r.ProductVariant != null && r.ProductVariant.Product != null
                            ? r.ProductVariant.Product.Img
                            : "/images/no-image.png",
                        CreatedAt = r.CreatedAt,
                        SellerReply = r.SellerReply,
                        SellerReplyAt = r.SellerReplyAt,
                        IsVisible = r.IsVisible,
                        avatarUrl = r.User != null
                            ? (string.IsNullOrEmpty(r.User.AvatarUrl) ? "/images/default-avatar.png" : r.User.AvatarUrl)
                            : "/images/default-avatar.png"
                    })
                    .ToListAsync();

                // 6️⃣ Trả về PartialView (phân trang)
                ViewBag.Total = totalReviews;
                ViewBag.TotalPage = (int)Math.Ceiling(totalReviews / (double)pageSize);
                ViewBag.CurrentPage = page;

                return PartialView("_CommentList", reviews);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error GetReviews: {ex}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách review: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> changVisible(int? reviewId)
        {
            if (reviewId == null)
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin bình luận." });
            var review = await db.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return Json(new { status = false, Message = "Bình luận đã bị xóa" });
            }
            try
            {
                review.IsVisible = !review.IsVisible;
                await db.SaveChangesAsync();
                string msg = review.IsVisible
                ? "Bình luận đã được hiển thị."
                : "Bình luận đã được ẩn.";
                return Json(new { status = true, message = msg });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Json(new { status = false, message = "Không ẩn được bình luận này" });
            }
            
        }
        [HttpPost]
        public async Task<IActionResult> UpdateReply(int? reviewId, string newReply)
        {
            if (reviewId == null)
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin bình luận." });
            var review = await db.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return Json(new { success = false, Message = "Bình luận đã bị xóa" });
            }
            try
            {
                review.SellerReply = newReply;
                await db.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật bình luận thành công" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Json(new { success = false, message = "Không cập nhật được bình luận này" });
            }
        }



    }
}


