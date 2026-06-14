using Translator.Desktop.Models;

namespace Translator.Desktop.Services;

public class SettingsStore
{
    public static AppSettings Settings { get; set; } = new(); // stub

    public void Save(AppSettings settings)
    {
    } // stub
}
