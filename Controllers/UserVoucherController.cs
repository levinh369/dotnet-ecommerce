using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using ProjectTest1.Enums;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Security.Claims;

namespace ProjectTest1.Controllers
{
    public class UserVoucherController : Controller
    {
        private readonly DataContext db;
        public UserVoucherController(DataContext db)
        {
            this.db = db;
        }
        public async Task<IActionResult> Index()
        {
            var vouchers = await db.Vouchers.Where(v=>v.Status!=Enums.VoucherStatus.Deleted|| v.Status!=Enums.VoucherStatus.Expired)
                .OrderByDescending(v => v.CreatedAt)
                 .Select(v => new VoucherViewModel
                 {
                     Id = v.Id,
                     Code = v.Code,
                     Name = v.Name,
                     DiscountValue = v.DiscountValue,
                     DiscountType = v.DiscountType,
                     Description = v.Description,
                     MinOrderValue = v.MinOrderValue,
                     UsedCount = v.UsedCount,
                     UsageLimit = v.UsageLimit,
                     CreatedAt = v.CreatedAt,
                     StartDate = v.StartDate,
                     EndDate = v.EndDate,
                     Status = v.Status
                 })
                .ToListAsync();
            return View(vouchers);
        }
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var userIdString = User.FindFirstValue("UserId");
            Guid? userId = null;
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out Guid uid))
            {
                userId = uid;
            }

            var vouchers = await db.Vouchers
            .Where(v => v.Status != Enums.VoucherStatus.Deleted && v.Status != Enums.VoucherStatus.Expired)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VoucherViewModel
            {
                Id = v.Id,
                Code = v.Code,
                Name = v.Name,
                DiscountValue = v.DiscountValue,
                DiscountType = v.DiscountType,
                Description = v.Description,
                MinOrderValue = v.MinOrderValue,
                Type = v.Type,
                UsedCount = v.UsedCount,
                UsageLimit = v.UsageLimit,
                CreatedAt = v.CreatedAt,
                StartDate = v.StartDate,
                EndDate = v.EndDate,
                Status = v.Status,
                MaxPerUser = v.MaxPerUser,
                PointCost = v.PointCost,
                // số lượng voucher user này đã nhận
                UserClaimedCount = userId != null
                    ? db.UserVouchers.Count(uv => uv.VoucherId == v.Id && uv.UserId == userId && uv.Used==false)
                    : 0
                })
                .ToListAsync();
            var userPoint = await db.UserPoints
            .FirstOrDefaultAsync(p => p.UserId == userId);

            ViewBag.TotalPoints = userPoint?.TotalPoints ?? 0;

            return PartialView(vouchers);
        }
        [HttpPost]
        public async Task<IActionResult> UserVoucher(int voucherId)
        {
            if (voucherId == 0)
                return Json(new { success = false, message = "❌ Voucher không tồn tại." });

            try
            {
                var userIdString = User.FindFirstValue("UserId");
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });

                var voucher = await db.Vouchers.FindAsync(voucherId);
                if (voucher == null)
                    return Json(new { success = false, message = "Voucher không tồn tại." });

                if (voucher.UsageLimit > 0 && voucher.UsedCount >= voucher.UsageLimit)
                    return Json(new { success = false, message = "Voucher đã hết lượt nhận!" });

                // ✅ Transaction đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var userVoucher = new UserVoucherModel
                        {
                            UserId = userId,
                            VoucherId = voucherId,
                            ClaimedAt = DateTime.Now,
                            Claimed = true,
                            Used = false,
                            ExpiredAt = voucher.EndDate
                        };

                        voucher.UsedCount += 1;

                        db.UserVouchers.Add(userVoucher);
                        await db.SaveChangesAsync();

                        await transaction.CommitAsync(); // ✅ Xác nhận
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(); // ❌ Hoàn tác nếu lỗi
                        throw; // ném lại lỗi để catch ngoài xử lý
                    }
                }

                return Json(new { success = true, message = "🎉 Nhận voucher thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "⚠️ Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UseVoucher(int voucherId)
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Json(new { success = false, message = "Bạn chưa đăng nhập" });

            var voucher = await db.Vouchers.FindAsync(voucherId);
            if (voucher == null || voucher.Status == Enums.VoucherStatus.Deleted || voucher.Status == Enums.VoucherStatus.Expired)
                return Json(new { success = false, message = "Voucher không tồn tại hoặc hết hạn" });

            var claimedCount = await db.UserVouchers.CountAsync(uv => uv.VoucherId == voucherId && uv.UserId == userId);
            if (claimedCount >= voucher.UsageLimit)
                return Json(new { success = false, message = "Bạn đã nhận đủ voucher này" });

            // Tính tổng tiền giỏ hàng
            float totalPrice = await db.CartItemModels
             .Where(ci => ci.Cart.UserId == userId)
             .Select(ci => (float?)ci.ProductVariant.Product.Price * ci.Quantity)
             .SumAsync() ?? 0f;
            // Áp dụng voucher
            float discountAmount = 0;
            if (voucher.DiscountType == DiscountType.Percent)
                discountAmount = totalPrice * voucher.DiscountValue / 100;
            else
                discountAmount = voucher.DiscountValue;

            var finalPrice = Math.Max(totalPrice - discountAmount, 0);

            return Json(new
            {
                success = true,
                Message = "Áp dụng voucher thành công",
                totalPrice = totalPrice,
                discountAmount = discountAmount,
                finalPrice = finalPrice
            });
        }
        [HttpGet]
        public async Task<IActionResult> Detail(int voucherId)
        {
            if (voucherId == 0)
                return Json(new { success = false, message = "❌ Id không dược để trống." });
            var voucher = await db.Vouchers.FirstOrDefaultAsync(v=>v.Id==voucherId);
            if(voucher==null)
                return Json(new { success = false, message = "❌ Voucher không tồn tại." });
            var model = new VoucherViewModel
            {
                Id = voucher.Id,
                Code = voucher.Code,
                Name = voucher.Name,
                DiscountValue = voucher.DiscountValue,
                DiscountType = voucher.DiscountType,
                Description = voucher.Description,
                MinOrderValue = voucher.MinOrderValue,
                Type = voucher.Type,
                UsedCount = voucher.UsedCount,
                UsageLimit = voucher.UsageLimit,
                CreatedAt = voucher.CreatedAt,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                Status = voucher.Status,
                MaxPerUser = voucher.MaxPerUser,
                PointCost = voucher.PointCost
            };
            return PartialView("Detail", model);
        }
        [HttpPost]
        public async Task<IActionResult> ExchangePointsForVoucher(int voucherId)
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Json(new { success = false, message = "❌ Bạn chưa đăng nhập." });

            if (voucherId == 0)
                return Json(new { success = false, message = "❌ ID voucher không hợp lệ." });

            // 👉 Lấy từ bảng Voucher
            var Voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId);

            if (Voucher == null)
                return Json(new { success = false, message = "❌ Voucher này không tồn tại " });
            int pointCost = Voucher.PointCost ?? 0;
            if (pointCost <= 0)
                return Json(new { success = false, message = "❌ Voucher này không thể đổi bằng điểm." });

            var userPoint = await db.UserPoints.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userPoint == null)
                return Json(new { success = false, message = "❌ Bạn chưa có điểm tích lũy." });

            if (userPoint.TotalPoints < pointCost)
                return Json(new { success = false, message = "❌ Bạn không đủ điểm để đổi voucher này." });
            int userVoucherCount = await db.UserVouchers
            .CountAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);

            if (userVoucherCount >= Voucher.MaxPerUser)
            {
                return Json(new
                {
                    success = false,
                    message = $"⚠️ Bạn chỉ có thể đổi tối đa {Voucher.MaxPerUser} lần voucher này."
                });
            }
            // ✅ Trừ điểm và cập nhật trạng thái
            userPoint.TotalPoints -= pointCost;
            var userVoucher = new UserVoucherModel
            {
                UserId = userId,
                VoucherId = Voucher.Id,
                Used = false,
                Claimed = true,
                ClaimedAt = DateTime.Now,
                ExpiredAt = Voucher.EndDate,
            };
            Voucher.UsedCount += 1;
            db.UserVouchers.Add(userVoucher);
            await db.SaveChangesAsync();
            return Json(new { success = true, message = "🎁 Đổi voucher thành công!" });
        }



    }
}
