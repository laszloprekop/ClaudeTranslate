using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace Translator.Core;

public class Translator(string apiKey, string model = "claude-opus-4-8") : ITranslator
{
    private readonly AnthropicClient _client = new() { ApiKey = apiKey };

    public async Task<TranslationResult> TranslateAsync(string text, string styleGuide)
    {
        var parameters = new MessageCreateParams
        {
            Model = model,
            MaxTokens = 1000,
            Messages = [new() { Role = Role.User, Content = PromptBuilder.Build(text, styleGuide) }],
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
