using Translator.Core;

var builder = WebApplication.CreateBuilder(args);
var apiKey = builder.Configuration["Anthropic:ApiKey"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
    throw new InvalidOperationException("No Anthropic API key configured.");
var model = builder.Configuration["Anthropic:Model"] ?? ModelCatalog.Default;
builder.Services.AddSingleton<ITranslator>(_ => new Translator.Core.Translator(apiKey, model));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/translate", async (TranslateRequest req, ITranslator translator) =>
{
    var result = string.IsNullOrWhiteSpace(req.Target)
        ? await translator.TranslateAsync(req.Text, req.Style ?? "")
        : await translator.TranslateAsync(req.Text, req.Style ?? "", req.Target);
    return Results.Ok(result);
});

// Core holds the whole Lookup capability; this endpoint keeps it reachable from any client.
// The browser page deliberately does not draw it — see Docs/adr/0005-lookup-is-desktop-only.md.
app.MapPost("/api/define", async (DefineRequest req, ITranslator translator) =>
{
    var languages = req.Languages is { Count: > 0 }
        ? req.Languages
        : LanguageCatalog.All.Select(l => l.Name).ToList();
    var result = await translator.LookUpAsync(req.Word, languages);
    return Results.Ok(result);
});

app.Run();

record TranslateRequest(string Text, string? Style, string? Target);

record DefineRequest(string Word, List<string>? Languages);
