using System.Text.Json.Serialization;

namespace Translator.Core;

public record TranslationResult(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("translation")] string Translation );
