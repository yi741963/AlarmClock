using System.Windows;
using Microsoft.Win32;

namespace AlarmClock;

public partial class SettingsDialog : Window
{
    public int IdleThresholdSeconds { get; private set; }
    public string MusicFolderPath { get; private set; } = "";
    public bool IsSaved { get; private set; }

    /// <summary>
    /// 建立設定對話框
    /// </summary>
    /// <param name="config">鬧鐘設定物件</param>
    public SettingsDialog(AlarmConfig config)
    {
        InitializeComponent();

        // 載入現有設定
        IdleThresholdSlider.Value = config.IdleThresholdSeconds;
        MusicFolderPath = config.MusicFolderPath;
        UpdateMusicFolderDisplay();
    }

    /// <summary>
    /// 處理閒置閾值滑桿的值變更事件，更新顯示文字
    /// </summary>
    private void IdleThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IdleThresholdText != null)
        {
            int value = (int)e.NewValue;
            IdleThresholdText.Text = $"{value} 秒";
        }
    }

    /// <summary>
    /// 更新音樂資料夾路徑的顯示文字
    /// </summary>
    private void UpdateMusicFolderDisplay()
    {
        if (string.IsNullOrEmpty(MusicFolderPath))
        {
            MusicFolderTextBox.Text = "預設路徑 (%AppData%\\SmartAlarmClock\\Music)";
        }
        else
        {
            MusicFolderTextBox.Text = MusicFolderPath;
        }
    }

    /// <summary>
    /// 處理瀏覽資料夾按鈕的點擊事件，開啟資料夾選擇對話框
    /// </summary>
    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "選擇音樂資料夾"
        };

        if (dialog.ShowDialog() == true)
        {
            MusicFolderPath = dialog.FolderName;
            UpdateMusicFolderDisplay();
        }
    }

    /// <summary>
    /// 處理儲存按鈕的點擊事件，儲存設定並關閉對話框
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        IdleThresholdSeconds = (int)IdleThresholdSlider.Value;
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
