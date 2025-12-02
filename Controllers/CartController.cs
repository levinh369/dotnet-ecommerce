using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ProjectTest1.Controllers
{
    public class CartController : Controller
    {
        private readonly DataContext db;
        public CartController(DataContext db)
        {
            this.db = db;
        }
        public IActionResult Index()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (userIdStr == null)
            {
                // Nếu chưa login mà vào trực tiếp /Cart -> chuyển luôn sang Login
                return RedirectToAction("Login", "Account");
            }

            return View();
        }
        public async Task<IActionResult> Checkout()
        {
            var userIdString = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var vouchers = await db.UserVouchers
            .Include(uv => uv.Voucher)

            .Where(uv =>
                uv.UserId == userId &&
                uv.Used == false &&
                uv.Claimed==true &&
                uv.Voucher.Status == Enums.VoucherStatus.Active &&
                (uv.Voucher.EndDate == null || uv.Voucher.EndDate > DateTime.Now)
            ).GroupBy(uv => uv.VoucherId)
            .Select(group => new VoucherViewModel
            {
                Id = group.Key, // VoucherId
                Name = group.FirstOrDefault().Voucher.Name,
                Code = group.FirstOrDefault().Voucher.Code,
                DiscountValue = group.FirstOrDefault().Voucher.DiscountValue,
                DiscountType = group.FirstOrDefault().Voucher.DiscountType,
                UserClaimedCount = group.Count(),
                UsageLimit = group.FirstOrDefault().Voucher.UsageLimit,
                MinOrderValue = group.FirstOrDefault().Voucher.MinOrderValue
            })
            .ToListAsync();
            // Lấy tổng giá của giỏ hàng user
            float totalPrice = (float)(await db.CartItemModels
            .Where(ci => ci.Cart.UserId == userId)
            .Select(ci => (decimal?)ci.ProductVariant.Product.Price * ci.Quantity)
            .SumAsync() ?? 0m);
            // Lấy thông tin user hiện tại
            var user = await db.User
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.ProvinceId,
                    u.DistrictId,
                    u.WardId,
                    u.Address,
                    u.FullName,
                    u.Phone,
                    u.Email
                })
                .FirstOrDefaultAsync();

            var model = new OrderViewModel
            {
                Vouchers = vouchers,
                Amount = totalPrice,
                ProvinceId = user?.ProvinceId,
                DistrictId = user?.DistrictId,
                WardId = user?.WardId,
                Address = user?.Address.Split(',').FirstOrDefault() ?? ""   ,
                userName = user?.FullName,
                Phone = user?.Phone,
                Email = user?.Email
            };

            return View(model);
        }

        public async Task<IActionResult> ListData(int page = 1, int pageSize = 5)
        {
            ViewBag.page = page;
            ViewBag.pageSize = pageSize;

            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, redirectUrl = Url.Action("Login", "Account") });
                }
                return RedirectToAction("Login", "Account");
            }

            var userId = Guid.Parse(userIdStr);

            // Query để lấy cart items của user
            var query = db.CartItemModels
                .Include(c => c.Cart)
                .Include(c => c.ProductVariant).ThenInclude(pv => pv.Size)
                .Include(c => c.ProductVariant).ThenInclude(pv => pv.Color)
                .Include(c => c.ProductVariant).ThenInclude(pv => pv.Product)
                .Where(c => c.Cart.UserId == userId);

            // Tổng số item (trước khi phân trang)
            var total = await query.CountAsync();
            ViewBag.total = total;
            ViewBag.totalPage = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.stt = (page - 1) * pageSize;

            // Lấy dữ liệu có phân trang
            var cartItems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map sang ViewModel
            var cartViewModel = cartItems.Select(c => new CartItemViewModel
            {
                ProductVariantId = c.ProductVariantId,
                ProductName = c.ProductVariant.Product.Name,
                ImageUrl = c.ProductVariant.Product.Img,
                UnitPrice = c.ProductVariant.Product.Price,
                Quantity = c.Quantity,
                SizeName = c.ProductVariant.Size?.SizeName ?? "",
                ColorName = c.ProductVariant.Color?.ColorName ?? ""
            }).ToList();

            return PartialView(cartViewModel);
        }

        [HttpPost]
        public async Task<JsonResult> AddCart(CartItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { status = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                // Lấy userId từ Claim
                var userIdStr = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                {
                    return Json(new { status = false, message = "Người dùng chưa đăng nhập" });
                }

                // Tìm giỏ hàng theo user
                var cart = await db.CartModels
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new CartModel
                    {
                        UserId = userId,
                        CreatedDate = DateTime.Now,
                        CartItems = new List<CartItemModel>()
                    };
                    db.CartModels.Add(cart);
                }

                // Kiểm tra sản phẩm đã có trong giỏ chưa
                var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductVariantId == model.ProductVariantId);
                if (existingItem != null)
                {
                    existingItem.Quantity += model.Quantity;
                }
                else
                {
                    var variant = await db.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.ProductVariantId == model.ProductVariantId);

                    if (variant == null)
                        return Json(new { status = false, message = "Không tìm thấy sản phẩm" });

                    cart.CartItems.Add(new CartItemModel
                    {
                        ProductVariantId = model.ProductVariantId,
                        Quantity = model.Quantity,
                        UnitPrice = variant.Product.Price // hoặc variant.UnitPrice nếu bạn có
                    });
                }

                await db.SaveChangesAsync();

                return Json(new { status = true, message = "Thêm giỏ hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Có lỗi: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (userId == null) return Json(new { count = 0 });

            var count = db.CartItemModels
                          .Where(c => c.Cart.UserId == Guid.Parse(userId))
                          .Sum(c => c.Quantity);

            return Json(new { count });
        }
        [HttpGet] 
        public IActionResult deleteCart(int productVariantId)
        {
            if (productVariantId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng thử lại" });
            }
            try
            {
                var obj = db.CartItemModels
            .FirstOrDefault(p => p.ProductVariantId == productVariantId);
                if (obj == null)
                {
                    return Json(new { status = false, message = "Không tìm thấy bản ghi." });
                } 
                db.CartItemModels.Remove(obj);
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Json(new { status = false, message = "Không xóa được sản phẩm này" });
            }
            return Json(new { status = true, message = "Sản phẩm đã được xóa thành công" });
        }
        [HttpGet]
        public IActionResult changeQuantity(String operation, int productVariantId)
        {
            if (string.IsNullOrEmpty(operation) || productVariantId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng thử lại" });
            }
            
            try
            {
                var cartItem = db.CartItemModels
            .FirstOrDefault(c => c.ProductVariantId == productVariantId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                if (operation.Equals("+"))
                {
                    cartItem.Quantity += 1;
                }
                else if (operation.Equals("-"))
                {
                    if (cartItem.Quantity > 1)
                    {
                        cartItem.Quantity -= 1;
                    }
                    else
                    {
                        db.CartItemModels.Remove(cartItem);
                    }
                }
                    db.SaveChanges();
                return Json(new { status = true, message = "Cập nhật số lượng thành công" });
            }catch(Exception ex)
            {
                return Json(new { status = false, message = "Cập nhật số lượng không thành công" +ex.Message});
            }
                
            }

    }
}
