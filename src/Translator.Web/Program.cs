using Translator.Core;

var builder = WebApplication.CreateBuilder(args);
var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "";
builder.Services.AddSingleton<ITranslator>(_ => new Translator.Core.Translator(apiKey));
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
