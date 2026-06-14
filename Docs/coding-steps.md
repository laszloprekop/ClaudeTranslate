# Coding Steps: Translate (EN ⇄ SV)

**Stack:** C# / .NET 10 · Anthropic SDK · Avalonia (MVVM) · ASP.NET Core (minimal API) · xUnit
**Design plan:** locked design from the grilling session (see `materials/CLAUDE.md` for original app context)
**Generated:** 2026-06-10

## Architecture at a glance

- **`Translator.Core`** — class library. The only place that talks to Anthropic (official C# SDK). Builds the prompt, calls the API with **structured outputs**, returns a guaranteed-shape `TranslationResult`. Model is configurable (default `claude-sonnet-4-6`).
- **`Translator.Desktop`** — Avalonia app (cross-platform native UI). Mirrors `translate.html`. Calls `Translator.Core` directly. Persists key + writing style + history to a JSON file in the OS app-data dir.
- **`Translator.Web`** — ASP.NET Core. Serves a stripped `translate.html` and a `POST /api/translate` endpoint that calls `Translator.Core` with a server-held key. Private use, behind simple auth. Deploys to Coolify/Hetzner.
- **`Translator.Core.Tests`** — xUnit. Covers prompt building and result mapping (no network).

> **Working convention:** all commands assume `fish` on macOS Apple Silicon, run from the repo root `~/dev/ClaudeTranslate`. The existing `materials/` folder stays as reference — the new solution lives alongside it under `src/`.

---

## Phase 0 — Walking Skeleton

> **Walking skeleton:** the thinnest possible end-to-end slice that proves everylayer can talk to the next — with stub implementations  throughout. No real logic yet; stubs and hardcoded returns everywhere. The only goal of this phase is: each app boots, shows one screen / responds to one request, and does not crash.

> Every commit in this phase is type `chore`. No `feat` until Phase 1.

### Step 1: Scaffold the solution, projects, and git

> **New concept: solution & projects.** A .NET *solution* (`.sln`) is just a manifest grouping *projects*. Each project is one `.csproj` that compiles to one assembly (a `.dll` or executable). A *class library* has no entry point — it's referenced by apps. This is the C# equivalent of a workspace with packages.

Create the repo, the solution, and the four projects, then wire up project references so the front-ends and tests can see Core.

```fish
# from ~/dev/ClaudeTranslate
git init
dotnet new sln -n Translator

# Core library + its tests
dotnet new classlib -o src/Translator.Core
dotnet new xunit    -o tests/Translator.Core.Tests

# Web front-end (minimal API)
dotnet new web -o src/Translator.Web

# Avalonia desktop front-end (install the template pack first)
dotnet new install Avalonia.Templates
dotnet new avalonia.mvvm -o src/Translator.Desktop

# Register every project in the solution
dotnet sln add src/Translator.Core src/Translator.Web src/Translator.Desktop tests/Translator.Core.Tests

# Wire references: everything that uses Core points at it
dotnet add src/Translator.Web        reference src/Translator.Core
dotnet add src/Translator.Desktop    reference src/Translator.Core
dotnet add tests/Translator.Core.Tests reference src/Translator.Core
```

Add a `.gitignore` for .NET so build output never gets committed:

```fish
dotnet new gitignore
```

**Verify:** `dotnet build` — the solution builds (4 projects, 0 errors).

**Commit:** `chore(repo): scaffold solution with core, desktop, web, and tests`

```shell
git add .
git commit -m "chore(repo): scaffold solution with core, desktop, web, and tests"
```

**Create a GitHub repo**, configure and push:

```shell
gh repo create ClaudeTranslate --public --source=. -remote=origin --push
```

 - `--source=.` uses the current directory as the repo source                                     
  - `--remote=origin` wires up the origin remote                                                   
  - `--push` pushes existing commits immediately                                               

  Swap `--public` for `--private` next time if you want a private repo.

### Step 2: Stub `Translator.Core`

> **New concept: interface + record.** An `interface` is a contract with no implementation — front-ends depend on `ITranslator`, not the concrete class, so they're testable and swappable (this is what makes dependency injection work later). A `record` is an immutable data class with value equality and a one-line declaration — ideal for a result DTO.

Delete the template's `Class1.cs`. Define the contract, the result shape, and a stub implementation that returns a hardcoded translation so the front-ends have something to call.

```csharp
// src/Translator.Core/TranslationResult.cs  (new file)
namespace Translator.Core;

public class TranslationResult
{
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
    public string translation { get; set; } = "";   // ← mistake: C# properties are PascalCase
}
```

```csharp
// src/Translator.Core/ITranslator.cs  (new file)
namespace Translator.Core;

public interface ITranslator
{
    Task<TranslationResult> TranslateAsync(string text, string styleGuide);
}
```

```csharp
// src/Translator.Core/Translator.cs  (new file)
namespace Translator.Core;

public class Translator : ITranslator
{
    public Task<TranslationResult> TranslateAsync(string text, string styleGuide)
    {
        // Stub: echo back, pretend we detected English.
        var result = new TranslationResult ( Source = "English", Target = "Swedish", Translation = $"[stub] {text}" );
        return Task.FromResult(result);
    }
}
```

**Verify:** `dotnet build src/Translator.Core` — fails first on the lowercase property, then builds after you fix `translation` → `Translation`.

**Commit:** `chore(core): stub ITranslator with hardcoded result`

---

### Step 3: Stub the desktop window

> **New concept: Avalonia & XAML.** Avalonia is a cross-platform native UI framework (macOS/Windows/Linux from one codebase). UI is declared in `.axaml` (XAML) — an XML dialect — and backed by C#. The `avalonia.mvvm` template already gave you `App.axaml`, `MainWindow.axaml`, and `MainWindowViewModel.cs`.

Replace the template's placeholder window content with a textbox, a button, and an output label. No logic yet — just prove the window renders.

```xml
<!-- src/Translator.Desktop/Views/MainWindow.axaml  (replace the root content) -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:Translator.Desktop.ViewModels"
        x:Class="Translator.Desktop.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Width="620" Height="480" Title="Translate · EN ⇄ SV">
  <StackPanel Margin="16" Spacing="12">
    <TextBox x:Name="Input" AcceptsReturn="True" Watermark="Type in English or Swedish…" />
    <Button Content="Translate" />
    <TextBlock x:Name="Output" Text="(output appears here)" />
  </StackPanel>
</Window>
```

**Verify:** `dotnet run --project src/Translator.Desktop` — a window opens with the textbox, button, and placeholder text. Close it.

**Commit:** `chore(desktop): stub main window layout`

---

### Step 4: Wire the desktop button to the Core stub

> **New concept: code-behind event handler.** Every `.axaml` has a `.axaml.cs` "code-behind" partial class. For the skeleton we'll attach a click handler directly there (we move to proper MVVM commands in Phase 1). This is the wiring step — one user action flowing Desktop → Core → back to the UI.

Give the button a name, then handle its click in code-behind by calling the Core stub.

```xml
<!-- src/Translator.Desktop/Views/MainWindow.axaml  (edit the Button line) -->
    <Button Content="Translate" Click="OnTranslateClick" />
```

```csharp
// src/Translator.Desktop/Views/MainWindow.axaml.cs  (add inside the class)
using Translator.Core;
// ... existing using directives ...

    private readonly ITranslator _translator = new Translator.Core.Translator();

    private async void OnTranslateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var result = await _translator.TranslateAsync(Input.Text ?? "", "");
        Output.Text = $"{result.Source} → {result.Target}: {result.Translation}";
    }
```

**Verify:** `dotnet run --project src/Translator.Desktop` — type text, click Translate, see `English → Swedish: [stub] <your text>`.

**Commit:** `chore(desktop): wire button to core stub`

---

### Step 5: Stub the web front-end

> **New concept: ASP.NET minimal API + static files.** `dotnet new web` gives a minimal-API `Program.cs` — no controllers, just `app.MapGet/MapPost`. `app.UseDefaultFiles()` + `app.UseStaticFiles()` serve files from `wwwroot/`. We'll serve a copy of `translate.html` and add a stub `POST /api/translate`.

Copy the page into `wwwroot`, then replace `Program.cs` with a static-file host plus a stub endpoint that calls Core.

```fish
mkdir -p src/Translator.Web/wwwroot
cp materials/translate.html src/Translator.Web/wwwroot/index.html
```

```csharp
// src/Translator.Web/Program.cs  (replace the whole file)
using Translator.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITranslator, Translator.Core.Translator>();
var app = builder.Build();

app.UseDefaultFiles();   // serve index.html at "/"
app.UseStaticFiles();

app.MapPost("/api/translate", async (TranslateRequest req, ITranslator translator) =>
{
    var result = await translator.TranslateAsync(req.Text, req.Style ?? "");
    return Results.Ok(result);
});

app.Run();

record TranslateRequest(string Text, string? Style);
```

**Verify:** `dotnet run --project src/Translator.Web`, then in another shell:
`curl -s -X POST localhost:5000/api/translate -H 'content-type: application/json' -d '{"text":"hej"}'` → returns the stub JSON. Visiting `http://localhost:5000` shows the page. (Note the real port printed on startup.)

**Commit:** `chore(web): serve page and stub /api/translate`

> ✅ **Skeleton walks.** All three projects boot, and one action flows end-to-end through Core in each. Phase 1 begins.

---

## Phase 1 — MVP

> Goal: flesh out one complete vertical slice at a time until every mandatory
> requirement is met. Each feature gets a **Stub** pass then an **Implement**
> pass where it helps; data classes are single-pass **Add** steps. Never move on
> until the current slice works end-to-end.

### Step 6: Add the result record and structured-output schema

> **New concept: structured outputs.** Instead of asking the model for JSON and hoping (the original app stripped ` ```json ` fences and sliced from `{` to `}`), we pass a JSON Schema in `output_config.format`. The API then *guarantees* the response is valid JSON of that shape — no fence-stripping, no fallback parsing.

Replace the stub class with a proper immutable record, and centralize the schema that both the request and the test will use.

```csharp
// src/Translator.Core/TranslationResult.cs  (replace the file)
using System.Text.Json.Serialization;

namespace Translator.Core;

// JsonPropertyName maps the model's lowercase JSON keys onto PascalCase members.
public record TranslationResult(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("translation")] string Translation);
```

```csharp
// src/Translator.Core/TranslationSchema.cs  (new file)
using System.Text.Json;

namespace Translator.Core;

internal static class TranslationSchema
{
    // The shape the model must return. Keys are lowercase to match the prompt.
    public static Dictionary<string, JsonElement> Build() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            source = new { type = "string", @enum = new[] { "English", "Swedish" } },
            target = new { type = "string", @enum = new[] { "English", "Swedish" } },
            translation = new { type = "string" },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "source", "target", "translation" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };
}
```

**Verify:** `dotnet build src/Translator.Core` — builds clean. (`Translator.cs` still returns the stub via the new constructor — adjust it to `new TranslationResult("English", "Swedish", $"[stub] {text}")` so it compiles.)

**Commit:** `feat(core): add TranslationResult record and output schema`

---

### Step 7: Implement the prompt builder

The prompt is the one piece of logic worth unit-testing in isolation, so pull it into a pure static method (no I/O) before touching the network.

```csharp
// src/Translator.Core/PromptBuilder.cs  (new file)
namespace Translator.Core;

