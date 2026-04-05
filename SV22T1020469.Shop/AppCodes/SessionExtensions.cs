using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SV22T1020469.Shop
{
    public static class SessionExtensions
    {
        public static void SetObject(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var data = session.GetString(key);
            if (string.IsNullOrEmpty(data)) return default;
            try
            {
                return JsonSerializer.Deserialize<T>(data);
            }
            catch
            {
                // Nếu dữ liệu session bị hỏng/không deserialize được (timeout, đổi model, ...),
                // xóa key để tránh crash và trả về null.
                session.Remove(key);
                return default;
            }
        }
    }
}
