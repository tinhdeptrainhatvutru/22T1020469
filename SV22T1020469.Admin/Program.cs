using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using SV22T1020605.Admin.AppCodes;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 🔥 SERVICES
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "SV22T1020605.Admin.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Tránh bị hết hạn cookie quá sớm => giảm rơi đăng nhập
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
});
builder.Services.AddControllersWithViews();

// 🔐 AUTH
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "SV22T1020605.Auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

var app = builder.Build();

// 🔥 FIX ĐÚNG CHUẨN ApplicationContext
ApplicationContext.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.GetRequiredService<IWebHostEnvironment>(),
    builder.Configuration
);

// 🌐 CULTURE
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// ⚠️ ERROR HANDLING
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// STATIC
app.UseStaticFiles();

app.UseRouting();

// 🔥 SESSION TRƯỚC AUTH (QUAN TRỌNG)
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// ROUTE
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// 🔥 FIX NULL CONNECTION STRING
string connectionString =
    builder.Configuration.GetConnectionString("LiteCommerceDB") ?? "";

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Connection string 'LiteCommerceDB' không tồn tại!");
}

// INIT DB
SV22T1020469.BusinessLayers.Configuration.Initialize(connectionString);

// === AUTO DB SCHEMA UPDATE (idempotent) ===
// Mục tiêu: đảm bảo các cột cần thiết tồn tại để tránh crash khi lưu.
static void EnsureSchema(string cs)
{
    using var cn = new SqlConnection(cs);
    cn.Open();

    void Exec(string sql)
    {
        using var cmd = new SqlCommand(sql, cn);
        cmd.ExecuteNonQuery();
    }

    // Products.Quantity (tồn kho theo mặt hàng)
    Exec(@"
        IF COL_LENGTH('Products', 'Quantity') IS NULL
        BEGIN
            ALTER TABLE Products
            ADD Quantity INT NOT NULL CONSTRAINT DF_Products_Quantity DEFAULT (0);
        END");

    // Orders.CustomerNote (ghi chú khách)
    Exec(@"
        IF COL_LENGTH('Orders', 'CustomerNote') IS NULL
        BEGIN
            ALTER TABLE Orders
            ADD CustomerNote NVARCHAR(1000) NULL;
        END");
}

EnsureSchema(connectionString);

app.Run();