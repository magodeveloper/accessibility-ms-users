using Microsoft.AspNetCore.Http;

namespace Users.Api.Helpers
{
    public static class LanguageHelper
    {
        public static string GetRequestLanguage(HttpRequest request)
        {
            try
            {
                var header = request.Headers["Accept-Language"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(header))
                {
                    return "es";
                }

                var token = header.Split(',')[0].Trim();
                if (token.Length < 2)
                {
                    return "es";
                }

                var lang2 = token[..2].ToLowerInvariant();
                return lang2 is "es" or "en" ? lang2 : "es";
            }
            catch
            {
                return "es";
            }
        }
    }
}