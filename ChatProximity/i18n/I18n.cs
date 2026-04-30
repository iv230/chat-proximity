using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ChatProximity.i18n;

public static class I18N
{
    private static Dictionary<string, string> Strings = new();

    public static void Load(string lang = "en")
    {
        var path = Path.Combine(
            ChatProximity.PluginInterface.AssemblyLocation.Directory!.FullName,
            "i18n", "locales", $"{lang}.json"
        );

        if (!File.Exists(path))
            throw new FileNotFoundException($"Missing i18n file: {path}");

        var json = File.ReadAllText(path);
        Strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
    }

    public static string T(string key)
        => Strings.TryGetValue(key, out var value) ? value : $"[{key}]";
}
