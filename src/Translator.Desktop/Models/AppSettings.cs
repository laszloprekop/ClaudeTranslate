using System.Collections.Generic;
using System.Text.Json.Serialization;
using Translator.Core;

namespace Translator.Desktop.Models;

public class AppSettings
{
    public string ApiKey { get; set; } = "";
    public string Style { get; set; } = "";
    public string Model { get; set; } = "claude-opus-4-8";
    public List<string> Targets { get; set; } = ["English", "Swedish"];
    public List<HistoryItem> History { get; set; } = new();
}

public record HistoryItem(string Input, string Source, string Target, string Translation)
{
    [JsonIgnore]
    public string Dir => $"{LanguageCatalog.CodeFor(Source)} → {LanguageCatalog.CodeFor(Target)}";
}
