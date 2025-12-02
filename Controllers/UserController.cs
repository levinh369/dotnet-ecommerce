using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Helpper;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;

namespace ProjectTest1.Controllers
{
    public class UserController : Controller
    {
        private readonly DataContext db;
        private readonly IHubContext<OrderHub> _hubContext;
        public UserController(DataContext db, CloudinaryHelper _cloudinaryService, IHubContext<OrderHub> _hubContext)
        {
            this.db = db;
            this._hubContext = _hubContext;
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }
       

        public async Task<IActionResult> ListData(int page = 1, int pageSize = 6, int? Status = 0, string FullName = "", DateTime? FromDate = null, DateTime? ToDate = null, bool check = true)
        {
            ViewBag.page = page;
            ViewBag.pageSize = pageSize;
            ViewBag.Check = check;
            var query = check
            ? db.User.AsNoTracking().Where(u => u.isDeleted == false).AsQueryable()
            : db.User.AsNoTracking().Where(u => u.isDeleted == true).AsQueryable();
            
            if (!string.IsNullOrEmpty(FullName))
            {
                query = query.Where(c => c.FullName.Contains(FullName));

            }
            if(Status==1)
            {
                query = query.Where(c => c.IsActive == true);
            }
            else if(Status==2)
            {
                query = query.Where(c => c.IsActive == false);
            }
            if (FromDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt >= FromDate.Value);
            }
            if (ToDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt <= ToDate.Value);
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
                .Select(c => new UserViewModel
                {
                    UserId = c.UserId,
                    FullName = c.FullName,
                    Role= c.Role,
                    Email = c.Email,
                    Phone = c.Phone,
                    IsActive = c.IsActive,
                    Address = c.Address,
                    CreatedAt = c.CreatedAt,
                    AvatarUrl = c.AvatarUrl
                }).ToListAsync();
            return PartialView(listData);
        }
        public async Task<IActionResult> Detail(Guid userId)
        {
            if(userId == Guid.Empty)
            {
                return Json(new { success = false, message = "Id không tồn tại." });
            }
            try
            {
                var user = await db.User.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }
                var model = new UserViewModel
                {
                    UserId = userId,
                    Phone = user.Phone,
                    IsActive = user.IsActive,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    CreatedAt = user.CreatedAt,
                    IsDelete = user.isDeleted,
                    Address = user.Address,
                    Role = user.Role,
                    FullName = user.FullName,
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
        public async Task<IActionResult> ChangeStatus(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return Json(new { success = false, message = "Id không tồn tại." });
            }
            try
            {
                var user = await db.User.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }
                user.IsActive = !user.IsActive;
                await db.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Cập nhật trạng thái thành công.",
                    status = user.IsActive
                });
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
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return Json(new { success = false, message = "Id không hợp lệ." });
            }

            try
            {
                var user = await db.User.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Xóa mềm
                user.isDeleted = true;
                user.IsActive = false; // cũng vô hiệu hóa luôn

                db.User.Update(user);
                await db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Xóa người dùng thành công.",
                    status = user.IsActive
                });
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
        public async Task<IActionResult> RestoreUser(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return Json(new { success = false, message = "Id không tồn tại." });
            }
            try
            {
                var user = await db.User.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }
                user.isDeleted = false;
                user.IsActive = true;
                await db.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Khôi phục người dùng thành công.",
                    status = user.IsActive
                });
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

    }
}
