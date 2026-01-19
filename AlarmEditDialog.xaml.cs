using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace AlarmClock;

public partial class AlarmEditDialog : Window
{
    private readonly MusicManager _musicManager;
    private string _selectedMusicFilePath = "";

    public string AlarmName { get; private set; } = "";
    public int Hour { get; private set; }
    public int Minute { get; private set; }
    public bool IsAlarmEnabled { get; private set; }
    public int RingingDuration { get; private set; }
    public int MaxRingingDuration { get; private set; }
    public string MusicFilePath { get; private set; } = "";
    public bool IsSaved { get; private set; }

    public AlarmEditDialog(AlarmItem? alarm = null, MusicManager? musicManager = null)
    {
        InitializeComponent();

        _musicManager = musicManager ?? new MusicManager();

        // 初始化小時選項 (0-23)
        for (int i = 0; i < 24; i++)
        {
            HourComboBox.Items.Add(i.ToString("00"));
        }

        // 初始化分鐘選項 (0-59)
        for (int i = 0; i < 60; i++)
        {
            MinuteComboBox.Items.Add(i.ToString("00"));
        }

        // 如果是編輯現有鬧鐘，載入資料
        if (alarm != null)
        {
            Title = "編輯鬧鐘";
            NameTextBox.Text = alarm.Name;
            HourComboBox.SelectedIndex = alarm.Time.Hours;
            MinuteComboBox.SelectedIndex = alarm.Time.Minutes;
            IsEnabledCheckBox.IsChecked = alarm.IsEnabled;
            RingingDurationSlider.Value = alarm.CustomRingingDurationSeconds;
            MaxRingingDurationSlider.Value = alarm.MaxRingingDurationMinutes;

            _selectedMusicFilePath = alarm.MusicFilePath;
            UpdateMusicFileDisplay();
        }
        else
        {
            Title = "新增鬧鐘";
            var now = DateTime.Now;
            HourComboBox.SelectedIndex = now.Hour;
            MinuteComboBox.SelectedIndex = now.Minute;
            IsEnabledCheckBox.IsChecked = true;
            RingingDurationSlider.Value = 5;
            MaxRingingDurationSlider.Value = 10;
        }
    }

    private void BrowseMusicButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "選擇鬧鐘音樂檔案",
            Filter = "音樂檔案 (*.mp3;*.wav;*.wma;*.m4a)|*.mp3;*.wav;*.wma;*.m4a|所有檔案 (*.*)|*.*",
            InitialDirectory = _musicManager.GetMusicFolderPath()
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var selectedFile = openFileDialog.FileName;

            // 驗證檔案
            if (!_musicManager.IsValidMusicFile(selectedFile))
            {
                MessageBox.Show(
                    "選擇的檔案無效！\n\n限制：\n- 僅支援 MP3, WAV, WMA, M4A 格式\n- 檔案大小不得超過 5 MB",
                    "檔案驗證失敗",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // 詢問是否複製到音樂資料夾
            var result = MessageBox.Show(
                "是否將音樂檔案複製到應用程式的音樂資料夾？\n\n" +
                "點擊「是」：複製檔案到程式資料夾（建議）\n" +
                "點擊「否」：使用原始檔案路徑",
                "複製檔案",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
            {
                var copiedPath = _musicManager.CopyMusicFileToFolder(selectedFile);
                if (copiedPath != null)
                {
                    _selectedMusicFilePath = copiedPath;
                    MessageBox.Show(
                        "檔案已成功複製到音樂資料夾！",
                        "成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "複製檔案失敗，請檢查檔案權限。",
                        "錯誤",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }
            }
            else
            {
                _selectedMusicFilePath = selectedFile;
            }

            UpdateMusicFileDisplay();
        }
    }

    private void UpdateMusicFileDisplay()
    {
        if (string.IsNullOrEmpty(_selectedMusicFilePath))
        {
            MusicFileTextBox.Text = "系統預設音效";
        }
        else
        {
            MusicFileTextBox.Text = Path.GetFileName(_selectedMusicFilePath);
        }
    }

    private void MaxRingingDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxRingingDurationText != null)
        {
            int value = (int)e.NewValue;
            if (value == 0)
            {
                MaxRingingDurationText.Text = "♾️ 永不停止";
                MaxRingingDurationText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 215, 0)); // 金色
            }
            else
            {
                MaxRingingDurationText.Text = $"{value} 分鐘";
                MaxRingingDurationText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 99, 71)); // 紅色
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("請輸入鬧鐘名稱", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (HourComboBox.SelectedIndex < 0 || MinuteComboBox.SelectedIndex < 0)
        {
            MessageBox.Show("請選擇時間", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AlarmName = NameTextBox.Text;
        Hour = HourComboBox.SelectedIndex;
        Minute = MinuteComboBox.SelectedIndex;
        IsAlarmEnabled = IsEnabledCheckBox.IsChecked ?? true;
        RingingDuration = (int)RingingDurationSlider.Value;
        MaxRingingDuration = (int)MaxRingingDurationSlider.Value;
        MusicFilePath = _selectedMusicFilePath;
        IsSaved = true;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = false;
        DialogResult = false;
        Close();
    }
}
