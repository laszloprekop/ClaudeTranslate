using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Translator.Core;

namespace Translator.Desktop.Models;

public class AppSettings
{
    public string ApiKey { get; set; } = "";
    public string Style { get; set; } = "";
    public string Model { get; set; } = ModelCatalog.Default;
    public List<string> Targets { get; set; } = ["English", "Swedish"];
    public List<HistoryItem> History { get; set; } = new();

    // Every Lookup asks for the whole Prefetch Set whatever is currently enabled; enabling a
    // language filters what is shown rather than triggering a fetch (ADR-0004).
    public List<string> Prefetch { get; set; } = LanguageCatalog.All.Select(l => l.Name).ToList();
    public List<TrailWord> Trail { get; set; } = new();
}

public record HistoryItem(string Input, string Source, string Target, string Translation)
{
    [JsonIgnore]
    public string Dir => $"{LanguageCatalog.CodeFor(Source)} → {LanguageCatalog.CodeFor(Target)}";
}
