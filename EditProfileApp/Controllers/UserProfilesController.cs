using ClosedXML.Excel;
using EditProfileApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Cryptography; 
using System.Text;                  

namespace EditProfileApp.Controllers
{
    [Authorize]
    public class UserProfilesController : Controller
    {
        private readonly RadiusDbContext _context;

        public UserProfilesController(RadiusDbContext context)
        {
            _context = context;
        }

        private string HashPasswordSHA256(string rawPassword)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawPassword));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var students = from s in _context.UserProfiles
                           select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.StudentId.Contains(searchString)
                                       || s.FirstName.Contains(searchString)
                                       || s.LastName.Contains(searchString));
            }

            return View(await students.OrderBy(s => s.StudentId).ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            UserProfile userProfile = await _context.UserProfiles.FirstOrDefaultAsync(m => m.StudentId == id);

            if (userProfile == null) return NotFound();

            return View(userProfile);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,FirstName,LastName,Nickname,Email,Phone,EmergencyMobile,UpdatedAt")] UserProfile userProfile)
        {
            userProfile.Department = "วิศวกรรมคอมพิวเตอร์";
            ModelState.Remove("Department");

            if (ModelState.IsValid)
            {
                TimeZoneInfo thaiZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                userProfile.UpdatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, thaiZone);

                _context.Add(userProfile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(userProfile);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            if (User.Identity.Name != id && !User.IsInRole("Admin"))
            {
                return RedirectToAction("Edit", "UserProfiles", new { id = User.Identity.Name });
            }

            UserProfile userProfile = await _context.UserProfiles.FindAsync(id);
            if (userProfile == null) return NotFound();

            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("StudentId,Email,EmergencyMobile,FirstName,LastName,Nickname,Phone")] UserProfile userProfile)
        {
            TimeZoneInfo thaiZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime thaiTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, thaiZone);

            if (id != userProfile.StudentId) return NotFound();

            if (User.Identity.Name != id && !User.IsInRole("Admin"))
            {
                return RedirectToAction("Edit", "UserProfiles", new { id = User.Identity.Name });
            }
            userProfile.Department = "วิศวกรรมคอมพิวเตอร์";
            ModelState.Remove("Department");

            if (ModelState.IsValid)
            {
                try
                {
                    userProfile.UpdatedAt = thaiTime;

                    _context.Update(userProfile);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "อัปเดตข้อมูลโปรไฟล์ของคุณเรียบร้อยแล้ว";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserProfileExists(userProfile.StudentId)) return NotFound();
                    else throw;
                }

                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return RedirectToAction("Edit", new { id = userProfile.StudentId });
                }
            }
            return View(userProfile);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            UserProfile userProfile = await _context.UserProfiles.FirstOrDefaultAsync(m => m.StudentId == id);

            if (userProfile == null) return NotFound();

            return View(userProfile);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            UserProfile userProfile = await _context.UserProfiles.FindAsync(id);
            if (userProfile != null)
            {
                _context.UserProfiles.Remove(userProfile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using (XLWorkbook workbook = new XLWorkbook())
            {
                IXLWorksheet worksheet = workbook.Worksheets.Add("Template");

                worksheet.Cell(1, 1).Value = "StudentId";
                worksheet.Cell(1, 2).Value = "Password";

                var headerRow = worksheet.Range("A1:B1");
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                worksheet.Cell(2, 1).Value = "65xxxxxx";
                worksheet.Cell(2, 2).Value = "รหัสผ่านที่ต้องการตั้ง";
                worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
                worksheet.Cell(2, 2).Style.Font.FontColor = XLColor.Gray;

                worksheet.Columns().AdjustToContents();

                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ImportTemplate.xlsx");
                }
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            TimeZoneInfo thaiZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime thaiTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, thaiZone);

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "กรุณาเลือกไฟล์ Excel ก่อนกดนำเข้า";
                return RedirectToAction(nameof(Index));
            }

            int successCount = 0;
            int skipCount = 0;

            using (MemoryStream stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (XLWorkbook workbook = new XLWorkbook(stream))
                {
                    IXLWorksheet worksheet = workbook.Worksheet(1);
                    IXLRows rows = worksheet.RowsUsed();

                    var strategy = _context.Database.CreateExecutionStrategy();

                    try
                    {
                        await strategy.ExecuteAsync(async () =>
                        {
                            using (IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync())
                            {
                                try
                                {
                                    successCount = 0;
                                    skipCount = 0;

                                    foreach (IXLRow row in rows.Skip(1))
                                    {
                                        string studentId = row.Cell(1).GetValue<string>().Trim();
                                        string rawPassword = row.Cell(2).GetValue<string>().Trim();

                                        if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(rawPassword)) continue;

                                        bool isProfileExists = _context.UserProfiles.Any(u => u.StudentId == studentId);
                                        bool isRadcheckExists = _context.Radchecks.Any(r => r.Username == studentId);

                                        if (isProfileExists || isRadcheckExists)
                                        {
                                            skipCount++;
                                            continue;
                                        }

                                        string hashedPassword = HashPasswordSHA256(rawPassword);

                                        Radcheck radiusUser = new Radcheck
                                        {
                                            Username = studentId,
                                            Attribute = "SHA256-Password",
                                            Op = ":=",
                                            Value = hashedPassword
                                        };
                                        _context.Radchecks.Add(radiusUser);

                                        UserProfile emptyProfile = new UserProfile
                                        {
                                            StudentId = studentId,
                                            Department = "วิศวกรรมคอมพิวเตอร์",
                                            UpdatedAt = thaiTime
                                        };
                                        _context.UserProfiles.Add(emptyProfile);

                                        successCount++;
                                    }

                                    await _context.SaveChangesAsync();
                                    await transaction.CommitAsync();
                                }
                                catch (Exception)
                                {
                                    await transaction.RollbackAsync();
                                    throw;
                                }
                            }
                        });

                        TempData["Success"] = $"นำเข้าสำเร็จ {successCount} รายการ และข้ามรายการที่ซ้ำ {skipCount} รายการ";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "เกิดข้อผิดพลาดในการนำเข้าข้อมูล (อาจเกิดจากการเชื่อมต่อขัดข้อง): " + ex.Message;
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());

            var suggestions = await _context.UserProfiles
                .Where(s => s.StudentId.Contains(term) || s.FirstName.Contains(term) || s.LastName.Contains(term))
                .Select(s => new {
                    id = s.StudentId,
                    label = $"{s.StudentId} - {s.FirstName} {s.LastName}"
                })
                .Take(10)
                .ToListAsync();

            return Json(suggestions);
        }

        private bool UserProfileExists(string id)
        {
            return _context.UserProfiles.Any(e => e.StudentId == id);
        }
    }
}