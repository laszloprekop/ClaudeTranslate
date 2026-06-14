using Avalonia.Controls;
using Avalonia.Interactivity;
using Translator.Core;

namespace Translator.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private readonly ITranslator _translator = new Translator.Core.Translator();

    private async void OnTranslateClick(object? sender, RoutedEventArgs e)
    {
        var result = await _translator.TranslateAsync(Input.Text ?? "", "");
        Output.Text = $"{result.Source} -> {result.Target}: {result.Translation}";
    }
}
