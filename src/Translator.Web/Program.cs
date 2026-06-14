using Translator.Core;

var builder = WebApplication.CreateBuilder(args);
var apiKey = builder.Configuration["Anthropic:ApiKey"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
    throw new InvalidOperationException("No Anthropic API key configured.");
var model = builder.Configuration["Anthropic:Model"] ?? "claude-opus-4-8";
builder.Services.AddSingleton<ITranslator>(_ => new Translator.Core.Translator(apiKey, model));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/translate", async (TranslateRequest req, ITranslator translator) =>
{
    var result = await translator.TranslateAsync(req.Text, req.Style ?? "");
    return Results.Ok(result);
});

app.Run();

record TranslateRequest(string Text, string? Style);
