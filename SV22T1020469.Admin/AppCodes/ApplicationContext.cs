using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SV22T1020605.Admin.AppCodes
{
    /// <summary>
    /// Lớp cung cấp các tiện ích liên quan đến ngữ cảnh của ứng dụng web
    /// </summary>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Gọi hàm này trong Program
        /// </summary>
        /// <param name="httpContextAccessor">app.Services.GetRequiredService<IHttpContextAccessor>()</param>
        /// <param name="webHostEnvironment">app.Services.GetRequiredService<IWebHostEnvironment>()</param>
        /// <param name="configuration">app.Configuration</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Configure(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// HttpContext
        /// </summary>
        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;

        /// <summary>
        /// WebHostEnvironment
        /// </summary>
        public static IWebHostEnvironment? WebHostEnvironment => _webHostEnvironment;

        /// <summary>
        /// Configuration
        /// </summary>
        public static IConfiguration? Configuration => _configuration;

        /// <summary>
        /// Ghi dữ liệu vào session
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetSessionData(string key, object value)
        {
            _httpContextAccessor?.HttpContext?.Session.SetString(key, JsonConvert.SerializeObject(value));
        }

        /// <summary>
        /// Đọc dữ liệu từ session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetSessionData<T>(string key) where T : class
        {
            try
            {
                string sValue = _httpContextAccessor?.HttpContext?.Session.GetString(key) ?? "";
                if (!string.IsNullOrEmpty(sValue))
                {
                    return JsonConvert.DeserializeObject<T>(sValue);
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Lấy chuỗi giá trị của cấu hình trong appsettings.json
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetConfigValue(string name)
        {
            return _configuration?[name] ?? "";
        }

        /// <summary>
        /// Lấy đối tượng có kiểu là T trong phần cấu hình có tên là name trong appsettings.json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetConfigSection<T>(string name) where T : new()
        {
            var value = new T();
            _configuration?.GetSection(name).Bind(value);
            return value;
        }
    }
}