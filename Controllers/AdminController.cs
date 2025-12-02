using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Enums;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Security.Claims;

namespace ProjectTest1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly DataContext db;
        
        public AdminController(DataContext db)
        {
            this.db = db;
        }
        public async Task<IActionResult> Index()
        {
            int cancelledCount = await db.Orders
                .CountAsync(x => x.Status == Enums.StatusOrderEnum.Cancelled);

            var stats = await db.Orders
                .Join(db.OrderDetails,
                      o => o.OrderId,
                      od => od.OrderId,
                      (o, od) => new { o, od })
                .Where(x => x.o.Status == Enums.StatusOrderEnum.Confirmed) // chỉ tính đơn hoàn tất
                .GroupBy(x => 1) // nhóm tất cả lại thành 1 nhóm
                .Select(g => new OrderStatsViewModel
                {
                    TotalOrders = g.Select(x => x.o.OrderId).Distinct().Count(),
                    TotalProductsSold = g.Sum(x => x.od.Quantity),
                    TotalRevenue = g.Sum(x => x.od.Quantity * x.od.UnitPrice),
                    TotalOrdersCancel = cancelledCount
                })
                .FirstOrDefaultAsync();
           

            return View(stats);
        }
        // GET: /Admin/OrderNotificationModal
        public IActionResult OrderNotificationModal()
        {
            return PartialView("OrderNotificationModal"); // chỉ load modal, chưa có data
        }

        public async Task<IActionResult> GetNotifications(int page =1, int pageSize=7)
        {
            var userIdString = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                return Content("❌ Không tìm thấy thông tin User");

            var notifs = await db.OrderNotification
                            .Where(n => n.UserId == userId)
                            .OrderByDescending(n => n.CreatedAt)
                            .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                            .Select(n => new OrderNotificationViewModel
                            {
                                Id = n.Id,
                                OrderId = n.OrderId,
                                IsRead = n.IsRead,
                                CreatedAt = n.CreatedAt,
                                Title = n.Title,
                                Message = n.Message,
                                Url = n.Url,
                                UserId = n.UserId ?? Guid.Empty,
                                Type = n.Type,
                                Image = n.Type == NotificationType.Comment || n.Type == NotificationType.Reply
                                    ? "/images/default-avatar.png"
                                    : (n.Order != null
                                        ? n.Order.OrderDetails
                                            .Select(od => od.ProductVariant.Product.Img)
                                            .FirstOrDefault() ?? "/images/default-product.png"
                                        : "/images/default.png")
                            })
                            .ToListAsync();
            return PartialView("NotificationList", notifs);
        }

        public async Task<JsonResult> GetUnreadCount()
        {
            var userIdString = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin User." });
            }

            var count = await db.OrderNotification.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Json(new { success = true, count });
        }

        public ActionResult RevenueData()
        {
            var data = db.Orders
                .Where(o => o.Status == Enums.StatusOrderEnum.Delivered)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Month)
                .ToList();

            return Json(data);
        }
        public ActionResult ProductVariantSalesData()
        {
            var data = db.OrderDetails
                .Where(od => od.Order.Status == Enums.StatusOrderEnum.Delivered)
                .GroupBy(od => new
                {
                    ProductName = od.ProductVariant.Product.Name,
                    Size = od.ProductVariant.Size.SizeName,
                    Color = od.ProductVariant.Color.ColorName
                })
                .Select(g => new
                {
                    VariantName = g.Key.ProductName + " - " + g.Key.Size + " - " + g.Key.Color,
                    Quantity = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(10) // Top 10 biến thể bán chạy
                .ToList();

            return Json(data);
        }


        public ActionResult OrderStatusData()
        {
            var data = db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToList();

            return Json(data);
        }
        public ActionResult NewCustomersData()
        {
            var data = db.User
                .GroupBy(u => u.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            return Json(data);
        }
        public ActionResult DashBroad()
        {
            return View();
        }
        public ActionResult ReplyComment()
        {
            return View();
        }
    }
}
