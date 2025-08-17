using System.Text.Json;
using System.Globalization;

namespace Users.Application;

public static class Localization
{
    private static readonly Dictionary<string, Dictionary<string, string>> _cache = new();
    private static readonly string ResourcePath = Path.Combine(AppContext.BaseDirectory, "Resources");

    public static string Get(string key, string? lang = null)
    {
        lang ??= CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (!_cache.TryGetValue(lang, out var dict))
        {
            var file = Path.Combine(ResourcePath, $"messages.{lang}.json");
            if (!File.Exists(file)) file = Path.Combine(ResourcePath, "messages.en.json");
            dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))!;
            _cache[lang] = dict;
        }
        return dict.TryGetValue(key, out var value) ? value : key;
    }
}
