using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Translator.Core;
using Translator.Desktop.Models;
using Translator.Desktop.Services;
using Translator.Desktop.ViewModels;
using Translator.Desktop.Views;

namespace Translator.Desktop.Tests;

public class SettingsTests
{
    private static SettingsStore FreshStore() =>
        new(Path.Combine(Path.GetTempPath(), $"translator-test-{Guid.NewGuid():N}.json"));

    [Fact]
    public void The_prefetch_set_is_saved_and_read_back()
    {
        var store = FreshStore();
        var vm = new MainWindowViewModel(store);

        foreach (var option in vm.PrefetchLanguages.Where(l => l.Name is not ("English" or "Swedish")))
            option.IsChecked = false;

        Assert.Equal(["English", "Swedish"], store.Load().Prefetch);
    }

    [Fact]
    public void The_last_prefetch_language_cannot_be_switched_off()
    {
        var store = FreshStore();
        var vm = new MainWindowViewModel(store);

        foreach (var option in vm.PrefetchLanguages) option.IsChecked = false;

        Assert.NotEmpty(store.Load().Prefetch);
        Assert.NotEmpty(vm.Error);
    }

    [Fact]
    public void Choosing_a_model_saves_it()
    {
        var store = FreshStore();
        var vm = new MainWindowViewModel(store) { SelectedModel = "claude-haiku-4-5" };

        Assert.Equal("claude-haiku-4-5", store.Load().Model);
    }

    [Fact]
    public void A_model_from_the_settings_file_is_offered_even_if_it_is_not_on_the_shortlist()
    {
        var store = FreshStore();
        store.Save(new AppSettings { Model = "claude-opus-4-1" });

        var vm = new MainWindowViewModel(store);

        Assert.Equal("claude-opus-4-1", vm.SelectedModel);
        Assert.Contains("claude-opus-4-1", vm.Models);
        Assert.Contains(ModelCatalog.Default, vm.Models);
    }

    [Fact]
    public void Both_kinds_of_work_raise_the_same_busy_flag()
    {
        var vm = new MainWindowViewModel(FreshStore());
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Assert.False(vm.IsWorking);
        vm.IsLookingUp = true;
        Assert.True(vm.IsWorking);
        Assert.Contains(nameof(vm.IsWorking), changed);
    }
}

[Trait("Category", "Avalonia")]
public class InputKeyTests(HeadlessSession fixture) : IClassFixture<HeadlessSession>
{
    private static SettingsStore FreshStore() =>
        new(Path.Combine(Path.GetTempPath(), $"translator-test-{Guid.NewGuid():N}.json"));

    private readonly HeadlessUnitTestSession _session = fixture.Session;

    [Fact]
    public Task Shift_Enter_breaks_the_line_and_Enter_does_not() => _session.Dispatch(() =>
    {
        var vm = new MainWindowViewModel(FreshStore());
        var window = new MainWindow { DataContext = vm, Width = 680, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var input = window.GetVisualDescendants().OfType<TextBox>()
            .First(t => t.Classes.Contains("bare"));
        input.Focus();
        window.KeyTextInput("hej");
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.Shift);
        window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.Shift);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("\n", input.Text ?? "");

        var beforeEnter = input.Text;
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        // Enter submits rather than typing: it must not add a second line break. (The translation
        // itself fails immediately here — there is no API key in this settings file.)
        Assert.Equal(beforeEnter?.Count(c => c == '\n'), input.Text?.Count(c => c == '\n'));
    }, CancellationToken.None);
}
