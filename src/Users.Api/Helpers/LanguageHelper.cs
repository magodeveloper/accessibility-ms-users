using Microsoft.AspNetCore.Http;

namespace Users.Api.Helpers
{
    public static class LanguageHelper
    {
        public static string GetRequestLanguage(HttpRequest request)
        {
            var lang = request.Headers["Accept-Language"].FirstOrDefault()?.Split(',')[0]?.Substring(0, 2);
            return string.IsNullOrWhiteSpace(lang) ? "es" : lang;
        }
    }
}