using MailKit.Search;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Enums;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Security.Claims;
using static NuGet.Packaging.PackagingConstants;
namespace ProjectTest1.Controllers
{
    public class OrderController : Controller
    {
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly DataContext db;
        public OrderController(DataContext db, IHubContext<OrderHub> hubContext)
        {
            this.db = db;
            _hubContext = hubContext;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<PartialViewResult> ListData(int page = 1, int pageSize = 5, String keySearch = "", DateTime? fromDate = null, DateTime? toDate = null, float minPrice = 0, float maxPrice = 0, int status = 0)
        {
            ViewBag.page = page;
            ViewBag.pageSize = pageSize;
            var query = db.Orders.Include(o => o.User).AsQueryable();
            if (!string.IsNullOrEmpty(keySearch))
            {
                query = query.Where(o =>
        o.User.FullName.Contains(keySearch) ||   // tìm theo tên khách
        o.OrderId.ToString().Contains(keySearch) // tìm theo mã đơn
    );
            }
            if (status > 0)
            {
                query = query.Where(o => o.Status == (StatusOrderEnum)status);
            }
            var total = await query.CountAsync();
            ViewBag.total = total;
            ViewBag.totalPage = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.stt = (page - 1) * pageSize;
            var order = await query
    .OrderByDescending(x => x.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

            var orderViewModel = order.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                userName = o.User.FullName,
                ShippingAddress = o.ShippingAddress,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            }).ToList();
            return PartialView(orderViewModel);
        }
        public async Task<JsonResult> Status(int? value, int? orderId)
        {
            if (value == null || orderId == null)
            {
                return Json(new { status = false, Message = "Giá trị không được để trống" });
            }

            var order = await db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return Json(new { status = false, Message = "Đơn hàng không tồn tại" });
            }

            if (order.Status == StatusOrderEnum.Delivered)
            {
                return Json(new { success = false, message = "Đơn hàng đã hoàn tất, không thể thay đổi." });
            }
            if (value == 4) // 4 = Delivered
            {
                var earnedPoints = (int)(order.FinalAmount / 10000); // ví dụ 100k = 10 điểm

                var userPoint = await db.UserPoints
                    .FirstOrDefaultAsync(p => p.UserId == order.UserId);

                if (userPoint == null)
                {
                    userPoint = new UserPointsModel
                    {
                        UserId = order.UserId,
                        TotalPoints = earnedPoints,
                        LifetimePoints = earnedPoints,
                        UpdatedAt = DateTime.Now
                    };
                    db.UserPoints.Add(userPoint);
                }
                else
                {
                    // 🔁 Nếu đã có thì cộng thêm
                    userPoint.TotalPoints += earnedPoints;
                    userPoint.LifetimePoints += earnedPoints;
                    userPoint.UpdatedAt = DateTime.Now;
                }
                await db.SaveChangesAsync();
            }

            // ✅ Thêm Transaction EF
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var newStatus = (StatusOrderEnum)value;
                order.Status = newStatus;
                await db.SaveChangesAsync();

                string message = newStatus switch
                {
                    StatusOrderEnum.Confirmed => $"Đơn hàng #{order.OrderId} của bạn đã được xác nhận ✅",
                    StatusOrderEnum.Cancelled => $"Đơn hàng #{order.OrderId} của bạn đã bị hủy ❌",
                    StatusOrderEnum.Shipping => $"Đơn hàng #{order.OrderId} của bạn đã được giao cho đơn vị vận chuyển 🚚",
                    StatusOrderEnum.Delivered => $"Đơn hàng #{order.OrderId} của bạn đã hoàn tất 🎉",
                    _ => $"Đơn hàng #{order.OrderId} của bạn đã được cập nhật."
                };

                string title = "Cập nhật trạng thái đơn hàng";

                var hubContext = HttpContext.RequestServices.GetService(typeof(IHubContext<OrderHub>)) as IHubContext<OrderHub>;
                if (hubContext != null)
                {
                    var notif = new OrderNotificationModel
                    {
                        OrderId = order.OrderId,
                        UserId = order.UserId,
                        Title = title,
                        Message = message,
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        Type = NotificationType.OrderConfirm,
                        Url = $"/Order/OrderDetailUser?orderId={order.OrderId}"
                    };

                    db.OrderNotification.Add(notif);
                    await db.SaveChangesAsync();

                    int unreadCount = db.OrderNotification.Count(n => !n.IsRead && n.UserId == order.UserId);

                    var firstDetail = order.OrderDetails.FirstOrDefault();
                    string image = firstDetail?.ProductVariant?.Product?.Img ?? "/images/default.png";

                    await hubContext.Clients.User(order.UserId.ToString())
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
                // ✅ Commit transaction nếu tất cả thành công
                await transaction.CommitAsync();

                return Json(new { status = true, message = "Đơn hàng đã được cập nhật trạng thái thành công" });
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return Json(new { status = false, message = "Không thay đổi được trạng thái đơn hàng này" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { status = false, message = "⚠️ Có lỗi xảy ra: " + ex.Message });
            }
        }

