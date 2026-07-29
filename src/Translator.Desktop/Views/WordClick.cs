using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Translator.Desktop.ViewModels;

namespace Translator.Desktop.Views;

// Makes every word in a run of prose a Lookup target without turning the prose into a row of
// buttons: the text stays one TextBlock, and the word under the pointer is found by hit-testing
// the laid-out text. The word carries the language of the text it was clicked in.
//
// It fires on release, and only when the pointer barely moved and nothing is selected — the same
// text has to stay selectable, and a drag across it means "select", not "look up".
public static class WordClick
{
    private const double DragThreshold = 4;

    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, ICommand?>("Command", typeof(WordClick));

    public static readonly AttachedProperty<string?> LanguageProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("Language", typeof(WordClick));

    // A chip holds one equivalent, which may be several words ("hatóanyag nélküli") — look the
    // whole thing up rather than the word under the pointer.
    public static readonly AttachedProperty<bool> WholeProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("Whole", typeof(WordClick));

    private static readonly AttachedProperty<Point?> PressedAtProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, Point?>("PressedAt", typeof(WordClick));

    static WordClick() => CommandProperty.Changed.AddClassHandler<TextBlock>(OnCommandChanged);

    public static void SetCommand(TextBlock block, ICommand? value) => block.SetValue(CommandProperty, value);
    public static ICommand? GetCommand(TextBlock block) => block.GetValue(CommandProperty);
    public static void SetLanguage(TextBlock block, string? value) => block.SetValue(LanguageProperty, value);
    public static string? GetLanguage(TextBlock block) => block.GetValue(LanguageProperty);
    public static void SetWhole(TextBlock block, bool value) => block.SetValue(WholeProperty, value);
    public static bool GetWhole(TextBlock block) => block.GetValue(WholeProperty);

    private static void OnCommandChanged(TextBlock block, AvaloniaPropertyChangedEventArgs e)
    {
        block.PointerPressed -= OnPointerPressed;
        block.PointerReleased -= OnPointerReleased;
        if (e.NewValue is null) return;
        block.PointerPressed += OnPointerPressed;
        block.PointerReleased += OnPointerReleased;
        block.Cursor = new Cursor(StandardCursorType.Hand);
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock block && e.GetCurrentPoint(block).Properties.IsLeftButtonPressed)
            block.SetValue(PressedAtProperty, e.GetPosition(block));
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not TextBlock block) return;
        var pressedAt = block.GetValue(PressedAtProperty);
        block.SetValue(PressedAtProperty, null);

        if (e.InitialPressMouseButton != MouseButton.Left || pressedAt is not { } start) return;
        var end = e.GetPosition(block);
        if (Math.Abs(end.X - start.X) > DragThreshold || Math.Abs(end.Y - start.Y) > DragThreshold) return;
        if (block is SelectableTextBlock { SelectionStart: var from, SelectionEnd: var to } && from != to) return;

        var command = GetCommand(block);
        var word = GetWhole(block) ? block.Text?.Trim() : WordAt(block, end);
        if (command is null || word is null) return;

        e.Handled = true;
        var chip = new WordChip(word, GetLanguage(block));
        if (command.CanExecute(chip)) command.Execute(chip);
    }

    private static string? WordAt(TextBlock block, Point point)
    {
        var text = block.Text ?? Flatten(block.Inlines);
        if (string.IsNullOrEmpty(text)) return null;

        var hit = block.TextLayout.HitTestPoint(
            new Point(point.X - block.Padding.Left, point.Y - block.Padding.Top));
        if (!hit.IsInside) return null;

        var index = Math.Clamp(hit.TextPosition, 0, text.Length - 1);
        if (!IsWordChar(text[index])) return null;

        var start = index;
        while (start > 0 && IsWordChar(text[start - 1])) start--;
        var end = index;
        while (end + 1 < text.Length && IsWordChar(text[end + 1])) end++;
        return text[start..(end + 1)];
    }

    // A block built from Runs — an example with its headword marked — has no Text of its own.
    private static string Flatten(InlineCollection? inlines) =>
        inlines is null ? "" : string.Concat(inlines.OfType<Run>().Select(r => r.Text));

    // Apostrophes and hyphens sit inside words; everything else ends one.
    private static bool IsWordChar(char c) => char.IsLetter(c) || c is '\'' or '’' or '-';
}
