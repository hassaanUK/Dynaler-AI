using System.Windows;
using DynalerAI.Core;

namespace DynalerAI;

public partial class SettingsWindow : Window
{
    private readonly ConfigManager _config;

    public SettingsWindow(ConfigManager config)
    {
        InitializeComponent();
        _config = config;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var cfg = _config.Load();
        BuiltinKeyBox.Password = cfg.BuiltinKey;
        MaxRetriesBox.Text = cfg.MaxRetries.ToString();
        ActionDelayBox.Text = cfg.ActionDelayMs.ToString();
        MinimizeToTrayCheck.IsChecked = cfg.MinimizeToTray;
        EnableLoggingCheck.IsChecked = cfg.EnableLogging;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var cfg = _config.Load();
        cfg.BuiltinKey = BuiltinKeyBox.Password;
        cfg.MaxRetries = int.TryParse(MaxRetriesBox.Text, out var r) ? r : 2;
        cfg.ActionDelayMs = int.TryParse(ActionDelayBox.Text, out var d) ? d : 150;
        cfg.MinimizeToTray = MinimizeToTrayCheck.IsChecked == true;
        cfg.EnableLogging = EnableLoggingCheck.IsChecked == true;
        _config.Save(cfg);
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}