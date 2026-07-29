using System.Collections.Generic;
using System.Text.Json.Serialization;
using Translator.Core;

namespace Translator.Desktop.Models;

// One row of the Word Trail. The trail is the visible history of Lookups *and* the store of
// their results — there is no cache beside it, so a word on the trail always has what it needs
// and a word that falls off the end takes it along.
//
// Word is the Lemma the result was filed under, not what was typed. Coverage is the Prefetch
// Set the Lookup asked for: a stored word whose Coverage is missing an Enabled Language is
// stale and gets replaced rather than topped up.
public record TrailWord(string Word, LookupResult Result, List<string> Coverage)
{
    [JsonIgnore]
    public bool HasEntries => Result.Entries.Count > 0;
}
