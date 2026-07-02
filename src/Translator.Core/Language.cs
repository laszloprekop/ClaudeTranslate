namespace Translator.Core;

public record Language(string Name, string NativeName, string Flag, string Code);

public static class LanguageCatalog
{
    public static readonly IReadOnlyList<Language> All =
    [
        new("English", "English", "🇬🇧", "en"),
        new("Swedish", "Svenska", "🇸🇪", "sv"),
        new("Hungarian", "Magyar", "🇭🇺", "hu"),
        new("German", "Deutsch", "🇩🇪", "de"),
        new("French", "Français", "🇫🇷", "fr"),
        new("Spanish", "Español", "🇪🇸", "es"),
    ];

    public static string CodeFor(string name) =>
        All.FirstOrDefault(l => l.Name == name)?.Code ?? "??";
}
