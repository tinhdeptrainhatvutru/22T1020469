using System.Globalization;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "SV22T1020469.Shop.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Tránh session cookie hết hạn quá sớm => người dùng không bị yêu cầu đăng nhập lại
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
});

var app = builder.Build();

// Middleware order is critical
app.UseStaticFiles();

// Share physical images folder from Admin project (same physical files)
var adminImagePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "SV22T1020605.Admin",
    "wwwroot",
    "images");

if (Directory.Exists(adminImagePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(adminImagePath),
        RequestPath = "/images"
    });
}

app.UseRouting();
app.UseSession();          // Session MUST come before controllers
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Culture: Vietnamese
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Initialize Business Layer
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB") ?? "";
SV22T1020469.BusinessLayers.Configuration.Initialize(connectionString);

// === AUTO DB SCHEMA UPDATE (idempotent) ===
// Đảm bảo các cột cần thiết tồn tại để chức năng tồn kho/ghi chú hoạt động ổn định.
static void EnsureSchema(string cs)
{
    using var cn = new SqlConnection(cs);
    cn.Open();

    void Exec(string sql)
    {
        using var cmd = new SqlCommand(sql, cn);
        cmd.ExecuteNonQuery();
    }

    Exec(@"
        IF COL_LENGTH('Products', 'Quantity') IS NULL
        BEGIN
            ALTER TABLE Products
            ADD Quantity INT NOT NULL CONSTRAINT DF_Products_Quantity DEFAULT (0);
        END");

    Exec(@"
        IF COL_LENGTH('Orders', 'CustomerNote') IS NULL
        BEGIN
            ALTER TABLE Orders
            ADD CustomerNote NVARCHAR(1000) NULL;
        END");
}

EnsureSchema(connectionString);

app.Run();
