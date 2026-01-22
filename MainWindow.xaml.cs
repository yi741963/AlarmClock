using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace AlarmClock;

public partial class MainWindow : Window
{
    private readonly AlarmService _alarmService;
    private readonly DispatcherTimer _uiTimer;
    private readonly SoundPlayer _alarmSound;
    private readonly AlarmConfig _config;
    private readonly MusicManager _musicManager;
    private WinForms.NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();

        // 載入設定
        _config = AlarmConfig.Load();

        _musicManager = new MusicManager(_config.MusicFolderPath);

        _alarmService = new AlarmService(_config);
        _alarmService.AlarmTriggered += OnAlarmTriggered;
        _alarmService.AlarmStopped += OnAlarmStopped;
        _alarmService.AlarmsChanged += OnAlarmsChanged;

        // UI 更新計時器
        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _uiTimer.Tick += UpdateUI;
        _uiTimer.Start();

        // 初始化音效（使用系統預設音效）
        _alarmSound = new SoundPlayer();
        SetSystemBeep();

        UpdateAlarmsDisplay();

        // 在標題欄顯示設定檔路徑
        Title = $"智慧鬧鐘 - {AlarmConfig.GetConfigPath()}";

        // 初始化系統匣圖示
        InitializeSystemTray();

        // 處理視窗狀態變更
        StateChanged += MainWindow_StateChanged;
    }

    private void SetSystemBeep()
    {
        // 使用系統預設的警告音效
        try
        {
            _alarmSound.Stream = System.Reflection.Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("SystemSounds.Beep");
        }
        catch
        {
            // 如果無法載入，將使用系統嗶聲
        }
    }

    private void UpdateUI(object? sender, EventArgs e)
    {
        // 更新當前時間
        CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");

        // 更新使用者活動狀態
        bool isActive = UserActivityDetector.IsUserActive(_config.IdleThresholdSeconds);
        UserActivityText.Text = $"使用者狀態: {(isActive ? "✅ 活躍中" : "💤 閒置中")}";
        UserActivityText.Foreground = isActive
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136));
    }

    private void OnAlarmsChanged(object? sender, EventArgs e)
    {
        UpdateAlarmsDisplay();
    }

    private void UpdateAlarmsDisplay()
    {
        AlarmsPanel.Children.Clear();

        var alarms = _alarmService.GetAlarms();
        if (alarms.Count == 0)
        {
            var emptyText = new TextBlock
            {
                Text = "尚無鬧鐘，請點擊上方按鈕新增",
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            AlarmsPanel.Children.Add(emptyText);
            return;
        }

        foreach (var alarm in alarms)
        {
            var alarmCard = CreateAlarmCard(alarm);
            AlarmsPanel.Children.Add(alarmCard);
        }
    }

    private Border CreateAlarmCard(AlarmItem alarm)
    {
        var border = new Border
        {
            Background = alarm.IsRinging
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 69, 0))
                : alarm.IsEnabled
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 0, 0, 10),
            Opacity = alarm.IsEnabled ? 1.0 : 0.5
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var stackPanel = new StackPanel();

        var nameText = new TextBlock
        {
            Text = alarm.Name,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };

        var timeText = new TextBlock
        {
            Text = alarm.Time.ToString(@"hh\:mm"),
            FontSize = 32,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 217, 255))
        };

        // 顯示週期資訊
        string daysInfo = "";
        if (alarm.DaysOfWeek != null && alarm.DaysOfWeek.Count > 0)
        {
            var dayNames = new[] { "日", "一", "二", "三", "四", "五", "六" };
            var selectedDays = alarm.DaysOfWeek.OrderBy(d => d).Select(d => dayNames[d]);
            daysInfo = $" | 週期: {string.Join(", ", selectedDays)}";
        }
        else
        {
            daysInfo = " | 每天";
        }

        var infoText = new TextBlock
        {
            Text = $"響鈴 {alarm.CustomRingingDurationSeconds} 秒 | {(alarm.IsEnabled ? "✅ 啟用" : "❌ 停用")}{daysInfo}",
            FontSize = 12,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)),
            Margin = new Thickness(0, 5, 0, 0)
        };

        var statusText = new TextBlock
        {
            Text = alarm.IsRinging ? "🔊 響鈴中..." : "⏰ 已設定",
            FontSize = 14,
            Foreground = alarm.IsRinging
                ? System.Windows.Media.Brushes.Yellow
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)),
            Margin = new Thickness(0, 5, 0, 0)
        };

        stackPanel.Children.Add(nameText);
        stackPanel.Children.Add(timeText);
        stackPanel.Children.Add(infoText);
        stackPanel.Children.Add(statusText);

        Grid.SetColumn(stackPanel, 0);
        grid.Children.Add(stackPanel);

        // 按鈕面板
        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Vertical,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        if (alarm.IsRinging)
        {
            var stopButton = new System.Windows.Controls.Button
            {
                Content = "關閉",
                FontSize = 14,
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stopButton.Click += (s, e) => _alarmService.StopAlarm();
            buttonPanel.Children.Add(stopButton);
        }
        else
        {
            var editButton = new System.Windows.Controls.Button
            {
                Content = "✏️",
                FontSize = 14,
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 123, 255)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 5)
            };
            editButton.Click += (s, e) => EditAlarm(alarm);
            buttonPanel.Children.Add(editButton);

            var toggleButton = new System.Windows.Controls.Button
            {
                Content = alarm.IsEnabled ? "🔕" : "🔔",
                FontSize = 14,
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 5)
            };
            toggleButton.Click += (s, e) => _alarmService.ToggleAlarm(alarm.Id);
            buttonPanel.Children.Add(toggleButton);

            var deleteButton = new System.Windows.Controls.Button
            {
                Content = "🗑️",
                FontSize = 14,
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            deleteButton.Click += (s, e) => DeleteAlarm(alarm);
            buttonPanel.Children.Add(deleteButton);
        }

        Grid.SetColumn(buttonPanel, 1);
        grid.Children.Add(buttonPanel);

        border.Child = grid;
        return border;
    }

    private void AddAlarmButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AlarmEditDialog(null, _musicManager);
        if (dialog.ShowDialog() == true && dialog.IsSaved)
        {
            _alarmService.AddAlarm(
                dialog.Hour,
                dialog.Minute,
                dialog.AlarmName,
                dialog.RingingDuration,
                dialog.MaxRingingDuration,
                dialog.MusicFilePath,
                dialog.DaysOfWeek
            );
        }
    }

    private void EditAlarm(AlarmItem alarm)
    {
        var dialog = new AlarmEditDialog(alarm, _musicManager);
        if (dialog.ShowDialog() == true && dialog.IsSaved)
        {
            _alarmService.UpdateAlarm(
                alarm.Id,
                dialog.Hour,
                dialog.Minute,
                dialog.AlarmName,
                dialog.IsAlarmEnabled,
                dialog.RingingDuration,
                dialog.MaxRingingDuration,
                dialog.MusicFilePath,
                dialog.DaysOfWeek
            );
        }
    }

    private void DeleteAlarm(AlarmItem alarm)
    {
        var result = System.Windows.MessageBox.Show(
            $"確定要刪除鬧鐘「{alarm.Name}」嗎？",
            "確認刪除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            _alarmService.DeleteAlarm(alarm.Id);
        }
    }

    private void OnAlarmTriggered(object? sender, AlarmEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"🔔 {e.Alarm.Name} 觸發！";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            UpdateAlarmsDisplay();

            // 播放警報音效（使用自訂音樂或預設音效）
            PlayAlarmSound(e.Alarm);
        });
    }

    private void OnAlarmStopped(object? sender, AlarmEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = "系統就緒";
            StatusText.Foreground = System.Windows.Media.Brushes.Green;
            UpdateAlarmsDisplay();

            // 停止音效
            StopAlarmSound();
        });
    }

    private void PlayAlarmSound(AlarmItem alarm)
    {
        try
        {
            // 如果鬧鐘有自訂音樂檔案，使用自訂音樂
            if (!string.IsNullOrEmpty(alarm.MusicFilePath) && File.Exists(alarm.MusicFilePath))
            {
                _alarmSound.SoundLocation = alarm.MusicFilePath;
                _alarmSound.PlayLooping();
            }
            else
            {
                // 使用系統預設音效
                SetSystemBeep();
                _alarmSound.PlayLooping();
            }
        }
        catch
        {
            // 如果音效播放失敗，使用系統嗶聲
            try
            {
                System.Console.Beep(800, 500);
            }
            catch
            {
                // 忽略錯誤
            }
        }
    }

    private void StopAlarmSound()
    {
        _alarmSound.Stop();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsDialog(_config);
        if (dialog.ShowDialog() == true && dialog.IsSaved)
        {
            // 更新設定
            _config.IdleThresholdSeconds = dialog.IdleThresholdSeconds;
            _config.MusicFolderPath = dialog.MusicFolderPath;
            _config.Save();

            // 更新 MusicManager 的資料夾路徑
            if (!string.IsNullOrEmpty(dialog.MusicFolderPath))
            {
                var newMusicManager = new MusicManager(dialog.MusicFolderPath);
            }

            System.Windows.MessageBox.Show(
                "設定已儲存！\n\n新的設定將立即生效。",
                "成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }

    private void InitializeSystemTray()
    {
        // 創建系統匣圖示
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application, // 使用預設應用程式圖示
            Visible = true,
            Text = "智慧鬧鐘"
        };

        // 雙擊圖示顯示視窗
        _notifyIcon.DoubleClick += (s, e) => ShowWindow();

        // 創建右鍵選單
        var contextMenu = new WinForms.ContextMenuStrip();

        var showMenuItem = new WinForms.ToolStripMenuItem("顯示主視窗");
        showMenuItem.Click += (s, e) => ShowWindow();
        contextMenu.Items.Add(showMenuItem);

        contextMenu.Items.Add(new WinForms.ToolStripSeparator());

        var exitMenuItem = new WinForms.ToolStripMenuItem("結束程式");
        exitMenuItem.Click += (s, e) =>
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        };
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        // 當視窗最小化時，隱藏到系統匣
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(
                    1000,
                    "智慧鬧鐘",
                    "程式已最小化到系統匣，雙擊圖示可重新顯示視窗",
                    WinForms.ToolTipIcon.Info
                );
            }
        }
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 關閉視窗時最小化到系統匣，而不是結束程式
        e.Cancel = true;
        WindowState = WindowState.Minimized;
        base.OnClosing(e);
    }
}