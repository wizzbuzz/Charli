using System.Text.Json;

namespace OverlayApp;

public class UserSettings
{
    public Keys MainKey { get; set; } = Keys.A;
    public bool UseCtrl { get; set; } = true;
    public bool UseAlt { get; set; } = true;
    public bool UseShift { get; set; } = true;

    private static readonly string _filePath = "settings.json";

    public static UserSettings Load()
    {
        if (File.Exists(_filePath))
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        return new UserSettings();
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}