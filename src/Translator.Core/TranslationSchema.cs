using System.Text.Json;

namespace Translator.Core;

public static class TranslationSchema
{
    public static Dictionary<string, JsonElement> Build() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            source = new { type = "string", @enum = LanguageCatalog.All.Select(l => l.Name).ToArray() },
            target = new { type = "string", @enum = LanguageCatalog.All.Select(l => l.Name).ToArray() },
            translation = new { type = "string" },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "source", "target", "translation" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false)
    };
}
