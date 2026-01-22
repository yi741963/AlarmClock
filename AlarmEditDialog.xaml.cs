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
    public List<int> DaysOfWeek { get; private set; } = new();
    public bool ExcludeHolidays { get; private set; }
    public bool IsSaved { get; private set; }

    /// <summary>
    /// 建立鬧鐘編輯對話框
    /// </summary>
    /// <param name="alarm">要編輯的鬧鐘，若為 null 則為新增模式</param>
    /// <param name="musicManager">音樂管理器，用於處理音樂檔案選擇</param>
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

            // 載入星期幾設定
            LoadDaysOfWeek(alarm.DaysOfWeek);

            // 載入排除假日設定
            ExcludeHolidaysCheckBox.IsChecked = alarm.ExcludeHolidays;
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

    /// <summary>
    /// 處理瀏覽音樂按鈕的點擊事件，開啟檔案選擇對話框並驗證所選檔案
    /// </summary>
    private void BrowseMusicButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
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
                System.Windows.MessageBox.Show(
                    "選擇的檔案無效！\n\n限制：\n- 僅支援 MP3, WAV, WMA, M4A 格式\n- 檔案大小不得超過 5 MB",
                    "檔案驗證失敗",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // 詢問是否複製到音樂資料夾
            var result = System.Windows.MessageBox.Show(
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
                    System.Windows.MessageBox.Show(
                        "檔案已成功複製到音樂資料夾！",
                        "成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    System.Windows.MessageBox.Show(
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

    /// <summary>
    /// 更新音樂檔案路徑的顯示文字，顯示檔案名稱或預設音效提示
    /// </summary>
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

    /// <summary>
    /// 根據設定的星期清單載入並勾選對應的星期核取方塊
    /// </summary>
    /// <param name="daysOfWeek">要勾選的星期清單（0=日, 1=一, ..., 6=六）</param>
    private void LoadDaysOfWeek(List<int> daysOfWeek)
    {
        // 清除所有勾選
        SundayCheckBox.IsChecked = false;
        MondayCheckBox.IsChecked = false;
        TuesdayCheckBox.IsChecked = false;
        WednesdayCheckBox.IsChecked = false;
        ThursdayCheckBox.IsChecked = false;
        FridayCheckBox.IsChecked = false;
        SaturdayCheckBox.IsChecked = false;

        // 根據設定勾選對應的星期
        if (daysOfWeek != null)
        {
            foreach (var day in daysOfWeek)
            {
                switch (day)
                {
                    case 0: SundayCheckBox.IsChecked = true; break;
                    case 1: MondayCheckBox.IsChecked = true; break;
                    case 2: TuesdayCheckBox.IsChecked = true; break;
                    case 3: WednesdayCheckBox.IsChecked = true; break;
                    case 4: ThursdayCheckBox.IsChecked = true; break;
                    case 5: FridayCheckBox.IsChecked = true; break;
                    case 6: SaturdayCheckBox.IsChecked = true; break;
                }
            }
        }
    }

    /// <summary>
    /// 取得目前勾選的星期核取方塊，轉換為星期數字清單
    /// </summary>
    /// <returns>已勾選的星期清單（0=日, 1=一, ..., 6=六）</returns>
    private List<int> GetSelectedDaysOfWeek()
    {
        var days = new List<int>();

        if (SundayCheckBox.IsChecked == true) days.Add(0);
        if (MondayCheckBox.IsChecked == true) days.Add(1);
        if (TuesdayCheckBox.IsChecked == true) days.Add(2);
        if (WednesdayCheckBox.IsChecked == true) days.Add(3);
        if (ThursdayCheckBox.IsChecked == true) days.Add(4);
        if (FridayCheckBox.IsChecked == true) days.Add(5);
        if (SaturdayCheckBox.IsChecked == true) days.Add(6);

        return days;
    }

    /// <summary>
    /// 處理最大響鈴時間滑桿的值變更事件，更新顯示文字和顏色
    /// </summary>
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

    /// <summary>
    /// 處理儲存按鈕的點擊事件，驗證輸入並儲存鬧鐘設定
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            System.Windows.MessageBox.Show("請輸入鬧鐘名稱", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (HourComboBox.SelectedIndex < 0 || MinuteComboBox.SelectedIndex < 0)
        {
            System.Windows.MessageBox.Show("請選擇時間", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AlarmName = NameTextBox.Text;
        Hour = HourComboBox.SelectedIndex;
        Minute = MinuteComboBox.SelectedIndex;
        IsAlarmEnabled = IsEnabledCheckBox.IsChecked ?? true;
        RingingDuration = (int)RingingDurationSlider.Value;
        MaxRingingDuration = (int)MaxRingingDurationSlider.Value;
        MusicFilePath = _selectedMusicFilePath;
        DaysOfWeek = GetSelectedDaysOfWeek();
        ExcludeHolidays = ExcludeHolidaysCheckBox.IsChecked ?? false;
        IsSaved = true;

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// 處理取消按鈕的點擊事件，關閉對話框且不儲存變更
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = false;
        DialogResult = false;
        Close();
    }
}
