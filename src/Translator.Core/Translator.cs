using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace Translator.Core;

public class Translator(string apiKey, string model = "claude-opus-4-8") : ITranslator
{
    private readonly AnthropicClient _client = new() { ApiKey = apiKey };

    public Task<TranslationResult> TranslateAsync(string text, string styleGuide) =>
        CreateAsync(PromptBuilder.Build(text, styleGuide));

    public Task<TranslationResult> TranslateAsync(string text, string styleGuide, string targetLanguage) =>
        CreateAsync(PromptBuilder.Build(text, styleGuide, targetLanguage));

    private async Task<TranslationResult> CreateAsync(string prompt)
    {
        var parameters = new MessageCreateParams
        {
            Model = model,
            MaxTokens = 1000,
            Messages = [new() { Role = Role.User, Content = prompt }],
            OutputConfig = new OutputConfig()
            {
                Format = new JsonOutputFormat { Schema = TranslationSchema.Build() },
            },
        };
        var response = await _client.Messages.Create(parameters);
        var json = response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text;
        return JsonSerializer.Deserialize<TranslationResult>(json)!;
    }
}
