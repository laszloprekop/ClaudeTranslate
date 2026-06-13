namespace Translator.Core;

public class Translator : ITranslator
{
    public Task<TranslationResult> TranslateAsync(string text, string styleGuide)
    {
        var result = new TranslationResult { Source = "English", Target = "Swedish", Translation = $"[stub]" };
        return Task.FromResult(result);
    }
}
