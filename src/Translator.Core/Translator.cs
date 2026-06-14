namespace Translator.Core;

public class Translator : ITranslator
{
    public Task<TranslationResult> TranslateAsync(string text, string styleGuide)
    {
        var result = new TranslationResult("English", "Swedish", $"[stub] {text}");
        return Task.FromResult(result);
    }
}
