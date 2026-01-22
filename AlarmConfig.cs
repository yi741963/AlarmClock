using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlarmClock;

/// <summary>
/// 鬧鐘設定檔類別，負責管理所有鬧鐘的設定資料及應用程式全域設定
/// </summary>
public class AlarmConfig
{
    private const string ConfigFileName = "alarm_config.json";
    private static string ConfigFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartAlarmClock",
        ConfigFileName
    );

    /// <summary>
    /// 所有鬧鐘的設定清單
    /// </summary>
    public List<AlarmConfigItem> Alarms { get; set; } = new();

    /// <summary>
    /// 預設的響鈴持續時間（秒數），當使用者活躍時使用
    /// </summary>
    [JsonPropertyName("defaultRingingDurationSeconds")]
    public int DefaultRingingDurationSeconds { get; set; } = 5;

    /// <summary>
    /// 最大響鈴持續時間（分鐘），當使用者閒置時的響鈴時間上限，0 表示永不自動停止
    /// </summary>
    [JsonPropertyName("maxRingingDurationMinutes")]
    public int MaxRingingDurationMinutes { get; set; } = 10;

    /// <summary>
    /// 判定使用者是否閒置的閾值（秒數），超過此時間沒有輸入視為閒置
    /// </summary>
    [JsonPropertyName("idleThresholdSeconds")]
    public int IdleThresholdSeconds { get; set; } = 30;

    /// <summary>
    /// 自訂音樂資料夾路徑，若為空則使用預設路徑
    /// </summary>
    [JsonPropertyName("musicFolderPath")]
    public string MusicFolderPath { get; set; } = "";

    /// <summary>
    /// 取得預設的音樂資料夾路徑
    /// </summary>
    /// <returns>預設音樂資料夾的完整路徑</returns>
    public static string GetDefaultMusicFolderPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartAlarmClock",
        "Music"
    );

    /// <summary>
    /// 從設定檔載入鬧鐘設定，若檔案不存在或載入失敗則建立預設設定
    /// </summary>
    /// <returns>載入的設定物件或預設設定物件</returns>
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

    /// <summary>
    /// 將目前的設定儲存到設定檔，如果目錄不存在會自動建立
    /// </summary>
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

    /// <summary>
    /// 建立預設的設定物件，包含兩個範例鬧鐘
    /// </summary>
    /// <returns>預設設定物件</returns>
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

    /// <summary>
    /// 取得設定檔的完整路徑
    /// </summary>
    /// <returns>設定檔的完整路徑</returns>
    public static string GetConfigPath() => ConfigFilePath;
}

/// <summary>
/// 單一鬧鐘設定項目，包含時間、名稱及響鈴行為等設定
/// </summary>
public class AlarmConfigItem
{
    /// <summary>
    /// 鬧鐘的唯一識別碼
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 鬧鐘的小時（0-23）
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// 鬧鐘的分鐘（0-59）
    /// </summary>
    public int Minute { get; set; }

    /// <summary>
    /// 鬧鐘的名稱描述
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 鬧鐘是否啟用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 自訂的響鈴持續時間（秒數），當使用者活躍時使用
    /// </summary>
    [JsonPropertyName("customRingingDurationSeconds")]
    public int CustomRingingDurationSeconds { get; set; } = 5;

    /// <summary>
    /// 最大響鈴持續時間（分鐘），當使用者閒置時的響鈴時間上限，0 表示永不自動停止
    /// </summary>
    [JsonPropertyName("maxRingingDurationMinutes")]
    public int MaxRingingDurationMinutes { get; set; } = 10;

    /// <summary>
    /// 自訂音樂檔案的完整路徑，若為空則使用系統預設音效
    /// </summary>
    [JsonPropertyName("musicFilePath")]
    public string MusicFilePath { get; set; } = "";

    /// <summary>
    /// 鬧鐘響鈴的星期幾 (0=星期日, 1=星期一, ..., 6=星期六)
    /// 空陣列或 null 代表每天都響
    /// </summary>
    [JsonPropertyName("daysOfWeek")]
    public List<int> DaysOfWeek { get; set; } = new();

    /// <summary>
    /// 是否排除台灣國定假日
    /// </summary>
    [JsonPropertyName("excludeHolidays")]
    public bool ExcludeHolidays { get; set; } = false;

    /// <summary>
    /// 音量大小 (0-100)，預設為 50
    /// </summary>
    [JsonPropertyName("volume")]
    public int Volume { get; set; } = 50;

    /// <summary>
    /// 將小時和分鐘轉換為 TimeSpan 物件
    /// </summary>
    /// <returns>代表鬧鐘時間的 TimeSpan 物件</returns>
    public TimeSpan GetTime() => new TimeSpan(Hour, Minute, 0);

    /// <summary>
    /// 檢查今天是否應該響鈴（包含星期幾和假日檢查）
    /// </summary>
    public bool ShouldRingToday()
    {
        var today = DateTime.Now;

        // 檢查是否排除假日
        if (ExcludeHolidays && TaiwanHolidays.IsHoliday(today))
        {
            return false;
        }

        // 如果沒有設定星期幾，代表每天都響
        if (DaysOfWeek == null || DaysOfWeek.Count == 0)
            return true;

        int dayOfWeek = (int)today.DayOfWeek;
        return DaysOfWeek.Contains(dayOfWeek);
    }
}
