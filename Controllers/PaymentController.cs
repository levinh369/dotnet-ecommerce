using MailKit.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Enums;
using ProjectTest1.Helpper;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Security.Claims;
using VNPAY.NET;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;

namespace ProjectTest1.Controllers
{
    public class PaymentController : Controller
    {
        private readonly EmailOrderHelper _emailService;
        private readonly IVnpay _vnpay;
        private readonly IConfiguration _configuration;
        private readonly DataContext db;
        private readonly IHubContext<OrderHub> _hubContext;
        public PaymentController(IVnpay vnpay, IConfiguration configuration, DataContext db, IHubContext<OrderHub> hubContext, EmailOrderHelper emailService)
        {
            _emailService = emailService;
            _hubContext = hubContext;
            _vnpay = vnpay;
            _configuration = configuration;
            this.db = db;
            _vnpay.Initialize(_configuration["Vnpay:TmnCode"], _configuration["Vnpay:HashSecret"], _configuration["Vnpay:BaseUrl"], _configuration["Vnpay:CallbackUrl"]);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(string paymentMethod, string OrderDescription, OrderViewModel model)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value);
                var fullName = User.FindFirstValue(ClaimTypes.Name);

                // Lấy giỏ hàng user
                var cartItems = await db.CartItemModels
                    .Include(c => c.ProductVariant).ThenInclude(pv => pv.Product)
                    .Where(c => c.Cart.UserId == userId)
                    .ToListAsync();

                if (!cartItems.Any())
                    return RedirectToAction("Index", "Cart");

                // Kiểm tra order đang chờ
                var pendingOrder = await db.Orders
                    .FirstOrDefaultAsync(o => o.UserId == userId &&
                                              o.Status == Enums.StatusOrderEnum.Pending &&
                                              o.PaymentStatus != Enums.StatusPaymentEnum.COD);

                OrderModel order;
                if (pendingOrder != null)
                {
                    order = pendingOrder;
                    order.TotalAmount = model.Amount;
                    order.DiscountValue = model.DiscountValue;
                    order.FinalAmount = model.FinalAmount;
                    order.VoucherId = model.VoucherId;
                    order.Phone = model.Phone;
                    order.Email = model.Email;
                    order.CreatedAt = DateTime.Now;

                    // Xóa chi tiết cũ
                    db.OrderDetails.RemoveRange(db.OrderDetails.Where(d => d.OrderId == order.OrderId));
                }
                else
                {
                    var fullAddress = $"{model.Address}, {model.shippingWardName}, {model.shippingDistrictName}, {model.shippingProvinceName}";
                    order = new OrderModel
                    {
                        UserId = userId,
                        ShippingAddress = fullAddress,
                        TotalAmount = model.Amount,
                        DiscountValue = model.DiscountValue,
                        FinalAmount = model.FinalAmount,
                        VoucherId = model.SelectedVoucherId,
                        Phone = model.Phone,
                        Email = model.Email,
                        Status = Enums.StatusOrderEnum.Pending,
                        CreatedAt = DateTime.Now
                    };
                    await db.Orders.AddAsync(order);
                }

                // Lưu để có OrderId
                await db.SaveChangesAsync();

                // Thêm OrderDetails mới
                var totalQuantity = 0;
                foreach (var item in cartItems)
                {
                    totalQuantity += item.Quantity;
                    await db.OrderDetails.AddAsync(new OrderDetailModel
                    {
                        OrderId = order.OrderId,
                        ProductVariantId = item.ProductVariantId,
                        Quantity = item.Quantity,
                        UnitPrice = item.ProductVariant.Product.Price
                    });
                }

                await db.SaveChangesAsync();

