using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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

    private string Tag(string lang) => lang == "English" ? "en" : lang == "Swedish" ? "sv" : "??";
}