        public async Task<ActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return Json(new { status = false, Message = "Id không được để trống" });
            }
            var order = await db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return Json(new { status = false, message = "Đơn hàng không tồn tại" });
            }
            var model = new OrderDetailViewModel
            {
                UserName = order.User.FullName,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Address = order.ShippingAddress,
                ShipDate = order.ShippedDate,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                FinalAmount = order.FinalAmount,
                Phone = order.Phone,
                Email = order.Email,
                PaymentMethod = "Thanh toán " + order.PaymentStatus.ToString(),
                DiscountValue = order.DiscountValue,
                Items = order.OrderDetails.Select(od => new OrderDetailItemViewModel
                {
                    ProductVariantId = od.ProductVariantId,
                    image = od.ProductVariant.Product.Img,
                    ProductName = od.ProductVariant.Product.Name,
                    Size = od.ProductVariant.Size?.SizeName ?? "",
                    Color = od.ProductVariant.Color?.ColorName ?? "",
                    Description = od.ProductVariant.Product.Description,
                    Quantity = od.Quantity,
                    UnitPrice = (float)od.UnitPrice,
                }).ToList()
            };

            return PartialView("Detail", model);
        }
        [HttpPost]
        public async Task<IActionResult> ReadNotif(int id)
        {
            var notif = await db.OrderNotification.FindAsync(id);
            if (notif == null)
                return NotFound();

            bool wasRead = notif.IsRead;

            if (!notif.IsRead)
            {
                notif.IsRead = true;
                await db.SaveChangesAsync();
            }

            return Json(new { success = true, alreadyRead = wasRead });
        }


        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return Json(new { status = false, message = "Id không được để trống" });
            }

            try
            {
                var obj = await db.Orders
                                  .FirstOrDefaultAsync(p => p.OrderId == id);
                if (obj == null)
                {
                    return Json(new { status = false, message = "Không tìm thấy bản ghi." });
                }

                db.Orders.Remove(obj);
                await db.SaveChangesAsync();  // dùng async version

                return Json(new { status = true, message = "Bản ghi đã được xóa thành công" });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { status = false, message = "Không xóa được bản ghi này" });
            }
        }
        public async Task<ActionResult> OrderUser()
        {
            var userIdString = User.FindFirstValue("UserId");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Content("❌ Không tìm thấy thông tin User");
            }
            try
            {
                var orders = await db.Orders.
                    Where(p => p.UserId == userId)
                    .Select(o => new OrderViewModel
                    {
                        OrderId = o.OrderId,
                        Status = o.Status,
                        Items = o.OrderDetails
                    .Select(d => new OrderDetailItemViewModel
                    {
                        ProductName = d.ProductVariant.Product.Name,
                        image = d.ProductVariant.Product.Img,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                    }).ToList()
                    })
                    .ToListAsync();
                if (orders == null || !orders.Any())
                {
                    return Content("⚠️ Không có đơn hàng nào.");
                }
                return View("Order", orders);
            }
            catch (Exception ex)
            {
                return Content("⚠️ Lỗi: " + ex.Message);
            }
        }
        [HttpPost]
        public async Task<ActionResult> GetOrder(int? status)
        {
            var userIdString = User.FindFirstValue("UserId");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Content("❌ Không tìm thấy thông tin User");
            }

            try
            {
                var query = db.Orders.Where(p => p.UserId == userId);

                if (status.HasValue)
                {
                    query = query.Where(o => (int)o.Status == status.Value);
                }

                var orders = await query
                    .Select(o => new OrderViewModel
                    {
                        OrderId = o.OrderId,
                        Status = o.Status,
                        Items = o.OrderDetails.Select(d => new OrderDetailItemViewModel
                        {
                            ProductName = d.ProductVariant.Product.Name,
                            image = d.ProductVariant.Product.Img,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice
                        }).ToList()
                    })
                    .ToListAsync();

                if (orders == null || !orders.Any())
                {
                    return Content("⚠️ Không có đơn hàng nào.");
                }

                return PartialView("OrderPartial", orders);
            }
            catch (Exception ex)
            {
                return Content("⚠️ Lỗi: " + ex.Message);
            }

        }
        [HttpPost]
        public async Task<IActionResult> loadNotification(int page = 1, int pageSize = 6)
        {
            var userIdString = User.FindFirstValue("UserId");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Content("❌ Không tìm thấy thông tin User");
            }
            try
            {
                var notif = await db.OrderNotification
                    .Include(o => o.Order)
                        .ThenInclude(od => od.OrderDetails)   // nếu cần ProductVariant
                            .ThenInclude(pv => pv.ProductVariant)
                            .ThenInclude(p => p.Product)// nếu cần Product
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                   .Skip((page - 1) * pageSize)
                   .Take(pageSize)
                   .Select(n => new OrderNotificationViewModel
                   {
                       Id = n.Id,
                       OrderId = n.OrderId,
                       Title = n.Title,
                       Message = n.Message,
                       CreatedAt = n.CreatedAt,
                       IsRead = n.IsRead,
                       Url = n.Url,
                       Image = n.Order.OrderDetails
              .Select(od => od.ProductVariant.Product.Img)
              .FirstOrDefault()

                   }).ToListAsync();
                return Json(notif); 
            }
            catch (Exception ex)
            {
                return Content("⚠️ Lỗi: " + ex.Message);
            }
        }
        //public async Task<IActionResult> getNotification()
        //{
        //    var userIdString = User.FindFirstValue("UserId");

        //    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        //    {
        //        return Content("❌ Không tìm thấy thông tin User");
        //    }
        //    var notifs = await db.OrderNotification
        //                   .Where(n => n.UserId == userId)
        //                   .OrderByDescending(n => n.CreatedAt)
        //                   .Take(5)   // Chỉ lấy 5 thông báo mới nhất
        //                   .Select(n => new OrderNotificationViewModel
        //                   {
        //                       Id = n.Id,
        //                       OrderId = n.OrderId,
        //                       IsRead = n.IsRead,
        //                       CreatedAt = n.CreatedAt,
        //                       Message = $"Đơn hàng #{n.OrderId} của bạn đã được cập nhật.",
        //                       Title = "Cập nhật đơn hàng"
        //                   })
        //                   .ToListAsync();
        //}
        public async Task<IActionResult> OrderDetailUser(int? orderId)
        {
            if (orderId == null)
            {
                return Json(new { status = false, Message = "Id không được để trống" });
            }
            try
            {
                var userIdString = User.FindFirstValue("UserId");
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                {
                    return Content("❌ Không tìm thấy thông tin User");
                }
                var order = await db.Orders.Include(o => o.OrderDetails)
                    .Where(o => o.OrderId == orderId).Select(n => new OrderViewModel
                    {
                        userName = n.User.FullName,
                        CreatedAt = n.CreatedAt,
                        UpdatedAt = n.UpdatedAt,
                        Address = n.ShippingAddress,
                        ShipDate = n.ShippedDate,
                        StatusName =n.Status.ToString(),
                        Amount = n.TotalAmount,
                        Phone = n.Phone,
                        Email = n.Email,
                        PaymentMethod = "Thanh toán "+n.PaymentStatus.ToString(),
                        FinalAmount = n.FinalAmount,
                        DiscountValue = n.DiscountValue,
                        OrderId = n.OrderId,
                        
                        Items = n.OrderDetails.Select(od => new OrderDetailItemViewModel
                        {
                            ProductId = od.ProductVariant.ProductId,
                            ProductVariantId = od.ProductVariantId,
                            image = od.ProductVariant.Product.Img,
                            ProductName = od.ProductVariant.Product.Name,
                            Size = od.ProductVariant.Size.SizeName,
                            Color = od.ProductVariant.Color.ColorName,
                            Description = od.ProductVariant.Product.Description,
                            Quantity = od.Quantity,
                            UnitPrice = (float)od.UnitPrice,
                            OrderDetailId = od.OrderDetailId,
                            // Check xem user đã review sản phẩm này trong orderDetail chưa
                            HasReview = db.Reviews.Any(r => r.UserId == userId && r.OrderDetailId == od.OrderDetailId)
                        }).ToList()
                    }).FirstOrDefaultAsync();
                return PartialView("OrderDetailUser", order);
            }
            catch (Exception ex)
            {
                return Content("⚠️ Lỗi: " + ex.Message);
            }
        }
        public async Task<IActionResult> getUnreadCount()
        {
            var userIdString = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                return Json(new { count = 0 });
            int unreadCount = await db.OrderNotification
            .CountAsync(n => !n.IsRead && n.UserId == userId);
            return Json(new { count = unreadCount });
        }
        [HttpGet]
        public async Task<IActionResult> ReadNotif(int? notifId)
        {
            // ✅ Lấy thông tin User hiện tại
            var userIdString = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin User." });

            if (notifId == null)
                return Json(new { success = false, message = "❌ Không tìm thấy thông báo." });

            // ✅ Tìm thông báo trong DB
            var notification = await db.OrderNotification
                .FirstOrDefaultAsync(n => n.UserId == userId && n.Id == notifId);

            if (notification == null)
                return Json(new { success = false, message = "⚠️ Không tìm thấy thông báo này." });
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await db.SaveChangesAsync();
            }
            var unreadCount = await db.OrderNotification
            .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { success = true, unreadCount });
        }


    }
}