public static class PromptBuilder
{
    public const string DefaultStyle =
        "Natural, idiomatic, slightly casual. Sound like a fluent bilingual person " +
        "writing quickly to a colleague. Translate intent, not words. Leave technical " +
        "terms and proper nouns untouched.";

    public static string Build(string text, string styleGuide)
    {
        var style = string.IsNullOrWhiteSpace(styleGuide) ? DefaultStyle : styleGuide;
        return $"""
        You translate between English and Swedish for a fluent bilingual user.

        Step 1: Detect the input language. If English, translate to Swedish. If Swedish, translate to English.
        Step 2: Translate following this style guide exactly:
        {style}

        Input:
        \"\"\"{text}\"\"\"
        """;
    }
}
```

**Verify:** `dotnet build src/Translator.Core` — builds clean.

**Commit:** `feat(core): add pure prompt builder`

---

### Step 8: Test the prompt builder and result mapping

> **New concept: xUnit.** xUnit is the standard .NET test runner. `[Fact]` marks a parameterless test; `[Theory]` + `[InlineData]` runs the same test with multiple inputs. `dotnet test` discovers and runs them. Because `PromptBuilder` and the schema are pure, these tests need no network.

Delete the template's `UnitTest1.cs` and add real coverage for the prompt and the JSON round-trip.

```csharp
// tests/Translator.Core.Tests/PromptBuilderTests.cs  (new file)
using System.Text.Json;
using Translator.Core;
using Xunit;

