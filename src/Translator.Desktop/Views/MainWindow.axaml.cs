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
    }
}
