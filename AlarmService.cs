using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace AlarmClock;

public class AlarmService
{
    private readonly DispatcherTimer _timer;
    private readonly List<AlarmItem> _alarms = new();
    private AlarmItem? _currentRingingAlarm;
    private AlarmConfig _config;

    public event EventHandler<AlarmEventArgs>? AlarmTriggered;
    public event EventHandler<AlarmEventArgs>? AlarmStopped;
    public event EventHandler? AlarmsChanged;

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
                DaysOfWeek = configItem.DaysOfWeek ?? new List<int>()
            });
        }
    }

    public void AddAlarm(int hour, int minute, string name = "", int customRingingSeconds = 5, int maxRingingMinutes = 10, string musicFilePath = "", List<int>? daysOfWeek = null)
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
            DaysOfWeek = daysOfWeek ?? new List<int>()
        };

        _alarms.Add(alarm);
        SaveToConfig();
        AlarmsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateAlarm(string id, int hour, int minute, string name, bool isEnabled, int customRingingSeconds, int maxRingingMinutes, string musicFilePath, List<int>? daysOfWeek = null)
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

            SaveToConfig();
            AlarmsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

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
                DaysOfWeek = alarm.DaysOfWeek
            });
        }
        _config.Save();
    }

    public List<AlarmItem> GetAlarms() => _alarms.ToList();

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

    private void TriggerAlarm(AlarmItem alarm)
    {
        alarm.IsRinging = true;
        alarm.RingingStartTime = DateTime.Now;
        _currentRingingAlarm = alarm;

        AlarmTriggered?.Invoke(this, new AlarmEventArgs(alarm));
    }

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

    public AlarmConfig GetConfig() => _config;

    public void UpdateConfig(int defaultRingingSeconds, int maxRingingMinutes, int idleThresholdSeconds)
    {
        _config.DefaultRingingDurationSeconds = defaultRingingSeconds;
        _config.MaxRingingDurationMinutes = maxRingingMinutes;
        _config.IdleThresholdSeconds = idleThresholdSeconds;
        _config.Save();
    }
}

public class AlarmItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public TimeSpan Time { get; set; }
    public string Name { get; set; } = "";
    public bool IsEnabled { get; set; }
    public bool IsRinging { get; set; }
    public DateTime? RingingStartTime { get; set; }
    public int CustomRingingDurationSeconds { get; set; } = 5;
    public int MaxRingingDurationMinutes { get; set; } = 10;
    public string MusicFilePath { get; set; } = "";
    public List<int> DaysOfWeek { get; set; } = new();

    /// <summary>
    /// 檢查今天是否應該響鈴
    /// </summary>
    public bool ShouldRingToday()
    {
        // 如果沒有設定星期幾，代表每天都響
        if (DaysOfWeek == null || DaysOfWeek.Count == 0)
            return true;

        int today = (int)DateTime.Now.DayOfWeek;
        return DaysOfWeek.Contains(today);
    }
}

public class AlarmEventArgs : EventArgs
{
    public AlarmItem Alarm { get; }

    public AlarmEventArgs(AlarmItem alarm)
    {
        Alarm = alarm;
    }
}

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
