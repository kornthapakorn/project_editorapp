using Microsoft.AspNetCore.Mvc;
using EditProfileApp.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using EditProfileApp.Services;
using Microsoft.AspNetCore.Http;


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
    }
}