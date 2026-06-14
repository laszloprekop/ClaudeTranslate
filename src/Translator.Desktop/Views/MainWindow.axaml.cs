using Avalonia.Controls;
using Avalonia.Input;

namespace Translator.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Wired up in Step 14 (Enter = translate, Shift+Enter = newline).
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter && e.KeyModifiers != Avalonia.Input.KeyModifiers.Shift)
        {
            e.Handled = true;
            (DataContext as ViewModels.MainWindowViewModel)?.TranslateCommand.Execute(null);
        }
    }
}