public class PromptBuilderTests
{
    [Fact]
    public void Build_uses_default_style_when_blank()
    {
        var prompt = PromptBuilder.Build("hej", "   ");
        Assert.Contains(PromptBuilder.DefaultStyle, prompt);
        Assert.Contains("hej", prompt);
    }

    [Fact]
    public void TranslationResult_deserializes_from_lowercase_json()
    {
        var json = """{"source":"Swedish","target":"English","translation":"hi"}""";
        var result = JsonSerializer.Deserialize<TranslationResult>(json)!;
        Assert.Equal("Swedish", result.Source);
        Assert.Equal("hi", result.Translation);
    }
}
```

**Verify:** `dotnet test` — both tests pass.

**Commit:** `test(core): cover prompt building and result mapping`

---

### Step 9: Implement the real Anthropic call

> **New concept: async/await + `Task`.** A method returning `Task<T>` is asynchronous; `await` suspends without blocking the thread until the result arrives. **Every `await` must be on a method whose caller is `async`** — forgetting `await` returns the unfinished `Task` instead of the value, a classic bug coming from JS where promises often resolve implicitly.

Add the SDK package, then replace the stub body with a real call that passes the schema and deserializes the guaranteed-shape JSON.

```fish
dotnet add src/Translator.Core package Anthropic
```

```csharp
// src/Translator.Core/Translator.cs  (replace the file)
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace Translator.Core;

