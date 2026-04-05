using Microsoft.AspNetCore.Http;
using System.Text.Json; // Sử dụng thư viện built-in của .NET

namespace SV22T1020469.Shop
{
    public static class ShopSession
    {
        private static IHttpContextAccessor? _accessor;

        public static void Configure(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public static void Set(string key, object value)
        {
            _accessor?.HttpContext?.Session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? Get<T>(string key)
        {
            var data = _accessor?.HttpContext?.Session.GetString(key);
            if (string.IsNullOrEmpty(data))
                return default;

            return JsonSerializer.Deserialize<T>(data);
        }
    }
}