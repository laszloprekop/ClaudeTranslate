using System.Text.Json.Serialization;

namespace Translator.Core;

// The result of one Lookup: an Entry per language in which the word exists.
// Zero entries is a legitimate answer — a misspelling, a proper noun or a non-word —
// in which case Note says why and Suggestion may offer the word that was meant.
public record LookupResult(
    [property: JsonPropertyName("entries")] IReadOnlyList<Entry> Entries,
    [property: JsonPropertyName("note")] string? Note = null,
    [property: JsonPropertyName("suggestion")] string? Suggestion = null );

// Headword is the lemma; LemmaNote carries "gick — past tense of gå" when the input was inflected.
public record Entry(
    [property: JsonPropertyName("headword")] string Headword,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("pronunciation")] string Pronunciation,
    [property: JsonPropertyName("part_of_speech")] string PartOfSpeech,
    [property: JsonPropertyName("senses")] IReadOnlyList<Sense> Senses,
    [property: JsonPropertyName("lemma_note")] string? LemmaNote = null );

// Equivalents, synonyms, antonyms and false-friend notes belong to a Sense, never to an Entry:
// Swedish takes inert for the chemistry sense and trög for the figurative one, and a flat list
// on the Entry loses exactly that.
public record Sense(
    [property: JsonPropertyName("gloss")] string Gloss,
    [property: JsonPropertyName("example")] string Example,
    [property: JsonPropertyName("equivalents")] IReadOnlyList<Equivalent> Equivalents,
    [property: JsonPropertyName("synonyms")] IReadOnlyList<string> Synonyms,
    [property: JsonPropertyName("antonyms")] IReadOnlyList<string> Antonyms,
    [property: JsonPropertyName("false_friends")] IReadOnlyList<FalseFriendNote> FalseFriends,
    [property: JsonPropertyName("domain")] string? Domain = null );

public record Equivalent(
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("word")] string Word );

// Language is what the note is about. Entries are fetched for the whole Prefetch Set and filtered
// on display, so an untagged note would be shown to someone who does not have that language on.
public record FalseFriendNote(
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("note")] string Note );
