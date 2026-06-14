using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        foreach (var h in _settings.History) Recents.Add(h);
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        IsBusy = true;
        Error = "";
        try
        {
            var translator = new Translator.Core.Translator(_settings.ApiKey, _settings.Model);
            var r = await translator.TranslateAsync(InputText, _settings.Style);
            Direction = $"{Tag(r.Source)} -> {Tag(r.Target)}";
            Output = r.Translation;
            var item = new HistoryItem(InputText, r.Source, r.Target, r.Translation);
            Recents.Insert(0, item);
            while (Recents.Count > 25) Recents.RemoveAt(Recents.Count - 1);
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
            OnPropertyChanged(nameof(HasError));
        }
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        var top =
            Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var clip = top?.MainWindow?.Clipboard;
        if (clip is not null) await clip.SetTextAsync(Output);
    }

    private string Tag(string lang) => lang == "English" ? "en" : lang == "Swedish" ? "sv" : "??";
}