                // Xử lý phương thức thanh toán
                if (paymentMethod == "VNPAY")
                {
                    order.PaymentStatus = StatusPaymentEnum.VNPAY;
                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    var ipAddress = NetworkHelper.GetIpAddress(HttpContext);
                    var request = new PaymentRequest
                    {
                        PaymentId = order.OrderId,
                        Money = model.FinalAmount,
                        Description = OrderDescription,
                        IpAddress = ipAddress
                    };

                    var paymentUrl = _vnpay.GetPaymentUrl(request);
                    return Redirect(paymentUrl);
                }
                else if (paymentMethod == "COD")
                {
                    order.Status = Enums.StatusOrderEnum.Pending;
                    order.PaymentStatus = StatusPaymentEnum.COD;

                    // Cập nhật tồn kho sản phẩm
                    foreach (var detail in await db.OrderDetails.Where(d => d.OrderId == order.OrderId).ToListAsync())
                    {
                        var variant = await db.ProductVariants.FindAsync(detail.ProductVariantId);
                        if (variant != null)
                        {
                            variant.StockQuantity = Math.Max(0, variant.StockQuantity - detail.Quantity);
                        }
                    }

                    // ✅ Giảm số lượng voucher user
                    if (order.VoucherId != null)
                    {
                        var userVoucher = await db.UserVouchers
                            .FirstOrDefaultAsync(uv => uv.UserId == order.UserId && uv.VoucherId == order.VoucherId && uv.Used!=true);
                        userVoucher.Used=true;
                        
                    }
                    // ✅ Xóa giỏ hàng
                    db.CartItemModels.RemoveRange(cartItems);

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["PaymentStatus"] = "success";
                    TempData["Message"] = "Thanh toán thành công!";

                    // 🔔 Gửi thông báo cho admin
                    var adminUser = await db.User.FirstOrDefaultAsync(u => u.Role == "Admin");
                    var hubContext = HttpContext.RequestServices.GetService(typeof(IHubContext<OrderHub>)) as IHubContext<OrderHub>;

                    if (hubContext != null && adminUser != null)
                    {
                        var orderNotification = new OrderNotificationModel
                        {
                            OrderId = order.OrderId,
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            UserId = adminUser.UserId,
                            Message = $"Khách hàng {fullName} vừa tạo đơn #{order.OrderId}. Tổng tiền: {model.FinalAmount:N0}₫.",
                            Title = "Đơn hàng mới từ khách hàng",
                            Type = NotificationType.Order,
                            Url = $"/Order/Detail/{order.OrderId}"
                        };

                        await db.OrderNotification.AddAsync(orderNotification);
                        await db.SaveChangesAsync();

                        var unreadCount = db.OrderNotification.Count(n => !n.IsRead && n.UserId == null);
                        await hubContext.Clients.All.SendAsync("ReceiveOrderNotification",
                            order.OrderId,
                            DateTime.Now.ToString("hh:mm tt, MMM dd, yyyy"),
                            unreadCount,
                            orderNotification.Id,
                            order.TotalAmount,
                            totalQuantity,
                            orderNotification.Title,
                            orderNotification.Message);
                    }

                    return RedirectToAction("PaymentResult");
                }

