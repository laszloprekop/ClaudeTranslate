using Translator.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITranslator, Translator.Core.Translator>();
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
