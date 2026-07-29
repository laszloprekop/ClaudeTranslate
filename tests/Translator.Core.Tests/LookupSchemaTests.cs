using System.Text.Json;

namespace Translator.Core.Tests;

public class LookupSchemaTests
{
    private static JsonElement Schema() =>
        JsonDocument.Parse(JsonSerializer.Serialize(LookupSchema.Build())).RootElement.Clone();

    private static JsonElement Property(JsonElement schema, string name) =>
        schema.GetProperty("properties").GetProperty(name);

    private static string[] Required(JsonElement schema) =>
        schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()!).ToArray();

    private static JsonElement Entry() => Property(Schema(), "entries").GetProperty("items");

    private static JsonElement Sense() => Property(Entry(), "senses").GetProperty("items");

    [Fact]
    public void Equivalents_and_false_friends_hang_off_a_sense_not_off_an_entry()
    {
        Assert.Contains("equivalents", Required(Sense()));
        Assert.Contains("false_friends", Required(Sense()));
        Assert.DoesNotContain("equivalents", Required(Entry()));
        Assert.DoesNotContain("false_friends", Required(Entry()));
    }

    [Fact]
    public void A_false_friend_note_must_name_the_language_it_is_about()
    {
        var note = Property(Sense(), "false_friends").GetProperty("items");

        Assert.Contains("language", Required(note));
        Assert.Equal(
            LanguageCatalog.All.Select(l => l.Name),
            Property(note, "language").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void An_equivalent_carries_its_own_language()
    {
        var equivalent = Property(Sense(), "equivalents").GetProperty("items");

        Assert.Equal(new[] { "language", "word" }, Required(equivalent));
    }

    [Fact]
    public void Returning_zero_entries_is_a_legitimate_answer()
    {
        var schema = Schema();

        // The model can only decline to invent an entry if the not-found fields are part of
        // the shape it is asked for, and nothing sets a floor on the number of entries.
        Assert.Equal(new[] { "entries", "note", "suggestion" }, Required(schema));
        Assert.False(Property(schema, "entries").TryGetProperty("minItems", out _));
    }

    [Fact]
    public void Optional_text_is_expressed_as_a_string_or_null_union()
    {
        foreach (var nullable in new[] { Property(Schema(), "note"), Property(Entry(), "lemma_note"), Property(Sense(), "domain") })
        {
            var options = nullable.GetProperty("anyOf").EnumerateArray()
                .Select(o => o.GetProperty("type").GetString());
            Assert.Equal(new[] { "string", "null" }, options);
        }
    }

    [Fact]
    public void Every_object_in_the_schema_closes_additional_properties()
    {
        foreach (var node in new[] { Schema(), Entry(), Sense() })
        {
            Assert.False(node.GetProperty("additionalProperties").GetBoolean());
        }
    }
}

public class LookupPromptTests
{
    private static readonly string[] AllLanguages =
        LanguageCatalog.All.Select(l => l.Name).ToArray();

    [Fact]
    public void Asks_for_one_entry_per_language_in_the_prefetch_set()
    {
        var prompt = PromptBuilder.BuildLookup("inert", AllLanguages);

        Assert.Contains("inert", prompt);
        foreach (var language in AllLanguages)
        {
            Assert.Contains(language, prompt);
        }
    }

    [Fact]
    public void Narrowing_the_prefetch_set_narrows_the_prompt()
    {
        var prompt = PromptBuilder.BuildLookup("inert", ["English", "Swedish"]);

        Assert.Contains("English, Swedish", prompt);
        Assert.DoesNotContain("Hungarian", prompt);
    }

    [Fact]
    public void States_the_sense_cap_as_a_number()
    {
        var prompt = PromptBuilder.BuildLookup("gick", AllLanguages);

        Assert.Contains(LookupSchema.MaxSenses.ToString(), prompt);
    }

    [Fact]
    public void Asks_for_the_lemma_and_for_the_note_when_the_input_was_inflected()
    {
        var prompt = PromptBuilder.BuildLookup("gick", AllLanguages);

        Assert.Contains("lemma", prompt);
        Assert.Contains("gick — past tense of gå", prompt);
    }

    [Fact]
    public void Gives_the_model_a_way_out_for_a_word_that_does_not_exist()
    {
        var prompt = PromptBuilder.BuildLookup("asdfgh", AllLanguages);

        Assert.Contains("return zero entries", prompt);
        Assert.Contains("suggestion", prompt);
    }
}
