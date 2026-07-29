using Avalonia.Controls.Documents;
using Translator.Core;
using Translator.Desktop.ViewModels;

namespace Translator.Desktop.Tests;

public class PanelViewModelTests
{
    private static Sense ChemistrySense() => new(
        "chemically inactive",
        "Argon is an inert gas.",
        [new Equivalent("Swedish", "inert"), new Equivalent("German", "inert")],
        ["unreactive"],
        ["reactive"],
        [new FalseFriendNote("German", "träge", "German träge means sluggish, not unreactive.")],
        "chemistry");

    private static Entry InertEntry() =>
        new("inert", "English", "/ɪˈnɜːt/", "adjective", [ChemistrySense()]);

    [Fact]
    public void An_equivalent_in_a_language_that_is_switched_off_is_not_shown()
    {
        var sense = new PanelSense(1, ChemistrySense(), InertEntry(), ["English", "Swedish"]);

        Assert.Equal(["Swedish"], sense.Equivalents.Select(e => e.Language));
    }

    [Fact]
    public void A_warning_about_a_language_that_is_switched_off_is_not_shown()
    {
        var withGerman = new PanelSense(1, ChemistrySense(), InertEntry(), ["English", "German"]);
        var withoutGerman = new PanelSense(1, ChemistrySense(), InertEntry(), ["English", "Swedish"]);

        Assert.Single(withGerman.Warnings);
        Assert.Empty(withoutGerman.Warnings);
    }

    [Fact]
    public void Equivalents_in_the_same_language_share_one_row()
    {
        var sense = new PanelSense(
            1,
            ChemistrySense() with { Equivalents = [new Equivalent("Swedish", "inert"), new Equivalent("Swedish", "reaktionströg")] },
            InertEntry(),
            ["Swedish"]);

        var row = Assert.Single(sense.Equivalents);
        Assert.Equal("inert · reaktionströg", row.Text);
        Assert.Equal("🇸🇪", row.Flag);
    }

    [Fact]
    public void The_headword_is_marked_inside_its_example()
    {
        var sense = new PanelSense(1, ChemistrySense(), InertEntry(), ["English"]);

        var runs = sense.ExampleInlines.OfType<Run>().ToList();
        Assert.Equal(["Argon is an ", "inert", " gas."], runs.Select(r => r.Text));
        Assert.Equal(Avalonia.Media.FontWeight.Bold, runs[1].FontWeight);
    }

    [Fact]
    public void An_example_that_does_not_contain_the_headword_is_left_alone()
    {
        var sense = new PanelSense(
            1, ChemistrySense() with { Example = "Argon does not react." }, InertEntry(), ["English"]);

        Assert.Equal("Argon does not react.", Assert.Single(sense.ExampleInlines.OfType<Run>()).Text);
    }

    [Fact]
    public void The_meta_line_reads_pronunciation_part_of_speech_and_language()
    {
        var entry = new PanelEntry(InertEntry(), ["English"]);

        Assert.Equal("/ɪˈnɜːt/  ·  adjective  ·  🇬🇧 English", entry.Meta);
    }

    [Fact]
    public void Source_links_are_scoped_to_the_entrys_language()
    {
        var english = new PanelEntry(InertEntry(), ["English"]);
        var swedish = new PanelEntry(
            new Entry("trög", "Swedish", "/trøːɡ/", "adjective", []), ["Swedish"]);
        var french = new PanelEntry(
            new Entry("inerte", "French", "/inɛʁt/", "adjectif", []), ["French"]);

        Assert.Equal(["OED", "SZTAKI"], english.Links.Select(l => l.Label));
        Assert.Contains("oed.com/dictionary/inert_adj", english.Links[0].Url);
        Assert.Equal(["SAOL"], swedish.Links.Select(l => l.Label));
        Assert.Contains("svenska.se", swedish.Links[0].Url);
        Assert.Empty(french.Links);
    }

    [Fact]
    public void A_headword_with_non_ascii_letters_is_escaped_into_its_link()
    {
        var entry = new PanelEntry(new Entry("gå", "Swedish", "/ɡoː/", "verb", []), ["Swedish"]);

        Assert.Contains("q=g%C3%A5", entry.Links[0].Url);
    }

    [Fact]
    public void An_equivalent_carries_its_own_language_so_clicking_it_opens_that_entry()
    {
        var sense = new PanelSense(1, ChemistrySense(), InertEntry(), ["English", "Swedish", "German"]);

        Assert.All(sense.Equivalents, row =>
            Assert.All(row.Words, word => Assert.Equal(row.Language, word.Language)));
        Assert.Contains(sense.Equivalents.SelectMany(r => r.Words), w => w == new WordChip("inert", "Swedish"));
    }

    [Fact]
    public void Synonyms_and_antonyms_carry_the_entrys_own_language()
    {
        var sense = new PanelSense(1, ChemistrySense(), InertEntry(), ["English"]);

        Assert.Equal([new WordChip("unreactive", "English")], sense.Synonyms);
        Assert.Equal([new WordChip("reactive", "English")], sense.Antonyms);
    }

    [Fact]
    public void Senses_are_numbered_from_one_in_the_order_they_came_back()
    {
        var entry = new PanelEntry(
            InertEntry() with { Senses = [ChemistrySense(), ChemistrySense() with { Domain = null }] },
            ["English"]);

        Assert.Equal(["1", "2"], entry.Senses.Select(s => s.Number));
        Assert.Equal("CHEMISTRY", entry.Senses[0].Domain);
        Assert.False(entry.Senses[1].HasDomain);
    }
}
