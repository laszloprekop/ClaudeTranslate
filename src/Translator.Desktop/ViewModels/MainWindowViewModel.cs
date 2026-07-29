using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Translator.Core;
using Translator.Desktop.Models;
using Translator.Desktop.Services;

namespace Translator.Desktop.ViewModels;

public partial class LanguageOption(Language language, bool isChecked, Action onToggled) : ObservableObject
{
    [ObservableProperty] private bool _isChecked = isChecked;

    public string Name => language.Name;
    public string Label => $"{language.Flag}  {language.NativeName}";

    partial void OnIsCheckedChanged(bool value) => onToggled();
}

public partial class MainWindowViewModel : ObservableObject
{
    // Every example is a phrase. A single word is a Lookup and produces no card, so a one-word
    // example would show the user something they cannot reproduce by typing it.
    private static readonly HistoryItem[] ExampleHistory =
    [
        new("Where is the nearest train station?", "English", "Swedish", "Var ligger närmaste tågstation?"),
        new("Tack för hjälpen!", "Swedish", "English", "Thanks for the help!"),
        new("Szeretlek, de most mennem kell.", "Hungarian", "English", "I love you, but I have to go now."),
        new("See you tomorrow!", "English", "German", "Bis morgen!"),
        new("On prend le petit déjeuner ensemble ?", "French", "English", "Shall we have breakfast together?"),
        new("La mariposa se posó en mi mano.", "Spanish", "Swedish", "Fjärilen landade i min hand."),
    ];

    private readonly SettingsStore _store;
    private readonly AppSettings _settings;
    private LookupResult? _shown;
    private string? _shownLanguage;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private string _error = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showSettings;
    [ObservableProperty] private bool _showingExamples;
    [ObservableProperty] private bool _isPanelOpen;
    [ObservableProperty] private bool _isLookingUp;
    [ObservableProperty] private string _panelWord = "";
    [ObservableProperty] private string _panelNote = "";
    [ObservableProperty] private string _panelSuggestion = "";
    [ObservableProperty] private bool _trailExpanded;
    public bool HasError => !string.IsNullOrEmpty(Error);
    public bool HasHistory => Recents.Count > 0;
    public string HistoryHeading => ShowingExamples ? "EXAMPLES" : "TRANSLATIONS";
    public bool HasPanelNote => !string.IsNullOrEmpty(PanelNote);
    public bool HasPanelSuggestion => !string.IsNullOrEmpty(PanelSuggestion);
    public bool HasTrail => Trail.Count > 0;
    public string TrailHeading => $"LOOKED UP · {Trail.Count}";
    public bool IsWorking => IsBusy || IsLookingUp;
    public ObservableCollection<HistoryItem> Recents { get; } = new();
    public ObservableCollection<LanguageOption> Languages { get; } = new();

    // The Prefetch Set: the languages every Lookup asks for, whatever is currently enabled.
    public ObservableCollection<LanguageOption> PrefetchLanguages { get; } = new();
    public IReadOnlyList<string> Models { get; }
    public ObservableCollection<PanelEntry> PanelEntries { get; } = new();
    // A trail word carries no language: it is filed under its lemma, and which language that
    // lemma belongs to is exactly what its stored entries say.
    public ObservableCollection<WordChip> Trail { get; } = new();

    public MainWindowViewModel() : this(new SettingsStore()) { }

