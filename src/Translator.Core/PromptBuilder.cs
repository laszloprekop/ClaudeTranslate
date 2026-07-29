namespace Translator.Core;

public class PromptBuilder
{
    public const string DefaultStyle =
        "Natural, idiomatic, slighltly casual. Sound like a fluent bilingual speaker" +
        " writing quickly to a collegue. Translate intent, not words." +
        " Leave technical terms and proper nouns untouched.";

    public static string Build(string text, string styleGuide = DefaultStyle)
    {
        var style = string.IsNullOrWhiteSpace(styleGuide) ? DefaultStyle : styleGuide;
        return $"""
                        You translate between English and Swedish for a fluent bilingual user.
                        Step 1. Detect input language. If English, translate to Swedish. If Swedish, translate to English.
                        Step 2. Translate following this style guide exactly:
                        {style}
                        Input:
                        \"\"\"{text}\"\"\"
                """;
    }

    public static string Build(string text, string styleGuide, string targetLanguage)
    {
        var style = string.IsNullOrWhiteSpace(styleGuide) ? DefaultStyle : styleGuide;
        return $"""
                        You translate into {targetLanguage} for a fluent multilingual user.
                        Step 1. Detect the input language and report it as "source".
                        If the input is already {targetLanguage}, return the text unchanged with source equal to target.
                        Step 2. Otherwise translate following this style guide exactly:
                        {style}
                        Input:
                        \"\"\"{text}\"\"\"
                """;
    }

    // The style guide is deliberately not a parameter: it shapes translations, not entries.
    public static string BuildLookup(string word, IReadOnlyList<string> languages)
    {
        var languageList = string.Join(", ", languages);
        return $"""
                You are a dictionary for a learner who reads these languages: {languageList}.
                Write one entry per language in which "{word}" exists as a word of that language.
                A word that exists in three of those languages gets three entries; a word that exists
                in one gets one. Do not invent an entry for a language where the word is not used.

                File each entry under its lemma — the dictionary form. If the input was inflected,
                the headword is still the lemma, and lemma_note says how they relate, in the form
                "gick — past tense of gå". Otherwise lemma_note is null.

                Give at most {LookupSchema.MaxSenses} senses per entry: the {LookupSchema.MaxSenses} a
                learner most needs, ordered most common first. Fewer is better than padding. Beyond
                that you are in archaic and hyper-technical territory, where a generated entry is
                least accurate and a learner has no way to tell.

                Inside a sense:
                - gloss and example are written in the entry's own language, not the learner's.
                - example is one short sentence with the headword in it.
                - domain is a short label like "chemistry" or "law" when the sense is confined to
                  one, and null otherwise.
                - equivalents are the words carrying THIS sense in the other languages listed above,
                  one per language that has one. A different sense of the same headword usually takes
                  a different equivalent — that difference is the point of the entry.
                - synonyms and antonyms are in the entry's own language and belong to this sense.
                - false_friends warns about a word in a named language that looks like it fits this
                  sense but does not. Always name the language it concerns. Empty list if there is none.

                If "{word}" is not a word in any of those languages, return zero entries. Do not
                invent one. Then:
                - note says why in one line — a misspelling, a proper noun, or not a word at all.
                - suggestion is the word that was probably meant, or null if nothing fits.
                Otherwise note and suggestion are both null.

                Word:
                \"\"\"{word}\"\"\"
                """;
    }
}
