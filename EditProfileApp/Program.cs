using EditProfileApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using EditProfileApp.Services;

System.Environment.SetEnvironmentVariable("TZ", "Asia/Bangkok");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddTransient<EmailService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<RadiusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "EditProfileAppLogin";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// สร้างฐานข้อมูลและตารางอัตโนมัติ พร้อมสร้าง Admin เริ่มต้น
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<RadiusDbContext>();

        context.Database.Migrate();

        if (!context.UserProfiles.Any(u => u.StudentId == "AdminCE"))
        {
            TimeZoneInfo thaiZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime thaiTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, thaiZone);

            context.UserProfiles.Add(new UserProfile
            {
                StudentId = "AdminCE",
                FirstName = "System",
                LastName = "Administrator",
                Email = "admince@kmitl.ac.th",
                Department = "Computer Engineering",
                UpdatedAt = thaiTime
            });

            var adminSecretPassword = builder.Configuration["AdminDefaultPassword"];

            if (string.IsNullOrEmpty(adminSecretPassword))
            {
                throw new Exception("FATAL ERROR: ระบบไม่พบรหัสผ่านสำหรับสร้าง AdminCE ");
            }

            context.Radchecks.Add(new Radcheck
            {
                Username = "AdminCE",
                Attribute = "Cleartext-Password",
                Op = ":=",
                Value = adminSecretPassword
            });

            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database Migration Failed!");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();