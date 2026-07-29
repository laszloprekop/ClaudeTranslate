using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Translator.Desktop.ViewModels;

namespace Translator.Desktop.Views;

public partial class MainWindow : Window
{
    // The app column keeps its 680; the Panel takes 400 on the right.
    private const double AppWidth = 680;
    private const double PanelWidth = 400;

    public MainWindow()
    {
        InitializeComponent();

        // Tunnelling: the handler has to see Enter before the TextBox does, or the TextBox
        // inserts the line break first and there is nothing left to cancel.
        InputBox.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm) vm.PropertyChanged += OnViewModelChanged;
        };
    }

    // Opening the Panel widens the window rather than covering the app.
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsPanelOpen)) return;
        var open = ((MainWindowViewModel)sender!).IsPanelOpen;
        MinWidth = open ? AppWidth * 0.76 + PanelWidth : 520;
        Width = open ? AppWidth + PanelWidth : AppWidth;
    }

    // Enter submits, Shift+Enter breaks the line. Any other modifier is left to the TextBox, so
    // Alt+Enter or Cmd+Enter cannot submit by accident.
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.ImeAccept)) return;
        if (e.KeyModifiers != KeyModifiers.None) return;

        e.Handled = true;
        (DataContext as MainWindowViewModel)?.TranslateCommand.Execute(null);
    }
}