public class Translator(string apiKey, string model = "claude-sonnet-4-6") : ITranslator
{
    private readonly AnthropicClient _client = new() { ApiKey = apiKey };

    public async Task<TranslationResult> TranslateAsync(string text, string styleGuide)
    {
        var parameters = new MessageCreateParams
        {
            Model = model,                 // Model converts implicitly from the id string
            MaxTokens = 1000,
            Messages = [new() { Role = Role.User, Content = PromptBuilder.Build(text, styleGuide) }],
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = TranslationSchema.Build() },
            },
        };

        var response = _client.Messages.Create(parameters);   // ← mistake: missing await
        var json = response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text;
        return JsonSerializer.Deserialize<TranslationResult>(json)!;
    }
}
```

**Verify:** `dotnet build` fails first because you're calling `.Content` on a `Task` — add `await` to the `_client.Messages.Create(parameters)` call. It then fails again on the two existing callers, because `Translator` now needs an `apiKey` argument. Update both — they take different shapes:

*Desktop code-behind (from Step 4)* — the translator is a field initializer, so substitute the env-var read directly where the no-arg constructor was. Add `using System;` (this project has no implicit usings) so `Environment` resolves:

```csharp
// src/Translator.Desktop/Views/MainWindow.axaml.cs
using System;
// ... existing using directives ...

    private readonly ITranslator _translator = new Translator.Core.Translator(
        Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "");
```

*Web `Program.cs` (from Step 5)* — this goes through DI, so the two-type-argument registration no longer works (the container can't supply the `apiKey`). Switch to the factory overload that constructs `Translator` explicitly:

```csharp
// src/Translator.Web/Program.cs  (replace the AddSingleton line)
var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "";
builder.Services.AddSingleton<ITranslator>(_ => new Translator.Core.Translator(apiKey));
```

Both key reads are temporary: the desktop reads it from saved settings in the settings slice, and Step 17 replaces the web version with a config-based key. With a real key set in the environment, `dotnet run --project src/Translator.Desktop` produces a genuine translation.

**Commit:** `feat(core): call Anthropic with structured outputs`

---

### Step 10: Stub the settings model and JSON config store

> **New concept: per-user app-data + JSON config.** `Environment.GetFolderPath(SpecialFolder.ApplicationData)` resolves to the platform's per-user config dir (on macOS under .NET that's `~/Library/Application Support`; on Windows `%AppData%`; on Linux `~/.config`). We persist a small JSON document there — same security posture as the original `localStorage`, fine for a personal local tool.

Add the settings shape and a store with stubbed load/save (return defaults, do nothing on save) so the UI can bind before the file logic exists.

```csharp
// src/Translator.Desktop/Models/AppSettings.cs  (new file)
namespace Translator.Desktop.Models;

