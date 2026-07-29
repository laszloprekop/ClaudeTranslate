using System.Text.Json;

namespace Translator.Core.Tests;

public class LookupResultTests
{
    private const string InertJson = """
        {
          "entries": [
            {
              "headword": "inert",
              "language": "English",
              "pronunciation": "/ɪˈnɜːt/",
              "part_of_speech": "adjective",
              "lemma_note": null,
              "senses": [
                {
                  "domain": "chemistry",
                  "gloss": "chemically inactive; not reacting with other substances",
                  "example": "Argon is an inert gas.",
                  "equivalents": [ { "language": "Swedish", "word": "inert" } ],
                  "synonyms": [ "unreactive" ],
                  "antonyms": [ "reactive" ],
                  "false_friends": []
                },
                {
                  "domain": null,
                  "gloss": "slow to move or act; lacking energy",
                  "example": "The committee was inert for months.",
                  "equivalents": [ { "language": "Swedish", "word": "trög" } ],
                  "synonyms": [ "sluggish" ],
                  "antonyms": [ "energetic" ],
                  "false_friends": [
                    {
                      "language": "German",
                      "word": "inert",
                      "note": "German inert is chemical only; for a sluggish person use träge."
                    }
                  ]
                }
              ]
            }
          ],
          "note": null,
          "suggestion": null
        }
        """;

    [Fact]
    public void Equivalents_belong_to_a_sense_not_to_the_entry()
    {
        var result = JsonSerializer.Deserialize<LookupResult>(InertJson);

        var senses = result!.Entries.Single().Senses;
        Assert.Equal("inert", senses[0].Equivalents.Single().Word);
        Assert.Equal("trög", senses[1].Equivalents.Single().Word);
    }

    [Fact]
    public void False_friend_notes_carry_the_language_they_are_about()
    {
        var result = JsonSerializer.Deserialize<LookupResult>(InertJson);

        var note = result!.Entries.Single().Senses[1].FalseFriends.Single();
        Assert.Equal("German", note.Language);
        Assert.Empty(result.Entries.Single().Senses[0].FalseFriends);
    }

    [Fact]
    public void Entry_carries_headword_language_and_lemma_note()
    {
        var json = """
            {
              "entries": [
                {
                  "headword": "gå",
                  "language": "Swedish",
                  "pronunciation": "/ɡoː/",
                  "part_of_speech": "verb",
                  "lemma_note": "gick — past tense of gå",
                  "senses": []
                }
              ]
            }
            """;

        var entry = JsonSerializer.Deserialize<LookupResult>(json)!.Entries.Single();
        Assert.Equal("gå", entry.Headword);
        Assert.Equal("Swedish", entry.Language);
        Assert.Equal("gick — past tense of gå", entry.LemmaNote);
    }

    [Fact]
    public void Zero_entries_with_a_note_and_a_suggestion_is_a_valid_result()
    {
        var json = """
            {"entries":[],"note":"No entry found.","suggestion":"inert"}
            """;

        var result = JsonSerializer.Deserialize<LookupResult>(json);

        Assert.Empty(result!.Entries);
        Assert.Equal("No entry found.", result.Note);
        Assert.Equal("inert", result.Suggestion);
    }
}
