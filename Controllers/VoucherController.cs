using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Helpper;
using ProjectTest1.Migrations;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;

namespace ProjectTest1.Controllers
{
    public class VoucherController : Controller
    {
        private readonly DataContext db;
        public VoucherController(DataContext db)
        {
            this.db = db;
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }
        public async Task<PartialViewResult> ListData(int page = 1, int pageSize = 6, int? status = -1, int? typeDiscount = 0, string? keySearch = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            ViewBag.page = page;
            ViewBag.pageSize = pageSize;
            var query = db.Vouchers.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(keySearch))
            {
                query = query.Where(c => c.Name.Contains(keySearch) || c.Code.Contains(keySearch));

            }
            if (fromDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt <= toDate.Value);
            }
            if(status == 0)
            {
                query = query.Where(v => v.Status == Enums.VoucherStatus.Draft);
            }
            else if (status == 1)
            {
                query = query.Where(v => v.Status == Enums.VoucherStatus.Active);
            }
            else if (status == 2)
            {
                query = query.Where(v => v.Status == Enums.VoucherStatus.Inactive);
            }
            else if (status == 3)
            {
                query = query.Where(v => v.Status == Enums.VoucherStatus.Expired);
            }
            else if (status == 4)
            {
                query = query.Where(v => v.Status == Enums.VoucherStatus.Exhausted);
            }
            else if (status == 5)
            {
                query = query.Where(v => v.Status == Enums.VoucherStatus.Deleted);
            }
            if (typeDiscount == 1)
            {
                query = query.Where(v => v.DiscountType == Enums.DiscountType.Fixed);
            }
            else if(typeDiscount == 2)
            {
                query = query.Where(v => v.DiscountType == Enums.DiscountType.Percent);
            }
            var total = await query.CountAsync();
            ViewBag.total = total;
            ViewBag.totalPage = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.stt = (page - 1) * pageSize;

            // Lấy dữ liệu theo trang, đồng thời project sang ViewModel
            var listData = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new VoucherViewModel
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Description = c.Description,
                    Type = c.Type,
                    DiscountType = c.DiscountType,
                    Status = c.Status,
                    DiscountValue = c.DiscountValue,
                    MinOrderValue = c.MinOrderValue,
                    UsedCount = c.UsedCount,
                    UsageLimit = c.UsageLimit,
                    ExpiryDate = c.EndDate.HasValue ? c.EndDate.Value : DateTime.MaxValue,
                    CreatedAt = c.CreatedAt,
                    MaxPerUser = c.MaxPerUser,
                }).ToListAsync();
            return PartialView(listData);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VoucherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            try
            {
                var voucher = new VoucherModel
                {
                    Code = model.Code,
                    Name = model.Name,
                    Description = model.Description,
                    Type = model.Type,
                    DiscountType = model.DiscountType,
                    Status = model.Status,
                    DiscountValue = model.DiscountValue,
                    MinOrderValue = model.MinOrderValue,
                    UsageLimit = model.UsageLimit,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    CreatedAt = DateTime.Now,
                    MaxPerUser = model.MaxPerUser,
                    PointCost = model.PointCost
                };
                db.Vouchers.Add(voucher);
                await db.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Thêm mã giảm giá thành công",

                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult Detail(int? voucherId)
        {
            if (voucherId == null || voucherId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Bản ghi không tồn tại hoặc đã bị xóa."
                });
            }

            try
            {
                var voucher = db.Vouchers
                    .AsNoTracking()
                    .FirstOrDefault(v => v.Id == voucherId);

                if (voucher == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy voucher tương ứng."
                    });
                }

                var model = new VoucherViewModel
                {
                    Id = voucher.Id,
                    Code = voucher.Code,
                    Name = voucher.Name,
                    Description = voucher.Description,
                    Type = voucher.Type,
                    DiscountType = voucher.DiscountType,
                    Status = voucher.Status,
                    DiscountValue = voucher.DiscountValue,
                    MinOrderValue = voucher.MinOrderValue,
                    UsedCount = voucher.UsedCount,
                    UsageLimit = voucher.UsageLimit,
                    ExpiryDate = voucher.EndDate ?? DateTime.MaxValue,
                    CreatedAt = voucher.CreatedAt,
                    StartDate = voucher.StartDate,
                    EndDate = voucher.EndDate,
                    MaxPerUser = voucher.MaxPerUser,
                };
                return PartialView("Detail", model);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Đã có lỗi xảy ra: " + ex.Message
                });
            }
        }
        [HttpGet]
        public IActionResult Edit(int? voucherId)
        {
            if (voucherId == null || voucherId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Bản ghi không tồn tại hoặc đã bị xóa."
                });
            }

            try
            {
                var voucher = db.Vouchers
                    .AsNoTracking()
                    .FirstOrDefault(v => v.Id == voucherId);

                if (voucher == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy voucher tương ứng."
                    });
                }

                var model = new VoucherViewModel
                {
                    Id = voucher.Id,
                    Code = voucher.Code,
                    Name = voucher.Name,
                    Description = voucher.Description,
                    Type = voucher.Type,
                    DiscountType = voucher.DiscountType,
                    Status = voucher.Status,
                    DiscountValue = voucher.DiscountValue,
                    MinOrderValue = voucher.MinOrderValue,
                    UsedCount = voucher.UsedCount,
                    UsageLimit = voucher.UsageLimit,
                    ExpiryDate = voucher.EndDate ?? DateTime.MaxValue,
                    CreatedAt = voucher.CreatedAt,
                    StartDate = voucher.StartDate,
                    EndDate = voucher.EndDate,
                    MaxPerUser = voucher.MaxPerUser,
                };
                return PartialView("Edit", model);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Đã có lỗi xảy ra: " + ex.Message
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VoucherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .Select(ms => new
                    {
                        Field = ms.Key,
                        Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    });

                return Json(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ.",
                    details = errors
                });
            }

            try
            {
                var voucher = await db.Vouchers.FindAsync(model.Id);
                if (voucher == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
                }
                voucher.Code = model.Code;
                voucher.Name = model.Name;
                voucher.Description = model.Description;
                voucher.Type = model.Type;
                voucher.DiscountType = model.DiscountType;
                voucher.Status = model.Status;
                voucher.DiscountValue = model.DiscountValue;
                voucher.MinOrderValue = model.MinOrderValue;
                voucher.UsageLimit = model.UsageLimit;
                voucher.StartDate = model.StartDate;
                voucher.EndDate = model.EndDate;
                voucher.MaxPerUser = model.MaxPerUser;
                db.Vouchers.Update(voucher);
                await db.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Cập nhật mã giảm giá thành công",
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
