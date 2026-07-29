using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Translator.Core;

namespace Translator.Desktop.ViewModels;

// A link out to a published dictionary. The app links; it never fetches (ADR-0001).
public record SourceLink(string Label, string Url);

// A clickable word plus the language it is a word of. Clicking a Swedish equivalent has to open
// a Swedish entry, so the language travels with the word rather than with the panel.
public record WordChip(string Text, string? Language);

public class EquivalentRow(string language, IReadOnlyList<string> words)
{
    public string Language { get; } = language;
    public IReadOnlyList<WordChip> Words { get; } = words.Select(w => new WordChip(w, language)).ToList();
    public string Flag { get; } = LanguageCatalog.All.FirstOrDefault(l => l.Name == language)?.Flag ?? "";
    public string Text { get; } = string.Join(" · ", words);
}

public class PanelSense
{
    public PanelSense(int number, Sense sense, Entry entry, IReadOnlyList<string> enabledLanguages)
    {
        Number = number.ToString();
        Domain = sense.Domain?.ToUpperInvariant() ?? "";
        Gloss = sense.Gloss;
        ExampleInlines = Highlight(sense.Example, entry.Headword);

        // Entries are fetched for the whole Prefetch Set and filtered here, on display: an
        // equivalent or a warning in a language the user does not have on is noise (ADR-0004).
        Equivalents = sense.Equivalents
            .Where(e => enabledLanguages.Contains(e.Language))
            .GroupBy(e => e.Language)
            .Select(g => new EquivalentRow(g.Key, g.Select(e => e.Word).ToList()))
            .ToList();
        Warnings = sense.FalseFriends
            .Where(f => enabledLanguages.Contains(f.Language))
            .ToList();

        // Synonyms and antonyms are words of the entry's own language.
        Synonyms = sense.Synonyms.Select(w => new WordChip(w, entry.Language)).ToList();
        Antonyms = sense.Antonyms.Select(w => new WordChip(w, entry.Language)).ToList();
    }

    public string Number { get; }
    public string Domain { get; }
    public bool HasDomain => Domain.Length > 0;
    public string Gloss { get; }
    public InlineCollection ExampleInlines { get; }
    public IReadOnlyList<EquivalentRow> Equivalents { get; }
    public IReadOnlyList<FalseFriendNote> Warnings { get; }
    public IReadOnlyList<WordChip> Synonyms { get; }
    public IReadOnlyList<WordChip> Antonyms { get; }
    public bool HasSynonyms => Synonyms.Count > 0;
    public bool HasAntonyms => Antonyms.Count > 0;

    // The headword is marked wherever it appears inside its own example.
    private static InlineCollection Highlight(string example, string headword)
    {
        var inlines = new InlineCollection();
        var at = example.IndexOf(headword, StringComparison.CurrentCultureIgnoreCase);
        if (headword.Length == 0 || at < 0)
        {
            inlines.Add(new Run(example));
            return inlines;
        }
        inlines.Add(new Run(example[..at]));
        inlines.Add(new Run(example.Substring(at, headword.Length)) { FontWeight = FontWeight.Bold });
        inlines.Add(new Run(example[(at + headword.Length)..]));
        return inlines;
    }
}

public class PanelEntry
{
    public PanelEntry(Entry entry, IReadOnlyList<string> enabledLanguages)
    {
        Headword = entry.Headword;
        Language = entry.Language;
        LemmaNote = entry.LemmaNote ?? "";
        var flag = LanguageCatalog.All.FirstOrDefault(l => l.Name == entry.Language)?.Flag ?? "";
        Meta = string.Join("  ·  ",
            new[] { entry.Pronunciation, entry.PartOfSpeech, $"{flag} {entry.Language}" }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
        Senses = entry.Senses
            .Select((sense, i) => new PanelSense(i + 1, sense, entry, enabledLanguages))
            .ToList();
        Links = LinksFor(entry);
    }

    public string Headword { get; }
    public string Language { get; }
    public string LemmaNote { get; }
    public bool HasLemmaNote => LemmaNote.Length > 0;
    public string Meta { get; }
    public IReadOnlyList<PanelSense> Senses { get; }
    public IReadOnlyList<SourceLink> Links { get; }
    public bool HasLinks => Links.Count > 0;

    // Scoped to the entry's language. Neither OED nor SAOL exposes an entry id we can derive
    // from the headword, so these land on a search or disambiguation page — one click to an
    // authority, not a precise anchor.
    private static IReadOnlyList<SourceLink> LinksFor(Entry entry)
    {
        var word = Uri.EscapeDataString(entry.Headword);
        var sztaki = new SourceLink(
            "SZTAKI", $"https://szotar.sztaki.hu/search?fromlang=all&tolang=all&searchWord={word}");
        return entry.Language switch
        {
            "English" =>
            [
                new SourceLink("OED",
                    $"https://www.oed.com/dictionary/{word}_{OedPartOfSpeech(entry.PartOfSpeech)}?tab=meaning_and_use"),
                sztaki,
            ],
            "Swedish" => [new SourceLink("SAOL", $"https://svenska.se/?activeTab=saol&q={word}&exactMatch=true")],
            "Hungarian" => [sztaki],
            _ => [],
        };
    }

    private static string OedPartOfSpeech(string partOfSpeech) => partOfSpeech.ToLowerInvariant() switch
    {
        var p when p.StartsWith("adj") => "adj",
        var p when p.StartsWith("adv") => "adv",
        var p when p.StartsWith("verb") => "v",
        var p when p.StartsWith("prep") => "prep",
        var p when p.StartsWith("pron") => "pron",
        var p when p.StartsWith("conj") => "conj",
        var p when p.StartsWith("interj") => "int",
        _ => "n",
    };
}
