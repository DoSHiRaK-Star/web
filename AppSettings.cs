using System.Text.Json;

namespace SimpleBrowser;

public class AppSettings
{
    public string HomePage { get; set; } = "https://www.google.com";
    public string SearchEngine { get; set; } = "https://www.google.com/search?q={0}";
    public bool IsStatusBarEnabled { get; set; } = true;
    public bool AreDevToolsEnabled { get; set; } = true;

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SimpleBrowser");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
            }
        }
        catch
        {
            // повреждённый файл — вернём дефолт
        }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOpts));
    }
}