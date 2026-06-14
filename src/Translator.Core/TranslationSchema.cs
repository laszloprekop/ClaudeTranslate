using System.Text.Json;

namespace Translator.Core;

public static class TranslationSchema
{
    public static Dictionary<string, JsonElement> Build() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            source = new { type = "string", @enum = new[] { "English", "Swedish" } },
            target = new { type = "string", @enum = new[] { "English", "Swedish" } },
            translation = new { type = "string" },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "source", "target", "translation" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false)
    };
}