public class AppSettings
{
    public string ApiKey { get; set; } = "";
    public string Style { get; set; } = "";
    public string Model { get; set; } = "claude-sonnet-4-6";
    public List<HistoryItem> History { get; set; } = new();
}

public record HistoryItem(string Input, string Source, string Target, string Translation);
```

```csharp
// src/Translator.Desktop/Services/SettingsStore.cs  (new file)
using Translator.Desktop.Models;

namespace Translator.Desktop.Services;

public class SettingsStore
{
    public AppSettings Load() => new();              // stub
    public void Save(AppSettings settings) { }       // stub
}
```

**Verify:** `dotnet build src/Translator.Desktop` — builds clean.

**Commit:** `feat(desktop): stub settings model and store`

---

### Step 11: Implement config load/save

Replace the stubs with real JSON file I/O under a `Translator/` folder in app-data, creating the directory on first run.

```csharp
// src/Translator.Desktop/Services/SettingsStore.cs  (replace the file)
using System.Text.Json;
using Translator.Desktop.Models;

namespace Translator.Desktop.Services;

public class SettingsStore
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Translator", "settings.json");

    public AppSettings Load()
    {
        if (!File.Exists(Path)) return new AppSettings();
        var json = File.ReadAllText(Path);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(settings,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
```

**Verify:** temporarily call `new SettingsStore().Save(new AppSettings { ApiKey = "test" })` from `App` startup, run once, and confirm the settings file exists (on macOS: `~/Library/Application Support/Translator/settings.json`). Remove the temporary call.

**Commit:** `feat(desktop): persist settings to JSON in app-data`

---

### Step 12: Build the main translate view

Rebuild `MainWindow.axaml` to match the real layout — direction tag, output area, copy button, a recents list, and a settings toggle — bound to the ViewModel (filled in next step). Bindings reference properties that won't exist until Step 13; that's expected.

```xml
<!-- src/Translator.Desktop/Views/MainWindow.axaml  (replace root content) -->
<StackPanel Margin="16" Spacing="12">
  <TextBox Text="{Binding InputText}" AcceptsReturn="True"
           Watermark="Type in English or Swedish…" KeyDown="OnInputKeyDown" />
  <Button Content="Translate" Command="{Binding TranslateCommand}"
          IsEnabled="{Binding !IsBusy}" />
  <TextBlock Text="{Binding Direction}" FontWeight="SemiBold" />
  <TextBlock Text="{Binding Output}" TextWrapping="Wrap" />
  <Button Content="Copy" Command="{Binding CopyCommand}" />
  <TextBlock Text="{Binding Error}" Foreground="#d14343" IsVisible="{Binding HasError}" />
  <ItemsControl ItemsSource="{Binding Recents}">
    <ItemsControl.ItemTemplate>
      <DataTemplate><TextBlock Text="{Binding Translation}" /></DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</StackPanel>
```

**Verify:** `dotnet build src/Translator.Desktop` — expect binding-related build to still succeed (Avalonia bindings resolve at runtime); it compiles. Window won't be functional until Step 13.

**Commit:** `feat(desktop): lay out main translate view with bindings`

---

### Step 13: Implement the MainViewModel

> **New concept: MVVM with CommunityToolkit.** The template uses `CommunityToolkit.Mvvm`. `[ObservableProperty]` on a field generates a bindable property + change notifications; `[RelayCommand]` turns a method into an `ICommand` the XAML `Command="..."` binds to. An `async` relay command method becomes an async command automatically — no `async void`.

Replace the template ViewModel with translate logic, busy/error state, and the recents list. Load settings on construction.

```csharp
// src/Translator.Desktop/ViewModels/MainWindowViewModel.cs  (replace the file)
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Translator.Core;
using Translator.Desktop.Models;
using Translator.Desktop.Services;

namespace Translator.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SettingsStore _store = new();
    private readonly AppSettings _settings;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private string _output = "";
    [ObservableProperty] private string _direction = "";
    [ObservableProperty] private string _error = "";
    [ObservableProperty] private bool _isBusy;
    public bool HasError => !string.IsNullOrEmpty(Error);
    public ObservableCollection<HistoryItem> Recents { get; } = new();

    public MainWindowViewModel()
    {
        _settings = _store.Load();
        foreach (var h in _settings.History) Recents.Add(h);   // ← mistake: History may be null after deserialize
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        IsBusy = true; Error = "";
        try
        {
            var translator = new Translator.Core.Translator(_settings.ApiKey, _settings.Model);
            var r = await translator.TranslateAsync(InputText, _settings.Style);
            Direction = $"{Tag(r.Source)} → {Tag(r.Target)}";
            Output = r.Translation;
        }
        catch (Exception ex) { Error = $"Couldn't translate: {ex.Message}"; }
        finally { IsBusy = false; OnPropertyChanged(nameof(HasError)); }
    }

    private static string Tag(string lang) => lang == "English" ? "en" : lang == "Swedish" ? "sv" : "??";
}
```

**Verify:** `dotnet build` — fails if `History` is null on a fresh install. Guard the load (`_settings.History ?? new()`, or default the property — you already defaulted it in Step 10, so the real fix is keeping `Load()` from returning a settings object with a null list). Run the app with a real key in settings.json → a real translation appears with the direction tag.

**Commit:** `feat(desktop): implement translate view-model with busy/error state`

---

### Step 14: Add recents, copy, and Enter-to-translate

Wire the remaining UX from the original app: push each translation onto the last-8 history (persisted), implement copy-to-clipboard, and make `Enter` translate while `Shift+Enter` inserts a newline.

```csharp
// src/Translator.Desktop/ViewModels/MainWindowViewModel.cs  (add inside TranslateAsync, after Output = r.Translation)
            var item = new HistoryItem(InputText, r.Source, r.Target, r.Translation);
            Recents.Insert(0, item);
            while (Recents.Count > 8) Recents.RemoveAt(Recents.Count - 1);
            _settings.History = Recents.ToList();
            _store.Save(_settings);
            InputText = "";
```

```csharp
// src/Translator.Desktop/ViewModels/MainWindowViewModel.cs  (add a new command)
    [RelayCommand]
    private async Task CopyAsync()
    {
        var top = Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var clip = top?.MainWindow?.Clipboard;
        if (clip is not null) await clip.SetTextAsync(Output);
    }
```

```csharp
// src/Translator.Desktop/Views/MainWindow.axaml.cs  (add handler; Enter = translate, Shift+Enter = newline)
    private void OnInputKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter && e.KeyModifiers != Avalonia.Input.KeyModifiers.Shift)
        {
            e.Handled = true;
            (DataContext as ViewModels.MainWindowViewModel)?.TranslateCommand.Execute(null);
        }
    }
