using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Helpper;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Net.Security;
using System.Security.Claims;

namespace ProjectTest1.Controllers
{
    
    public class AccountController : Controller
    {
        private readonly DataContext db;
        private readonly CloudinaryHelper cloudinaryService;
        public AccountController(DataContext db, CloudinaryHelper cloudinaryService)
        {
            this.db = db;
            this.cloudinaryService = cloudinaryService;
        }
        
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = db.User.FirstOrDefault(u => u.Email == model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng!");
                return View(model);
            }
            var claims = new List<Claim>
        {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? ""),
                new Claim("AvatarUrl", user.AvatarUrl ?? "")
        };
            // Tạo identity và principal
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Đăng nhập
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            if (!string.IsNullOrEmpty(user.Role) && user.Role.ToLower() == "admin")
            {
                return RedirectToAction("Index", "Admin"); // Controller Admin, Action Index
            }
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // 🔍 Kiểm tra email tồn tại (async)
            var user = await db.User.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user != null)
            {
                ModelState.AddModelError(string.Empty, "Email đã tồn tại!");
                return View(model);
            }

            // 🔐 Tạo user mới
            var newUser = new UserModel
            {
                UserId = Guid.NewGuid(),
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FullName = model.FullName,
                Phone = model.Phone,
                CreatedAt = DateTime.Now,
                IsEmailConfirmed = false,
                AvatarUrl = "https://res.cloudinary.com/dx7f8zj2u/image/upload/v1696546893/DefaultAvatar_yzqv3r.png"
            };

            await db.User.AddAsync(newUser); // 👈 thêm async
            await db.SaveChangesAsync();     // 👈 lưu async

            return RedirectToAction("Login", "Account");
        }
        [Authorize]
        [HttpGet]
        public IActionResult changePassWord()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var model = new ChangePassWordViewModel
            {
                Email = email
            };
            return PartialView(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> changePassWord(ChangePassWordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }
            var user = db.User.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                return Json(new { success = false, message = "Email không tồn tại!" });
            }
            bool isValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash);
            if (!isValid)
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng!" });
            }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassWord);
            db.Update(user);
            await db.SaveChangesAsync();
            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await db.User.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError(String.Empty, "Email không tồn tại trong hệ thống.");
                return View(model);
            }

            // Tạo token tạm thời (đơn giản hoặc dùng Guid)
            var token = Guid.NewGuid().ToString();

            // Lưu token & thời gian hết hạn (ví dụ 30 phút)
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddMinutes(3);
            await db.SaveChangesAsync();

            // Tạo URL reset
            var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token = token }, Request.Scheme);

            // Gửi email
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "bạn";

            var body = $"Hello {userName},<br>" +
                       $"Nhấn vào liên kết sau để đặt lại mật khẩu: <a href='{resetLink}'>Đặt lại mật khẩu</a>";
            await EmailHelper.SendEmailAsync(user.Email, "Đặt lại mật khẩu", body);

            ViewBag.Message = "Liên kết đặt lại mật khẩu đã được gửi tới email của bạn.";
            return View("ForgotPassword", model);

        }
        [HttpGet]
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = db.User.FirstOrDefault(u =>
                u.Email == model.Email &&
                u.ResetToken == model.Token &&
                u.ResetTokenExpiry > DateTime.Now);

            if (user == null)
            {
                ModelState.AddModelError("", "Token không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Reset mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            // Xóa token sau khi sử dụng
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            db.Update(user);
            await db.SaveChangesAsync();

            ViewBag.SuccessMessage = "Mật khẩu đã được đặt lại thành công!";
            return View("ResetPassword");
        }
        public ActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> UserAccount()
        {
            var userIdString = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin User." });

            var user = await db.User.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return Json(new { success = false, message = "❌ Không tìm thấy thông tin User." });

            var userVm = new UserViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl,
                ProvinceId = user.ProvinceId,
                DistrictId = user.DistrictId,
                WardId = user.WardId,
            };

            return View(userVm);
        }
        [HttpPost]
        public async Task<IActionResult> EditUserAccount(UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }
            try
            {
                var user = await db.User.FirstOrDefaultAsync(u => u.UserId == model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }
                // Cập nhật thông tin người dùng
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.Email = model.Email;
                user.ProvinceId = model.ProvinceId;
                user.DistrictId = model.DistrictId;
                user.WardId = model.WardId;
                var Province = model.Province ?? "";
                var district = model.District ?? "";
                var ward = model.Ward ?? "";
                var addressDetail = model.addressDetail ?? "";
                var addressParts = new[] { model.addressDetail, model.Ward, model.District, model.Province }
                .Where(x => !string.IsNullOrWhiteSpace(x));

                user.Address = string.Join(", ", addressParts);

                if (model.MainImageFile != null && model.MainImageFile.Length > 0)
                {
                    var imageUrl = await cloudinaryService.UploadImageAsync(model.MainImageFile);
                    user.AvatarUrl = imageUrl;
                }

                db.Update(user);
                await db.SaveChangesAsync();
                var claims = new List<Claim>
                {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? ""),
                new Claim("AvatarUrl", user.AvatarUrl ?? "")
                 };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)
                );
                return Json(new
                {
                    success = true,
                    message = "Cập nhật thông tin thành công!",
                    avatarUrl = user.AvatarUrl
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi cập nhật tài khoản: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi trong quá trình cập nhật. Vui lòng thử lại sau!" });
            }
        }

    }


}
