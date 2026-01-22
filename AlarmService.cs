using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace AlarmClock;

/// <summary>
/// 鬧鐘服務類別，負責管理所有鬧鐘的運作、觸發及停止邏輯
/// </summary>
public class AlarmService
{
    private readonly DispatcherTimer _timer;
    private readonly List<AlarmItem> _alarms = new();
    private AlarmItem? _currentRingingAlarm;
    private AlarmConfig _config;

    /// <summary>
    /// 鬧鐘觸發事件，當鬧鐘開始響鈴時觸發
    /// </summary>
    public event EventHandler<AlarmEventArgs>? AlarmTriggered;

    /// <summary>
    /// 鬧鐘停止事件，當鬧鐘停止響鈴時觸發
    /// </summary>
    public event EventHandler<AlarmEventArgs>? AlarmStopped;

    /// <summary>
    /// 鬧鐘清單變更事件，當新增、刪除或更新鬧鐘時觸發
    /// </summary>
    public event EventHandler? AlarmsChanged;

    /// <summary>
    /// 建立鬧鐘服務並啟動計時器
    /// </summary>
    /// <param name="config">鬧鐘設定物件</param>
    public AlarmService(AlarmConfig config)
    {
        _config = config;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += CheckAlarms;
        _timer.Start();

        LoadAlarmsFromConfig();
    }

    /// <summary>
    /// 從設定檔載入所有鬧鐘資料到記憶體中
    /// </summary>
    private void LoadAlarmsFromConfig()
    {
        _alarms.Clear();
        foreach (var configItem in _config.Alarms)
        {
            _alarms.Add(new AlarmItem
            {
                Id = configItem.Id,
                Time = configItem.GetTime(),
                Name = configItem.Name,
                IsEnabled = configItem.IsEnabled,
                CustomRingingDurationSeconds = configItem.CustomRingingDurationSeconds,
                MaxRingingDurationMinutes = configItem.MaxRingingDurationMinutes,
                MusicFilePath = configItem.MusicFilePath,
                DaysOfWeek = configItem.DaysOfWeek ?? new List<int>(),
                ExcludeHolidays = configItem.ExcludeHolidays
            });
        }
    }

