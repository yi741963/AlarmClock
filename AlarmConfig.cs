using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlarmClock;

public class AlarmConfig
{
    private const string ConfigFileName = "alarm_config.json";
    private static string ConfigFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartAlarmClock",
        ConfigFileName
    );

    public List<AlarmConfigItem> Alarms { get; set; } = new();

    [JsonPropertyName("defaultRingingDurationSeconds")]
    public int DefaultRingingDurationSeconds { get; set; } = 5;

    [JsonPropertyName("maxRingingDurationMinutes")]
    public int MaxRingingDurationMinutes { get; set; } = 10;

    [JsonPropertyName("idleThresholdSeconds")]
    public int IdleThresholdSeconds { get; set; } = 30;

    [JsonPropertyName("musicFolderPath")]
    public string MusicFolderPath { get; set; } = "";

    public static string GetDefaultMusicFolderPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartAlarmClock",
        "Music"
    );

    public static AlarmConfig Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AlarmConfig>(json);
                return config ?? CreateDefault();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"載入設定檔失敗: {ex.Message}");
        }

        return CreateDefault();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"儲存設定檔失敗: {ex.Message}");
        }
    }

    private static AlarmConfig CreateDefault()
    {
        return new AlarmConfig
        {
            Alarms = new List<AlarmConfigItem>
            {
                new AlarmConfigItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Hour = 23,
                    Minute = 0,
                    Name = "晚上 11 點提醒",
                    IsEnabled = true,
                    CustomRingingDurationSeconds = 5
                },
                new AlarmConfigItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Hour = 0,
                    Minute = 0,
                    Name = "午夜 12 點提醒",
                    IsEnabled = true,
                    CustomRingingDurationSeconds = 5
                }
            },
            DefaultRingingDurationSeconds = 5,
            MaxRingingDurationMinutes = 10,
            IdleThresholdSeconds = 30
        };
    }

    public static string GetConfigPath() => ConfigFilePath;
}

public class AlarmConfigItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Hour { get; set; }
    public int Minute { get; set; }
    public string Name { get; set; } = "";
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("customRingingDurationSeconds")]
    public int CustomRingingDurationSeconds { get; set; } = 5;

    [JsonPropertyName("maxRingingDurationMinutes")]
    public int MaxRingingDurationMinutes { get; set; } = 10;

    [JsonPropertyName("musicFilePath")]
    public string MusicFilePath { get; set; } = "";

    public TimeSpan GetTime() => new TimeSpan(Hour, Minute, 0);
}
