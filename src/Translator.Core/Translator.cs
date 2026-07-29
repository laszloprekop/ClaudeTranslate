using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace Translator.Core;

public class Translator(string apiKey, string model = ModelCatalog.Default) : ITranslator
{
    // A capped entry runs ~1200-1500 tokens and a lookup asks for one per language, so the
    // ceiling has to clear the whole prefetch set. With structured outputs an overrun does not
    // truncate gracefully: the JSON never closes and deserialisation throws.
    private const int TranslationMaxTokens = 4000;
    private const int LookupMaxTokens = 16000;

    private readonly AnthropicClient _client = new() { ApiKey = apiKey };

    public Task<TranslationResult> TranslateAsync(string text, string styleGuide) =>
        CreateAsync<TranslationResult>(
            PromptBuilder.Build(text, styleGuide), TranslationSchema.Build(), TranslationMaxTokens, Effort.High);

    public Task<TranslationResult> TranslateAsync(string text, string styleGuide, string targetLanguage) =>
        CreateAsync<TranslationResult>(
            PromptBuilder.Build(text, styleGuide, targetLanguage), TranslationSchema.Build(),
            TranslationMaxTokens, Effort.High);

    // Medium rather than the default High: measured against the same word, medium returned the
    // same entries, the same senses and the same false-friend notes about 16 seconds sooner.
    // Low is not an option — it drops the false-friend notes, which are the point of an entry.
    public Task<LookupResult> LookUpAsync(string word, IReadOnlyList<string> languages) =>
        CreateAsync<LookupResult>(
            PromptBuilder.BuildLookup(word, languages), LookupSchema.Build(), LookupMaxTokens, Effort.Medium);

    private async Task<T> CreateAsync<T>(
        string prompt, Dictionary<string, JsonElement> schema, int maxTokens, Effort effort)
    {
        var parameters = new MessageCreateParams
        {
            Model = model,
            MaxTokens = maxTokens,
            Messages = [new() { Role = Role.User, Content = prompt }],
            OutputConfig = new OutputConfig()
            {
                Format = new JsonOutputFormat { Schema = schema },
                Effort = effort,
            },
        };
        var response = await _client.Messages.Create(parameters);
        var json = response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text;
        return JsonSerializer.Deserialize<T>(json)!;
    }
}
