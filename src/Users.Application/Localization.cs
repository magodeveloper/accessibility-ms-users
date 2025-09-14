using System.Text.Json;
using System.Globalization;
using System.Collections.Concurrent;

namespace Users.Application;

public static class Localization
{
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private static readonly string ResourcePath = Path.Combine(AppContext.BaseDirectory, "Resources");

    public static string Get(string key, string? lang = null)
    {
        lang ??= CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        var dict = _cache.GetOrAdd(lang, langKey =>
        {
            var file = Path.Combine(ResourcePath, $"messages.{langKey}.json");
            if (!File.Exists(file)) file = Path.Combine(ResourcePath, "messages.en.json");
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))!;
        });

        return dict.TryGetValue(key, out var value) ? value : key;
    }
}