using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace Temp33;
public class Settings(int updateFrequencySeconds = 1) {
    public string Version => Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "Unknown";
    public int UpdateFrequencySeconds { get; set; } = updateFrequencySeconds;
    public string? SensorIdentifier { get; set; }
    public string? HardwareIdentifier { get; set; }

    private static readonly string  settingsFilePath = Path.Combine(
       AppConstants.SettingsDirectory,
       "settings.json"
    );

    public void Save() {
        var directoryPath = Path.GetDirectoryName(settingsFilePath);
        if (!string.IsNullOrEmpty(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        } else {
            Logger.WriteLine("Failed to create settings directory.", null, null);
            return;
        }
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(settingsFilePath, json);
    }
    public static void Delete() {
        if (File.Exists(settingsFilePath)) {
            File.Delete(settingsFilePath);
        }
    }

    public static Settings Load() {
        if (File.Exists(settingsFilePath)) {
            string json = File.ReadAllText(settingsFilePath);
            try {
                Settings? settings = JsonConvert.DeserializeObject<Settings>(json);
                if (settings != null) {
                    return settings;
                } else {
                    Logger.WriteLine("Failed to deserialize the settings. Default settings will be used.", null, null);
                }
            } catch (JsonException ex) {
                Logger.WriteLine($"Error deserializing settings: {ex.Message}", null, null);
            }
        }
        return new Settings(1);
    }
}
