using EditProfileApp.Models;
using EditProfileApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace EditProfileApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly RadiusDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(RadiusDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "UserProfiles");
                }
                else
                {
                    return RedirectToAction("Edit", "UserProfiles", new { id = User.Identity.Name });
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                Radcheck user = _context.Radchecks.FirstOrDefault(u => u.Username == model.Username && u.Value == model.Password);

                if (user != null)
                {
                    string role = model.Username.Equals("AdminCE", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Student";

                    List<Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, role)
                    };

                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    if (role == "Admin")
                    {
                        return RedirectToAction("Index", "UserProfiles");
                    }
                    else
                    {
                        return RedirectToAction("Edit", "UserProfiles", new { id = user.Username });
                    }
                }

                ModelState.AddModelError("", "รหัสนักศึกษา หรือ รหัสผ่านไม่ถูกต้อง");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            Radcheck user = _context.Radchecks.FirstOrDefault(u => u.Username == model.StudentId);

            if (user != null)
            {
                Random random = new Random();
                string otp = random.Next(100000, 999999).ToString();

                HttpContext.Session.SetString("OTP", otp);
                HttpContext.Session.SetString("ResetStudentId", model.StudentId);

                string targetEmail = $"{model.StudentId}@kmitl.ac.th";
                string subject = "รหัสยืนยันการตั้งรหัสผ่านใหม่ (OTP)";
                string body = $"<h3>รหัสยืนยันของคุณคือ: <b style='color:red;'>{otp}</b></h3><p>รหัสนี้จะหมดอายุใน 5 นาที</p>";

                await _emailService.SendEmailAsync(targetEmail, subject, body);

                return RedirectToAction("ResetPassword");
            }

            ModelState.AddModelError("", "ไม่พบรหัสนักศึกษานี้ในระบบ");
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            string sessionOtp = HttpContext.Session.GetString("OTP");
            string sessionStudentId = HttpContext.Session.GetString("ResetStudentId");

            if (model.Otp != sessionOtp)
            {
                ModelState.AddModelError("", "รหัส OTP ไม่ถูกต้อง");
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "รหัสผ่านไม่ตรงกัน");
                return View(model);
            }

            Radcheck user = _context.Radchecks.FirstOrDefault(u => u.Username == sessionStudentId);
            if (user != null)
            {
                user.Value = model.NewPassword;
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("ResetStudentId");

                TempData["Success"] = "เปลี่ยนรหัสผ่านสำเร็จแล้ว กรุณาล็อกอิน";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendPasswordChangeOtp()
        {
            var username = User.Identity.Name;

            string targetEmail = $"{username}@kmitl.ac.th";

            Random rand = new Random();
            string otp = rand.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("ChangePassOTP", otp);
            HttpContext.Session.SetString("OtpExpiry", DateTime.UtcNow.AddMinutes(5).ToString());

            try
            {
                string subject = "รหัส OTP สำหรับเปลี่ยนรหัสผ่าน";
                string body = $"สวัสดีครับ,<br><br>รหัส OTP 6 หลักสำหรับการเปลี่ยนรหัสผ่านของคุณคือ: <b>{otp}</b><br><br>รหัสนี้จะหมดอายุภายใน 5 นาทีครับ";

                await _emailService.SendEmailAsync(targetEmail, subject, body);

                return Json(new { success = true, message = $"ระบบได้ส่ง OTP ไปที่อีเมล {targetEmail} แล้ว" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการส่งอีเมล: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> VerifyOtpAndChangePassword(string otp, string newPassword)
        {
            var username = User.Identity.Name;
            var storedOtp = HttpContext.Session.GetString("ChangePassOTP");
            var expiryStr = HttpContext.Session.GetString("OtpExpiry");

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != otp)
                return Json(new { success = false, message = "รหัส OTP ไม่ถูกต้อง" });

            if (DateTime.TryParse(expiryStr, out DateTime expiry) && DateTime.UtcNow > expiry)
                return Json(new { success = false, message = "รหัส OTP หมดอายุแล้ว กรุณากดขอใหม่" });

            var radcheck = await _context.Radchecks.FirstOrDefaultAsync(r => r.Username == username && r.Attribute == "Cleartext-Password");
            if (radcheck != null)
            {
                radcheck.Value = newPassword;
                _context.Update(radcheck);
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("ChangePassOTP");
                HttpContext.Session.Remove("OtpExpiry");

                return Json(new { success = true, message = "เปลี่ยนรหัสผ่านสำเร็จเรียบร้อยแล้ว!" });
            }

            return Json(new { success = false, message = "เกิดข้อผิดพลาด ไม่พบบัญชีในระบบ" });
        }
    }
}