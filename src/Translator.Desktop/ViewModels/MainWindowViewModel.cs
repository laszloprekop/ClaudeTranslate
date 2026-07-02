using System;
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
    private readonly SettingsStore _store = new();
    private readonly AppSettings _settings;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private string _error = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showSettings;
    public bool HasError => !string.IsNullOrEmpty(Error);
    public bool HasHistory => Recents.Count > 0;
    public ObservableCollection<HistoryItem> Recents { get; } = new();
    public ObservableCollection<LanguageOption> Languages { get; } = new();

    public MainWindowViewModel()
    {
        _settings = _store.Load();
        foreach (var h in _settings.History) Recents.Add(h);
        foreach (var l in LanguageCatalog.All)
            Languages.Add(new LanguageOption(l, _settings.Targets.Contains(l.Name), SaveTargets));
        Recents.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasHistory));
    }

    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));

    private void SaveTargets()
    {
        _settings.Targets = Languages.Where(l => l.IsChecked).Select(l => l.Name).ToList();
        _store.Save(_settings);
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsBusy) return;
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

    [RelayCommand]
    private void ClearInput() => InputText = "";

    [RelayCommand]
    private void ClearHistory()
    {
        Recents.Clear();
        _settings.History = [];
        _store.Save(_settings);
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
}
