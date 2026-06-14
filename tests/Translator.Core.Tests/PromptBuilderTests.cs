using System.Text.Json;

namespace Translator.Core.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void Build_uses_default_style_when_blank()
    {
        var prompt = PromptBuilder.Build("hej", "  ");
        Assert.Contains(PromptBuilder.DefaultStyle, prompt);
        Assert.Contains("hej", prompt);
    }

    [Fact]
    public void TranslationResult_deserializes_from_lowercase_json()
    {
        var json = """{"source":"Swedish","target":"English","translation":"hi"}""";
        var result = JsonSerializer.Deserialize<TranslationResult>(json);
        Assert.Equal("Swedish", result?.Source);
        Assert.Equal("hi", result?.Translation);
    }
}
