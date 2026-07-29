using System;
using System.Collections.Generic;
using System.Linq;
using Translator.Core;
using Translator.Desktop.Models;

namespace Translator.Desktop.Services;

// The rules of the Word Trail: newest first, one row per Lemma, a fixed length, and a stored
// word that no longer covers every Enabled Language is replaced rather than merged.
public static class WordTrail
{
    public const int MaxLength = 100;

    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public static TrailWord? Find(IEnumerable<TrailWord> trail, string word) =>
        trail.FirstOrDefault(w => Comparer.Equals(w.Word, word.Trim()));

    // Missing an Enabled Language means the Panel would open without exactly the language the
    // user just switched on, with nothing on screen able to explain why.
    public static bool IsStale(TrailWord stored, IEnumerable<string> enabledLanguages) =>
        enabledLanguages.Any(l => !stored.Coverage.Contains(l, Comparer));

    public static void Remember(
        List<TrailWord> trail, string typed, LookupResult result, IReadOnlyList<string> coverage)
    {
        var word = new TrailWord(LemmaFor(typed, result), result, coverage.ToList());
        trail.RemoveAll(w => Comparer.Equals(w.Word, word.Word));
        trail.Insert(0, word);
        while (trail.Count > MaxLength) trail.RemoveAt(trail.Count - 1);
    }

    // Words are filed under their Lemma, not under what was typed: gick and gå are one row.
    // A word the model does not know keeps the typed form, or there would be nothing to file.
    private static string LemmaFor(string typed, LookupResult result)
    {
        var word = typed.Trim();
        var headwords = result.Entries.Select(e => e.Headword).ToList();
        return headwords.FirstOrDefault(h => Comparer.Equals(h, word))
               ?? headwords.FirstOrDefault()
               ?? word;
    }
}
