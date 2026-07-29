using System.Text.Json;
using Translator.Core;
using Translator.Desktop.Models;
using Translator.Desktop.Services;

namespace Translator.Desktop.Tests;

public class WordTrailTests
{
    private static readonly string[] AllLanguages = ["English", "Swedish", "Hungarian", "German", "French", "Spanish"];

    private static LookupResult ResultFor(params string[] headwords) =>
        new([.. headwords.Select(h => new Entry(h, "Swedish", "", "verb", []))]);

    private static LookupResult NotFound(string? suggestion = null) =>
        new([], "Not a word.", suggestion);

    [Fact]
    public void A_word_is_filed_under_its_lemma_not_under_what_was_typed()
    {
        List<TrailWord> trail = [];

        WordTrail.Remember(trail, "gick", ResultFor("gå"), AllLanguages);

        Assert.Equal("gå", trail.Single().Word);
        Assert.NotNull(WordTrail.Find(trail, "gå"));
    }

    [Fact]
    public void Looking_the_same_word_up_twice_is_one_row_not_two()
    {
        List<TrailWord> trail = [];

        WordTrail.Remember(trail, "gick", ResultFor("gå"), AllLanguages);
        WordTrail.Remember(trail, "gå", ResultFor("gå"), AllLanguages);

        Assert.Single(trail);
    }

    [Fact]
    public void The_newest_word_is_first()
    {
        List<TrailWord> trail = [];

        WordTrail.Remember(trail, "trög", ResultFor("trög"), AllLanguages);
        WordTrail.Remember(trail, "inert", ResultFor("inert"), AllLanguages);

        Assert.Equal(["inert", "trög"], trail.Select(w => w.Word));
    }

    [Fact]
    public void A_word_that_falls_off_the_end_takes_its_entries_with_it()
    {
        List<TrailWord> trail = [];

        foreach (var i in Enumerable.Range(0, WordTrail.MaxLength + 5))
            WordTrail.Remember(trail, $"word{i}", ResultFor($"word{i}"), AllLanguages);

        Assert.Equal(WordTrail.MaxLength, trail.Count);
        Assert.Null(WordTrail.Find(trail, "word0"));
    }

    [Fact]
    public void A_word_the_model_does_not_know_is_still_remembered_under_what_was_typed()
    {
        List<TrailWord> trail = [];

        WordTrail.Remember(trail, "asdfgh", NotFound(), AllLanguages);

        var stored = WordTrail.Find(trail, "asdfgh");
        Assert.NotNull(stored);
        Assert.False(stored.HasEntries);
        Assert.Equal("Not a word.", stored.Result.Note);
    }

    [Fact]
    public void Find_ignores_case_and_surrounding_space()
    {
        List<TrailWord> trail = [];
        WordTrail.Remember(trail, "Inert", ResultFor("inert"), AllLanguages);

        Assert.NotNull(WordTrail.Find(trail, "  INERT "));
    }

    [Fact]
    public void A_word_fetched_for_fewer_languages_than_are_enabled_is_stale()
    {
        List<TrailWord> trail = [];
        WordTrail.Remember(trail, "inert", ResultFor("inert"), ["English", "Swedish"]);
        var stored = WordTrail.Find(trail, "inert")!;

        Assert.False(WordTrail.IsStale(stored, ["English", "Swedish"]));
        Assert.False(WordTrail.IsStale(stored, ["Swedish"]));
        Assert.True(WordTrail.IsStale(stored, ["English", "German"]));
    }

    [Fact]
    public void Coverage_is_what_was_asked_for_not_what_came_back()
    {
        List<TrailWord> trail = [];

        // "inert" does not exist in Hungarian, so no Hungarian entry comes back — that is an
        // answer, not a gap, and must not make the stored word look stale.
        WordTrail.Remember(trail, "inert", ResultFor("inert"), AllLanguages);

        Assert.False(WordTrail.IsStale(WordTrail.Find(trail, "inert")!, AllLanguages));
    }
}

public class AppSettingsTests
{
    [Fact]
    public void A_settings_file_written_before_the_dictionary_existed_still_loads()
    {
        var json = """
            {"ApiKey":"sk-ant-x","Style":"","Model":"claude-opus-4-8",
             "Targets":["English","Swedish"],"History":[]}
            """;

        var settings = JsonSerializer.Deserialize<AppSettings>(json)!;

        Assert.Equal("sk-ant-x", settings.ApiKey);
        Assert.Equal("claude-opus-4-8", settings.Model);
        Assert.Empty(settings.Trail);
        Assert.Equal(6, settings.Prefetch.Count);
    }

    [Fact]
    public void A_stored_trail_round_trips_through_the_settings_file()
    {
        var settings = new AppSettings();
        WordTrail.Remember(
            settings.Trail,
            "inert",
            new LookupResult([
                new Entry("inert", "English", "/ɪˈnɜːt/", "adjective", [
                    new Sense("slow to act", "The committee was inert.",
                        [new Equivalent("Swedish", "trög")], ["sluggish"], ["active"],
                        [new FalseFriendNote("German", "inert", "German inert is chemical only.")])
                ])
            ]),
            ["English", "Swedish"]);

        var reloaded = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;

        var sense = reloaded.Trail.Single().Result.Entries.Single().Senses.Single();
        Assert.Equal("trög", sense.Equivalents.Single().Word);
        Assert.Equal("German", sense.FalseFriends.Single().Language);
        Assert.Equal(["English", "Swedish"], reloaded.Trail.Single().Coverage);
    }
}
