using System;
using System.IO;
using System.Text.Json;
using Translator.Desktop.Models;

namespace Translator.Desktop.Services;

public class SettingsStore(string? path = null)
{
    private static readonly string DefaultPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Translator", "settings.json");

    // Overridable so a test can point at its own file rather than the real one.
    private readonly string _path = path ?? DefaultPath;

    public AppSettings Load()
    {
        if (!File.Exists(_path)) return new AppSettings();
        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
