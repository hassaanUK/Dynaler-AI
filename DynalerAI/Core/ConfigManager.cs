using System.IO;
using System.Text.Json;

namespace DynalerAI.Core;

public class AppConfig
{
    public string BuiltinKey { get; set; } = "";
    public bool SafeMode { get; set; } = false;
    public bool ScreenVision { get; set; } = true;
    public bool AutoRetry { get; set; } = true;
    public int MaxRetries { get; set; } = 2;
    public int ActionDelayMs { get; set; } = 150;
    public bool MinimizeToTray { get; set; } = true;
    public string StopHotkey { get; set; } = "Ctrl+Shift+S";
    public bool EnableLogging { get; set; } = true;
}

public class ConfigManager
{
    private static readonly string BaseDir = AppContext.BaseDirectory;
    private static string ConfigFile => Path.Combine(BaseDir, "config.json");
    public  static string LogFile    => Path.Combine(BaseDir, "history.log");
    public  static string PresetsFile => Path.Combine(BaseDir, "presets.json");

    public AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigFile))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFile)) ?? new AppConfig();
        }
        catch { }
        return new AppConfig();
    }

    public void Save(AppConfig config)
    {
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
}