```

**Verify:** `dotnet run --project src/Translator.Desktop` — translate, see it appear in recents and persist across restarts; Copy puts the result on the clipboard; Enter translates, Shift+Enter adds a line.

**Commit:** `feat(desktop): add recents, copy, and enter-to-translate`

---

### Step 15: Add the settings panel

Add a collapsible settings area to edit the writing style and API key, saving on change — mirroring the original's Settings drawer.

```xml
<!-- src/Translator.Desktop/Views/MainWindow.axaml  (add at top of the StackPanel) -->
  <ToggleButton Content="Settings" IsChecked="{Binding ShowSettings}" />
  <StackPanel IsVisible="{Binding ShowSettings}" Spacing="8">
    <TextBlock Text="Writing style" />
    <TextBox Text="{Binding StyleText}" AcceptsReturn="True" Height="80" />
    <TextBlock Text="Anthropic API key" />
    <TextBox Text="{Binding ApiKeyText}" PasswordChar="•" />
  </StackPanel>
```

```csharp
// src/Translator.Desktop/ViewModels/MainWindowViewModel.cs  (add properties + persistence)
    [ObservableProperty] private bool _showSettings;

    public string StyleText
    {
        get => _settings.Style;
        set { _settings.Style = value; _store.Save(_settings); OnPropertyChanged(); }
    }
    public string ApiKeyText
    {
        get => _settings.ApiKey;
        set { _settings.ApiKey = value; _store.Save(_settings); OnPropertyChanged(); }
    }