    public MainWindowViewModel(SettingsStore store)
    {
        _store = store;
        _settings = _store.Load();
        Models = ModelCatalog.Known.Contains(_settings.Model)
            ? ModelCatalog.Known
            : [_settings.Model, .. ModelCatalog.Known];
        if (_settings.History.Count == 0) ShowExamples();
        else foreach (var h in _settings.History) Recents.Add(h);
        foreach (var l in LanguageCatalog.All)
        {
            Languages.Add(new LanguageOption(l, _settings.Targets.Contains(l.Name), OnTargetsChanged));
            PrefetchLanguages.Add(new LanguageOption(l, _settings.Prefetch.Contains(l.Name), OnPrefetchChanged));
        }
        Recents.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasHistory));
        RefreshTrail();
    }

    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsWorking));
    partial void OnIsLookingUpChanged(bool value) => OnPropertyChanged(nameof(IsWorking));
    partial void OnShowingExamplesChanged(bool value) => OnPropertyChanged(nameof(HistoryHeading));
    partial void OnPanelNoteChanged(string value) => OnPropertyChanged(nameof(HasPanelNote));
    partial void OnPanelSuggestionChanged(string value) => OnPropertyChanged(nameof(HasPanelSuggestion));

    private void ShowExamples()
    {
        Recents.Clear();
        foreach (var item in ExampleHistory) Recents.Add(item);
        ShowingExamples = true;
    }

    // Toggling a language while the Panel is open is instant: the hidden languages were always
    // there, because every Lookup fetches the whole Prefetch Set (ADR-0004).
    private void OnTargetsChanged()
    {
        _settings.Targets = Languages.Where(l => l.IsChecked).Select(l => l.Name).ToList();
        _store.Save(_settings);
        if (IsPanelOpen) ReopenPanel();
    }

    // A Lookup with an empty Prefetch Set would ask for entries in no language at all, so the
    // last language cannot be switched off.
    private void OnPrefetchChanged()
    {
        var chosen = PrefetchLanguages.Where(l => l.IsChecked).Select(l => l.Name).ToList();
        if (chosen.Count == 0)
        {
            Error = "A lookup has to ask for at least one language.";
            var only = PrefetchLanguages.First(l => l.Name == _settings.Prefetch[0]);
            only.IsChecked = true;
            return;
        }
        _settings.Prefetch = chosen;
        _store.Save(_settings);
    }

    private List<string> EnabledLanguages() =>
        Languages.Where(l => l.IsChecked).Select(l => l.Name).ToList();

    private static bool IsOneWord(string text) =>
        text.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length == 1;

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsBusy) return;

        // Exactly one word is a Lookup and produces no Translation Card: the Panel's
        // equivalents already give the translation into every enabled language, split by sense.
        if (IsOneWord(InputText))
        {
            var word = InputText.Trim();
            await LookUpAsync(word, null);
            if (!HasError) InputText = "";
            return;
        }

        var targets = Languages.Where(l => l.IsChecked).Select(l => l.Name).ToList();
        if (targets.Count == 0)
        {
            Error = "Pick at least one target language.";
            return;
        }
        IsBusy = true;
        Error = "";
        try
        {
            var translator = new Translator.Core.Translator(_settings.ApiKey, _settings.Model);
            var results = await Task.WhenAll(
                targets.Select(t => translator.TranslateAsync(InputText, _settings.Style, t)));
            var fresh = results.Where(r => r.Source != r.Target).ToList();
            if (fresh.Count == 0)
            {
                Error = "The input is already in every selected language — nothing to translate.";
                return;
            }
            if (ShowingExamples)
            {
                Recents.Clear();
                ShowingExamples = false;
            }
            for (var i = fresh.Count - 1; i >= 0; i--)
                Recents.Insert(0, new HistoryItem(InputText, fresh[i].Source, fresh[i].Target, fresh[i].Translation));
            while (Recents.Count > 30) Recents.RemoveAt(Recents.Count - 1);
            _settings.History = Recents.ToList();
            _store.Save(_settings);
            InputText = "";
        }
        catch (Exception ex)
        {
            Error = $"Could not translate: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // A word carries its own language: clicking a Swedish equivalent opens the Swedish entry
    // first, which is where the round trip pays off — looking up trög from an English entry
    // shows which English senses it does and doesn't reach.
    [RelayCommand]
    private Task LookUpChipAsync(WordChip? chip) =>
        chip is null || string.IsNullOrWhiteSpace(chip.Text)
            ? Task.CompletedTask
            : LookUpAsync(Clean(chip.Text), chip.Language);

    [RelayCommand]
    private Task LookUpWordAsync(string? word) =>
        string.IsNullOrWhiteSpace(word) ? Task.CompletedTask : LookUpAsync(Clean(word), null);

    private async Task LookUpAsync(string word, string? language)
    {
        if (IsLookingUp) return;
        Error = "";

        var stored = WordTrail.Find(_settings.Trail, word);
        if (stored is not null && !WordTrail.IsStale(stored, EnabledLanguages()))
        {
            Remember(word, stored.Result, stored.Coverage);
            ShowPanel(stored.Word, stored.Result, language);
            return;
        }

        // Clear first: leaving the previous word's entries under the progress line reads as if
        // the panel had already answered.
        IsLookingUp = true;
        IsPanelOpen = true;
        PanelWord = word;
        PanelNote = "";
        PanelSuggestion = "";
        _shown = null;
        PanelEntries.Clear();
        try
        {
            var translator = new Translator.Core.Translator(_settings.ApiKey, _settings.Model);
            var result = await translator.LookUpAsync(word, _settings.Prefetch);
            Remember(word, result, _settings.Prefetch);
            ShowPanel(WordTrail.Find(_settings.Trail, word)?.Word ?? word, result, language);
        }
        catch (Exception ex)
        {
            Error = $"Could not look up “{word}”: {ex.Message}";
            IsPanelOpen = false;
        }
        finally
        {
            IsLookingUp = false;
        }
    }

    private void Remember(string word, LookupResult result, IReadOnlyList<string> coverage)
    {
        WordTrail.Remember(_settings.Trail, word, result, coverage);
        _store.Save(_settings);
        RefreshTrail();
    }

    private void ShowPanel(string word, LookupResult result, string? language)
    {
        PanelWord = word;
        PanelNote = result.Note ?? "";
        PanelSuggestion = result.Suggestion ?? "";
        _shown = result;
        _shownLanguage = language;
        FillPanel();
        IsPanelOpen = true;
    }

    private void ReopenPanel()
    {
        if (_shown is not null) FillPanel();
    }

    private void FillPanel()
    {
        var enabled = EnabledLanguages();
        PanelEntries.Clear();
        if (_shown is null) return;

        // Filter to the Enabled Languages, but never leave the Panel blank: an entry that only
        // exists in a language the user has switched off is still an answer about their word.
        var entries = _shown.Entries.Where(e => enabled.Contains(e.Language)).ToList();
        if (entries.Count == 0) entries = _shown.Entries.ToList();
        if (_shownLanguage is not null)
            entries = entries.OrderByDescending(e => e.Language == _shownLanguage).ToList();

        foreach (var entry in entries) PanelEntries.Add(new PanelEntry(entry, enabled));
    }

    private void RefreshTrail()
    {
        Trail.Clear();
        foreach (var word in _settings.Trail) Trail.Add(new WordChip(word.Word, null));
        OnPropertyChanged(nameof(HasTrail));
        OnPropertyChanged(nameof(TrailHeading));
    }

    private static string Clean(string word) => word.Trim().Trim('.', ',', '!', '?', ';', ':', '"', '(', ')', '«', '»');

    [RelayCommand]
    private void ClosePanel()
    {
        IsPanelOpen = false;
        _shown = null;
        PanelEntries.Clear();
    }

    [RelayCommand]
    private void ToggleTrail() => TrailExpanded = !TrailExpanded;

    [RelayCommand]
    private void ClearTrail()
    {
        _settings.Trail = [];
        _store.Save(_settings);
        RefreshTrail();
        ClosePanel();
    }

    [RelayCommand]
    private async Task OpenLinkAsync(string? url)
    {
        var top = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (top?.MainWindow is { } window && !string.IsNullOrEmpty(url))
            await window.Launcher.LaunchUriAsync(new Uri(url));
    }

    [RelayCommand]
    private void ClearInput() => InputText = "";

    [RelayCommand]
    private void ClearHistory()
    {
        _settings.History = [];
        _store.Save(_settings);
        ShowExamples();
    }

    [RelayCommand]
    private async Task CopyAsync(string? text)
    {
        var top =
            Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var clip = top?.MainWindow?.Clipboard;
        if (clip is not null && !string.IsNullOrEmpty(text)) await clip.SetTextAsync(text);
    }

    public string StyleText
    {
        get => _settings.Style;
        set
        {
            _settings.Style = value;
            _store.Save(_settings);
            OnPropertyChanged();
        }
    }

    public string ApiKeyText
    {
        get => _settings.ApiKey;
        set
        {
            _settings.ApiKey = value;
            _store.Save(_settings);
            OnPropertyChanged();
        }
    }

    // A model that is in the settings file but not on the shortlist still shows and still works.
    public string SelectedModel
    {
        get => _settings.Model;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value == _settings.Model) return;
            _settings.Model = value;
            _store.Save(_settings);
            OnPropertyChanged();
        }
    }
}
