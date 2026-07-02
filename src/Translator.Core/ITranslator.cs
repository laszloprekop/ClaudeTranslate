namespace Translator.Core;

public interface ITranslator
{
    Task<TranslationResult> TranslateAsync(string text, string styleGuide);
    Task<TranslationResult> TranslateAsync(string text, string styleGuide, string targetLanguage);
}