```

**Verify:** run the app, open Settings, paste a key and edit the style; restart → both persist and the next translation reflects the new style.

**Commit:** `feat(desktop): add settings panel for style and key`

---

### Step 16: Strip the web page and point it at the API

Make a web-only copy of the page: remove the API-key field and the direct `api.anthropic.com` fetch, and point the request at `POST /api/translate`. This is the only front-end change for the web slice.

```fish
# index.html already copied in Step 5; edit it in place
```

In `src/Translator.Web/wwwroot/index.html`, replace the `translate(text)` network section so the body posts to your endpoint instead of Anthropic, and delete the `apiKey` input + its `Store` wiring:

```javascript
// src/Translator.Web/wwwroot/index.html  (inside translate(), replace the fetch block)
  const res = await fetch("/api/translate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ text, style: els.style.value }),
  });
  if (!res.ok) throw new Error("API " + res.status);
  const p = await res.json();   // server returns {source,target,translation} already
  return { source: p.source, target: p.target, translation: p.translation };
```

Delete the `<input id="apiKey">` block and the `tr:key` lines in the page's script (the server holds the key now).

**Verify:** with the Web project running (still the Core stub or real key via env), open `http://localhost:5000`, type a phrase → it round-trips through `/api/translate`.

**Commit:** `feat(web): point stripped page at /api/translate`

---

### Step 17: Implement /api/translate with the server-held key

> **New concept: configuration + DI in ASP.NET.** ASP.NET reads settings from `appsettings.json`, environment variables, and user-secrets via `builder.Configuration`. Registering `ITranslator` in the DI container (`AddSingleton`) lets the endpoint receive it as a parameter. Keep the key out of source — read it from config/env.

Register a real `Translator` built from the configured key + model, replacing the Step 5 stub registration.

```csharp
// src/Translator.Web/Program.cs  (replace the AddSingleton line)
var apiKey = builder.Configuration["Anthropic:ApiKey"]
    ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    ?? throw new InvalidOperationException("No Anthropic API key configured.");
var model = builder.Configuration["Anthropic:Model"] ?? "claude-sonnet-4-6";
builder.Services.AddSingleton<ITranslator>(_ => new Translator.Core.Translator(apiKey, model));
```

**Verify:** `set -x ANTHROPIC_API_KEY sk-ant-…` then `dotnet run --project src/Translator.Web`; the page returns real translations. Without the key set, startup throws a clear error.

**Commit:** `feat(web): wire endpoint to core with configured key`

---

### Step 18: Add simple auth to the web endpoint

The deploy is private (one user), so a single shared token is enough. Gate the endpoint on a header compared against a configured secret.

```csharp
// src/Translator.Web/Program.cs  (read the secret near the top, after `model`)
var authToken = builder.Configuration["Auth:Token"]
    ?? Environment.GetEnvironmentVariable("AUTH_TOKEN");
```

```csharp
// src/Translator.Web/Program.cs  (replace the MapPost body's first lines)
app.MapPost("/api/translate", async (TranslateRequest req, HttpRequest http, ITranslator translator) =>
{
    if (!string.IsNullOrEmpty(authToken) &&
        !string.Equals(http.Headers["X-Auth-Token"], authToken, StringComparison.Ordinal))
        return Results.Unauthorized();

    var result = await translator.TranslateAsync(req.Text, req.Style ?? "");
    return Results.Ok(result);
});
```

Have the page send the token (inject it server-side, or for personal use store it in the page). Simplest: a meta tag the script reads and sends as `X-Auth-Token`.

**Verify:** with `AUTH_TOKEN` set, a request without the header returns 401; with the correct header it translates.

**Commit:** `feat(web): gate endpoint behind shared-token auth`

> ✅ **MVP complete.** Desktop and web both translate end-to-end through one shared Core, with persistence, settings, recents, and private auth.

---

## Phase 2 — Stretch Goals

> Goal: polish and packaging beyond the requirements. Each step references the
> Phase 1 step it builds on.

### Step 19: Publish a self-contained macOS app bundle

