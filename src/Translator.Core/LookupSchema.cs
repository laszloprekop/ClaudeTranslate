using System.Text.Json;

namespace Translator.Core;

public static class LookupSchema
{
    // The cap lives in the prompt, not in the schema: structured outputs do not accept array
    // size constraints (minItems/maxItems), and an unsupported keyword fails the whole request.
    public const int MaxSenses = 4;

    public static Dictionary<string, JsonElement> Build() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            entries = new { type = "array", items = Entry() },
            note = NullableString(),
            suggestion = NullableString(),
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "entries", "note", "suggestion" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false)
    };

    private static object Entry() => new
    {
        type = "object",
        properties = new
        {
            headword = new { type = "string" },
            language = LanguageEnum(),
            pronunciation = new { type = "string" },
            part_of_speech = new { type = "string" },
            lemma_note = NullableString(),
            senses = new { type = "array", items = Sense() },
        },
        required = new[]
        {
            "headword", "language", "pronunciation", "part_of_speech", "lemma_note", "senses"
        },
        additionalProperties = false,
    };

    private static object Sense() => new
    {
        type = "object",
        properties = new
        {
            gloss = new { type = "string" },
            example = new { type = "string" },
            equivalents = new { type = "array", items = Equivalent() },
            synonyms = new { type = "array", items = new { type = "string" } },
            antonyms = new { type = "array", items = new { type = "string" } },
            false_friends = new { type = "array", items = FalseFriendNote() },
            domain = NullableString(),
        },
        required = new[]
        {
            "gloss", "example", "equivalents", "synonyms", "antonyms", "false_friends", "domain"
        },
        additionalProperties = false,
    };

    private static object Equivalent() => new
    {
        type = "object",
        properties = new
        {
            language = LanguageEnum(),
            word = new { type = "string" },
        },
        required = new[] { "language", "word" },
        additionalProperties = false,
    };

    private static object FalseFriendNote() => new
    {
        type = "object",
        properties = new
        {
            language = LanguageEnum(),
            word = new { type = "string" },
            note = new { type = "string" },
        },
        required = new[] { "language", "word", "note" },
        additionalProperties = false,
    };

    private static object LanguageEnum() =>
        new { type = "string", @enum = LanguageCatalog.All.Select(l => l.Name).ToArray() };

    private static object NullableString() =>
        new { anyOf = new object[] { new { type = "string" }, new { type = "null" } } };
}