    /// <summary>
    /// 新增一個鬧鐘到系統中
    /// </summary>
    /// <param name="hour">小時（0-23）</param>
    /// <param name="minute">分鐘（0-59）</param>
    /// <param name="name">鬧鐘名稱</param>
    /// <param name="customRingingSeconds">自訂響鈴秒數</param>
    /// <param name="maxRingingMinutes">最大響鈴分鐘數</param>
    /// <param name="musicFilePath">音樂檔案路徑</param>
    /// <param name="daysOfWeek">響鈴的星期幾（0=日, 1=一, ..., 6=六），空值或空清單表示每天</param>
    /// <param name="excludeHolidays">是否排除台灣國定假日</param>
    public void AddAlarm(int hour, int minute, string name = "", int customRingingSeconds = 5, int maxRingingMinutes = 10, string musicFilePath = "", List<int>? daysOfWeek = null, bool excludeHolidays = false)
    {
        var id = Guid.NewGuid().ToString();
        var alarm = new AlarmItem
        {
            Id = id,
            Time = new TimeSpan(hour, minute, 0),
            Name = string.IsNullOrEmpty(name) ? $"鬧鐘 {hour:00}:{minute:00}" : name,
            IsEnabled = true,
            CustomRingingDurationSeconds = customRingingSeconds,
            MaxRingingDurationMinutes = maxRingingMinutes,
            MusicFilePath = musicFilePath,
            DaysOfWeek = daysOfWeek ?? new List<int>(),
            ExcludeHolidays = excludeHolidays
        };

        _alarms.Add(alarm);
        SaveToConfig();
        AlarmsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 更新指定的鬧鐘設定
    /// </summary>
    /// <param name="id">鬧鐘的唯一識別碼</param>
    /// <param name="hour">小時（0-23）</param>
    /// <param name="minute">分鐘（0-59）</param>
    /// <param name="name">鬧鐘名稱</param>
    /// <param name="isEnabled">是否啟用</param>
    /// <param name="customRingingSeconds">自訂響鈴秒數</param>
    /// <param name="maxRingingMinutes">最大響鈴分鐘數</param>
    /// <param name="musicFilePath">音樂檔案路徑</param>
    /// <param name="daysOfWeek">響鈴的星期幾（0=日, 1=一, ..., 6=六），空值或空清單表示每天</param>
    /// <param name="excludeHolidays">是否排除台灣國定假日</param>
    public void UpdateAlarm(string id, int hour, int minute, string name, bool isEnabled, int customRingingSeconds, int maxRingingMinutes, string musicFilePath, List<int>? daysOfWeek = null, bool excludeHolidays = false)
    {
        var alarm = _alarms.FirstOrDefault(a => a.Id == id);
        if (alarm != null)
        {
            alarm.Time = new TimeSpan(hour, minute, 0);
            alarm.Name = name;
            alarm.IsEnabled = isEnabled;
            alarm.CustomRingingDurationSeconds = customRingingSeconds;
            alarm.MaxRingingDurationMinutes = maxRingingMinutes;
            alarm.MusicFilePath = musicFilePath;
            alarm.DaysOfWeek = daysOfWeek ?? new List<int>();
            alarm.ExcludeHolidays = excludeHolidays;

            SaveToConfig();
            AlarmsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 刪除指定的鬧鐘
    /// </summary>
    /// <param name="id">鬧鐘的唯一識別碼</param>
    public void DeleteAlarm(string id)
    {
        var alarm = _alarms.FirstOrDefault(a => a.Id == id);
        if (alarm != null)
        {
            _alarms.Remove(alarm);
            SaveToConfig();
            AlarmsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 切換指定鬧鐘的啟用狀態（啟用變停用，停用變啟用）
    /// </summary>
    /// <param name="id">鬧鐘的唯一識別碼</param>
    public void ToggleAlarm(string id)
    {
        var alarm = _alarms.FirstOrDefault(a => a.Id == id);
        if (alarm != null)
        {
            alarm.IsEnabled = !alarm.IsEnabled;
            SaveToConfig();
            AlarmsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 將記憶體中的鬧鐘清單儲存到設定檔
    /// </summary>
    private void SaveToConfig()
    {
        _config.Alarms.Clear();
        foreach (var alarm in _alarms)
        {
            _config.Alarms.Add(new AlarmConfigItem
            {
                Id = alarm.Id,
                Hour = alarm.Time.Hours,
                Minute = alarm.Time.Minutes,
                Name = alarm.Name,
                IsEnabled = alarm.IsEnabled,
                CustomRingingDurationSeconds = alarm.CustomRingingDurationSeconds,
                MaxRingingDurationMinutes = alarm.MaxRingingDurationMinutes,
                MusicFilePath = alarm.MusicFilePath,
                DaysOfWeek = alarm.DaysOfWeek,
                ExcludeHolidays = alarm.ExcludeHolidays
            });
        }
        _config.Save();
    }

    /// <summary>
    /// 取得所有鬧鐘的複本清單
    /// </summary>
    /// <returns>鬧鐘清單</returns>
    public List<AlarmItem> GetAlarms() => _alarms.ToList();

    /// <summary>
    /// 定期檢查是否有鬧鐘需要觸發，以及正在響鈴的鬧鐘是否應該停止
    /// </summary>
    private void CheckAlarms(object? sender, EventArgs e)
    {
        var now = DateTime.Now.TimeOfDay;

        // 檢查是否有鬧鐘需要觸發
        foreach (var alarm in _alarms.Where(a => a.IsEnabled && !a.IsRinging))
        {
            if (Math.Abs((now - alarm.Time).TotalSeconds) < 1)
            {
                // 檢查今天是否應該響鈴
                if (alarm.ShouldRingToday())
                {
                    TriggerAlarm(alarm);
                }
            }
        }

        // 檢查正在響鈴的鬧鐘
        if (_currentRingingAlarm != null)
        {
            CheckRingingAlarm();
        }
    }

    /// <summary>
    /// 觸發指定的鬧鐘，設定為響鈴狀態並觸發事件
    /// </summary>
    /// <param name="alarm">要觸發的鬧鐘</param>
    private void TriggerAlarm(AlarmItem alarm)
    {
        alarm.IsRinging = true;
        alarm.RingingStartTime = DateTime.Now;
        _currentRingingAlarm = alarm;

        AlarmTriggered?.Invoke(this, new AlarmEventArgs(alarm));
    }

    /// <summary>
    /// 檢查正在響鈴的鬧鐘是否應該自動停止
    /// 根據使用者活動狀態和設定的時間來決定是否停止
    /// </summary>
    private void CheckRingingAlarm()
    {
        if (_currentRingingAlarm == null) return;

        bool isUserActive = UserActivityDetector.IsUserActive(_config.IdleThresholdSeconds);
        var ringingDuration = DateTime.Now - _currentRingingAlarm.RingingStartTime!.Value;

        // 如果有人在使用電腦，響自訂秒數後自動關閉
        if (isUserActive && ringingDuration.TotalSeconds >= _currentRingingAlarm.CustomRingingDurationSeconds)
        {
            StopAlarm();
        }
        // 如果沒人使用，且設定了時限（不為0），則檢查是否超時
        else if (!isUserActive && _currentRingingAlarm.MaxRingingDurationMinutes > 0
                && ringingDuration.TotalMinutes >= _currentRingingAlarm.MaxRingingDurationMinutes)
        {
            StopAlarm();
        }
        // 如果 MaxRingingDurationMinutes = 0，則永不自動停止，只能手動關閉
    }

    /// <summary>
    /// 停止目前正在響鈴的鬧鐘
    /// </summary>
    public void StopAlarm()
    {
        if (_currentRingingAlarm != null)
        {
            _currentRingingAlarm.IsRinging = false;
            _currentRingingAlarm.RingingStartTime = null;

            AlarmStopped?.Invoke(this, new AlarmEventArgs(_currentRingingAlarm));
            _currentRingingAlarm = null;
        }
    }

    /// <summary>
    /// 取得目前的設定物件
    /// </summary>
    /// <returns>設定物件</returns>
    public AlarmConfig GetConfig() => _config;

    /// <summary>
    /// 更新全域設定並儲存到設定檔
    /// </summary>
    /// <param name="defaultRingingSeconds">預設響鈴秒數</param>
    /// <param name="maxRingingMinutes">最大響鈴分鐘數</param>
    /// <param name="idleThresholdSeconds">閒置閾值秒數</param>
    public void UpdateConfig(int defaultRingingSeconds, int maxRingingMinutes, int idleThresholdSeconds)
    {
        _config.DefaultRingingDurationSeconds = defaultRingingSeconds;
        _config.MaxRingingDurationMinutes = maxRingingMinutes;
        _config.IdleThresholdSeconds = idleThresholdSeconds;
        _config.Save();
    }
}

/// <summary>
/// 鬧鐘項目，代表一個執行中的鬧鐘物件（包含狀態資訊）
/// </summary>
public class AlarmItem
{
    /// <summary>
    /// 鬧鐘的唯一識別碼
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 鬧鐘的時間
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// 鬧鐘的名稱描述
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 鬧鐘是否啟用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 鬧鐘是否正在響鈴
    /// </summary>
    public bool IsRinging { get; set; }

    /// <summary>
    /// 鬧鐘開始響鈴的時間，未響鈴時為 null
    /// </summary>
    public DateTime? RingingStartTime { get; set; }

    /// <summary>
    /// 自訂的響鈴持續時間（秒數），當使用者活躍時使用
    /// </summary>
    public int CustomRingingDurationSeconds { get; set; } = 5;

    /// <summary>
    /// 最大響鈴持續時間（分鐘），當使用者閒置時的響鈴時間上限，0 表示永不自動停止
    /// </summary>
    public int MaxRingingDurationMinutes { get; set; } = 10;

    /// <summary>
    /// 自訂音樂檔案的完整路徑，若為空則使用系統預設音效
    /// </summary>
    public string MusicFilePath { get; set; } = "";

    /// <summary>
    /// 響鈴的星期幾（0=星期日, 1=星期一, ..., 6=星期六），空清單表示每天都響
    /// </summary>
    public List<int> DaysOfWeek { get; set; } = new();

    /// <summary>
    /// 是否排除台灣國定假日
    /// </summary>
    public bool ExcludeHolidays { get; set; } = false;

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

/// <summary>
/// 鬧鐘事件參數，包含觸發的鬧鐘資訊
/// </summary>
public class AlarmEventArgs : EventArgs
{
    /// <summary>
    /// 觸發事件的鬧鐘物件
    /// </summary>
    public AlarmItem Alarm { get; }

    /// <summary>
    /// 建立鬧鐘事件參數
    /// </summary>
    /// <param name="alarm">鬧鐘物件</param>
    public AlarmEventArgs(AlarmItem alarm)
    {
        Alarm = alarm;
    }
}

/// <summary>
/// 使用者活動偵測器，用於判斷使用者是否正在使用電腦
/// </summary>
public static class UserActivityDetector
{
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    /// <summary>
    /// 判斷使用者是否在指定的閾值時間內有活動
    /// </summary>
    /// <param name="thresholdSeconds">閾值秒數，超過此時間沒有活動視為閒置</param>
    /// <returns>true 表示使用者活躍，false 表示閒置</returns>
    public static bool IsUserActive(int thresholdSeconds = 30)
    {
        var lastInput = new LASTINPUTINFO();
        lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);

        if (!GetLastInputInfo(ref lastInput))
            return false;

        var idleTime = Environment.TickCount - lastInput.dwTime;

        // 如果在閾值時間內有活動，視為有人在使用
        return idleTime < (thresholdSeconds * 1000);
    }
}