> **New concept: `dotnet publish` self-contained + RID.** A *Runtime Identifier* (RID) like `osx-arm64` targets a specific OS/CPU. `--self-contained` bundles the .NET runtime so users don't install anything; `PublishSingleFile=true` collapses it to one executable. macOS apps are `.app` folders with a specific layout + `Info.plist`.

Builds on Step 3 (the desktop app). Publish, then wrap the binary in a minimal `.app` and ad-hoc sign it.

```fish
dotnet publish src/Translator.Desktop -c Release -r osx-arm64 --self-contained \
  -p:PublishSingleFile=true -o publish/osx-arm64

# Minimal .app bundle
set app "publish/Translate.app/Contents/MacOS"
mkdir -p $app
cp publish/osx-arm64/Translator.Desktop $app/Translate
# Add publish/Translate.app/Contents/Info.plist with CFBundleExecutable=Translate

codesign --force --deep -s - publish/Translate.app   # ad-hoc (unsigned) signature
```

**Verify:** double-click `Translate.app` → Gatekeeper blocks it; right-click → Open → Open once, and it launches. (No Apple Developer cert needed.)

**Commit:** `chore(desktop): document self-contained macOS bundle build`

---

### Step 20: GitHub Actions release matrix

> **New concept: CI matrix.** A `strategy.matrix` runs the same job once per entry — here, once per RID — producing one artifact per OS from a single workflow.

Builds on Step 19. On every GitHub Release, publish unsigned binaries for all four targets and attach them.

```yaml
# .github/workflows/release.yml  (new file)
name: release
on:
  release: { types: [published] }
jobs:
  build:
    strategy:
      matrix:
        rid: [osx-arm64, osx-x64, win-x64, linux-x64]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet publish src/Translator.Desktop -c Release -r ${{ matrix.rid }}
             --self-contained -p:PublishSingleFile=true -o out/${{ matrix.rid }}
      - uses: actions/upload-artifact@v4
        with: { name: translate-${{ matrix.rid }}, path: out/${{ matrix.rid }} }
```

**Verify:** push the workflow, cut a test release on GitHub → four artifacts build and appear on the run. (macOS cross-build from Linux works for the binary; the `.app` wrapping/signing still happens on a Mac as in Step 19.)

**Commit:** `chore(ci): build per-OS desktop binaries on release`

---

### Step 21: Containerize the web app for Coolify

> **New concept: multi-stage Dockerfile.** A build stage compiles with the SDK image; a smaller runtime stage carries only the published output. Coolify on Hetzner builds this image and serves it under your `dentaku.se` subdomain.

Builds on Steps 17–18 (the web app + auth). Add a Dockerfile and set the key/token as Coolify secrets.

```dockerfile
# src/Translator.Web/Dockerfile  (new file)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Translator.Web -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Translator.Web.dll"]
```

**Verify:** `docker build -f src/Translator.Web/Dockerfile -t translate-web .` then `docker run -p 8080:8080 -e ANTHROPIC_API_KEY=… -e AUTH_TOKEN=… translate-web` → page works at `localhost:8080`. In Coolify: point at the repo, set the two secrets, deploy under the subdomain.

**Commit:** `chore(web): add Dockerfile for Coolify deploy`

---

### Step 22: Polish — OS dark mode and an optional direction picker

Builds on Steps 12–15. Avalonia follows the OS theme when `RequestedThemeVariant="Default"` is set on `App` — confirm dark mode tracks the system. Optionally add a third language or an explicit EN/SV/HU direction picker (per the original roadmap) by extending the schema `enum` in Step 6 and the `Tag()` map in Step 13.

```xml
<!-- src/Translator.Desktop/App.axaml  (on the Application root) -->
  RequestedThemeVariant="Default"
```

**Verify:** toggle macOS appearance (System Settings → Appearance) → the app switches light/dark with it.

**Commit:** `feat(desktop): follow OS theme and prep for third language`

---

## Where to go from here

- **Run the whole suite anytime:** `dotnet build && dotnet test`.
- **Daily desktop dev loop:** `dotnet watch --project src/Translator.Desktop`.
- **Daily web dev loop:** `dotnet watch --project src/Translator.Web`.
- The original single-file app stays in `materials/` as the reference for visual direction and the smoke tests in `CLAUDE.md` (`Hej, hur mår du?` → English, tag `sv → en`).
