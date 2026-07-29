namespace Translator.Core;

public interface ITranslator
{
    Task<TranslationResult> TranslateAsync(string text, string styleGuide);
    Task<TranslationResult> TranslateAsync(string text, string styleGuide, string targetLanguage);

    // A sibling of translation, not a variant of it: different prompt, different schema,
    // different result — the same client and the same one door out to Anthropic.
    Task<LookupResult> LookUpAsync(string word, IReadOnlyList<string> languages);
}