                return BadRequest("Phương thức thanh toán không hợp lệ.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("❌ Lỗi CreatePayment: " + ex.Message);
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Callback()
        {
            if (!Request.QueryString.HasValue)
                return NotFound("Không tìm thấy thông tin thanh toán.");
            int totalProducts = 0;
            double? totalPrice = 0;
            try
            {
                var paymentResult = _vnpay.GetPaymentResult(Request.Query);
                if (paymentResult == null)
                    return BadRequest("Không lấy được kết quả thanh toán.");

                var resultDescription = $"{paymentResult.Description}. {paymentResult.Description}.";
                var orderId = paymentResult.PaymentId;

                // Lấy order từ DB (await!)

                var order = await db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                    return NotFound("Đơn hàng không tồn tại.");
                var fullName = User.FindFirstValue(ClaimTypes.Name);
                if (paymentResult.IsSuccess)
                {
                    order.Status = Enums.StatusOrderEnum.Confirmed;
                    await db.SaveChangesAsync();
                    //var orderViewModel = await GetOrderViewModelAsync(orderId);
                    //await _emailService.SendOrderConfirmationAsync(order.Email, orderViewModel);
                    //order.CreatedAt = DateTime.Now;
                    if (order.VoucherId != null)
                    {
                       
                        var userVoucher = await db.UserVouchers
                            .FirstOrDefaultAsync(uv => uv.UserId == order.UserId && uv.VoucherId == order.VoucherId && uv.Used!=true);
                        userVoucher.Used = true;
                        await db.SaveChangesAsync();
                    }
                    var orderDetails = await db.OrderDetails
                        .Where(d => d.OrderId == order.OrderId)
                        .ToListAsync();
                    totalProducts = orderDetails.Sum(d => d.Quantity);
                    totalPrice = orderDetails.Sum(d => d.Quantity * d.UnitPrice);
                    foreach (var detail in orderDetails)
                    {
                        var variant = await db.ProductVariants
                            .FirstOrDefaultAsync(v => v.ProductVariantId == detail.ProductVariantId);

                        if (variant != null)
                        {
                            variant.StockQuantity -= detail.Quantity;
                            if (variant.StockQuantity < 0) variant.StockQuantity = 0;
                        }
                    }

                    var cart = await db.CartModels.FirstOrDefaultAsync(c => c.UserId == order.UserId);
                    if (cart != null)
                    {
                        db.CartModels.Remove(cart);
                    }
                    await db.SaveChangesAsync();
                    TempData["PaymentStatus"] = "success";
                    TempData["Message"] = "Thanh toán thành công!";
                }

                else
                {
                    order.Status = Enums.StatusOrderEnum.Cancelled;
                    await db.SaveChangesAsync();
                    TempData["PaymentStatus"] = "error";
                    TempData["Message"] = "Thanh toán không thành công!";
                }
                var adminUser = await db.User
                .Where(u => u.Role == "Admin")
                .FirstOrDefaultAsync();
                var hubContext = HttpContext.RequestServices.GetService(typeof(IHubContext<OrderHub>)) as IHubContext<OrderHub>;
                if (hubContext != null)
                {
                    var orderNotification = new OrderNotificationModel
                    {
                        OrderId = order.OrderId,
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        UserId = adminUser.UserId,
                        Message = $"Khách hàng {fullName} vừa tạo đơn #{order.OrderId}. Tổng tiền: {order.FinalAmount:N0}₫.",
                        Title = "Đơn hàng mới từ khách hàng ",
                        Type = NotificationType.Order,
                        Url = $"/Order/Detail/{order.OrderId}"
                    };
                    db.OrderNotification.Add(orderNotification);
                    await db.SaveChangesAsync();
                    int unreadCount = db.OrderNotification.Count(n => !n.IsRead && n.UserId ==null); 
                    await hubContext.Clients.All.SendAsync("ReceiveOrderNotification", order.OrderId, DateTime.Now.ToString("hh:mm tt, MMM dd, yyyy"), unreadCount, orderNotification.Id, totalPrice, totalProducts,orderNotification.Title,orderNotification.Message);
                }
                return RedirectToAction("PaymentResult");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        public IActionResult PaymentResult()
        {

            return View();
        }
        public async Task<OrderViewModel?> GetOrderViewModelAsync(long orderId)
        {
            var orders = await db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant.Color)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant.Size)
                .Where(o => o.OrderId == orderId)
                .ToListAsync();

            var order = orders.FirstOrDefault();
            if (order == null) return null;

            return new OrderViewModel
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                userName = order.User.FullName,
                Email = order.User.Email,
                Phone = order.User.Phone,
                CreatedAt = order.CreatedAt,
                ShippingAddress = order.ShippingAddress,
                Amount = order.TotalAmount,
                Status = order.Status,
                Items = order.OrderDetails.Select(d => new OrderDetailItemViewModel
                {
                    ProductName = d.ProductVariant?.Product?.Name ?? "",
                    image = d.ProductVariant?.Product?.Img ?? "",
                    Color = d.ProductVariant?.Color?.ColorName ?? "",
                    Size = d.ProductVariant?.Size?.SizeName ?? "",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            };
        }

    }

}
