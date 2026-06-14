using System;
using System.IO;
using System.Text.Json;
using Translator.Desktop.Models;

namespace Translator.Desktop.Services;

public class SettingsStore
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Translator", "settings.json");

    public AppSettings Load()
    {
        if (!File.Exists(Path)) return new AppSettings();
        var json = File.ReadAllText(Path);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public static AppSettings Settings { get; set; } = new(); // stub

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    } // stub
}
