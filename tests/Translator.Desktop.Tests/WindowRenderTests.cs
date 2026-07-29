using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Translator.Desktop.Services;
using Translator.Desktop.ViewModels;
using Translator.Desktop.Views;

namespace Translator.Desktop.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

// Draws the real window against a settings file the test owns. This is the only check that the
// XAML loads, that every binding resolves at runtime, and that the Panel lays out — none of
// which a test on the view models can tell you.
// Avalonia can only be set up once per process, and a session that is started per test leaves a
// thread behind that keeps the test host alive. One session, shared by the class.
public sealed class HeadlessSession : IDisposable
{
    public HeadlessUnitTestSession Session { get; } = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));

    public void Dispose() => Session.Dispose();
}

[Trait("Category", "Avalonia")]
public class WindowRenderTests(HeadlessSession fixture) : IClassFixture<HeadlessSession>
{
    private static readonly string OutputDirectory =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    private readonly HeadlessUnitTestSession _session = fixture.Session;

    // Never the real settings file: the store is pointed at a fixture, or at nothing at all.
    private static SettingsStore Store() =>
        new(Environment.GetEnvironmentVariable("TRANSLATOR_TEST_SETTINGS")
            ?? Path.Combine(OutputDirectory, "no-such-settings.json"));

    [Fact]
    public Task The_window_draws_with_the_panel_closed() => _session.Dispatch(() =>
    {
        var window = new MainWindow { DataContext = new MainWindowViewModel(Store()), Width = 680, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Save(window, "panel-closed.png");
    }, CancellationToken.None);

    [Fact]
    public Task The_window_draws_with_the_panel_open() => _session.Dispatch(() =>
    {
        var vm = new MainWindowViewModel(Store());
        var window = new MainWindow { DataContext = vm, Width = 1080, Height = 900 };
        window.Show();

        // Without a fixture there is nothing to look up; the closed-panel test still covers the XAML.
        if (vm.Trail.Count == 0) return;

        // Open the Panel the way a user does: by clicking a word on the Word Trail.
        vm.LookUpChipCommand.Execute(vm.Trail.First());
        Dispatcher.UIThread.RunJobs();

        Assert.True(vm.IsPanelOpen);
        Assert.Equal(2, vm.PanelEntries.Count);

        Save(window, "panel-open.png");
    }, CancellationToken.None);

    // The panel is built against the dark frame in the mock — that is the environment it is used in.
    [Fact]
    public Task The_panel_draws_in_the_dark_theme() => _session.Dispatch(() =>
    {
        var vm = new MainWindowViewModel(Store());
        var window = new MainWindow { DataContext = vm, Width = 1080, Height = 900 };
        window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        window.Show();

        if (vm.Trail.Count == 0) return;

        vm.LookUpChipCommand.Execute(vm.Trail.First());
        Dispatcher.UIThread.RunJobs();

        Save(window, "panel-open-dark.png");
    }, CancellationToken.None);

    [Fact]
    public Task The_settings_card_draws_with_the_model_and_prefetch_controls() => _session.Dispatch(() =>
    {
        var vm = new MainWindowViewModel(Store()) { ShowSettings = true };
        var window = new MainWindow { DataContext = vm, Width = 680, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(6, vm.PrefetchLanguages.Count);
        Save(window, "settings-open.png");
    }, CancellationToken.None);

    [Fact]
    public Task The_panel_shows_a_moving_line_while_a_lookup_is_in_flight() => _session.Dispatch(() =>
    {
        var vm = new MainWindowViewModel(Store()) { IsPanelOpen = true, IsLookingUp = true, PanelWord = "trögare" };
        var window = new MainWindow { DataContext = vm, Width = 1080, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(vm.IsWorking);
        Save(window, "panel-looking-up.png");

        // The line is on screen: its track runs the width of the panel. That the indicator also
        // *moves* cannot be checked here — headless renders every frame identically, animation
        // clock included — so motion is only ever seen in the running app.
        var track = Row(window, ProgressLineY);
        Assert.True(track.Contains(AccentSoftRgb) || track.Contains(AccentSoftBgr),
            "the progress line's track was not drawn across the panel");
    }, CancellationToken.None);

    // The row the panel's progress line sits on, in the 1080x900 window above, and the light
    // theme's AccentSoftBrush in both byte orders.
    private const int ProgressLineY = 57;
    private const string AccentSoftRgb = "EEF0FE";
    private const string AccentSoftBgr = "FEF0EE";

    private static string Row(Window window, int y)
    {
        using var frame = window.CaptureRenderedFrame()!;
        using var buffer = new MemoryStream();
        frame.Save(buffer);
        var pixels = new byte[frame.PixelSize.Width * 4];
        using var locked = frame.Lock();
        System.Runtime.InteropServices.Marshal.Copy(
            locked.Address + y * locked.RowBytes, pixels, 0, pixels.Length);
        return Convert.ToHexString(pixels);
    }

    private static void Save(Window window, string name)
    {
        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(Path.Combine(OutputDirectory, name));
    }
}
