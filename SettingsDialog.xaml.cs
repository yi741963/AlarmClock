using System.Windows;
using Microsoft.Win32;

namespace AlarmClock;

public partial class SettingsDialog : Window
{
    public int IdleThresholdSeconds { get; private set; }
    public string MusicFolderPath { get; private set; } = "";
    public bool IsSaved { get; private set; }

    public SettingsDialog(AlarmConfig config)
    {
        InitializeComponent();

        // 載入現有設定
        IdleThresholdSlider.Value = config.IdleThresholdSeconds;
        MusicFolderPath = config.MusicFolderPath;
        UpdateMusicFolderDisplay();
    }

    private void IdleThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IdleThresholdText != null)
        {
            int value = (int)e.NewValue;
            IdleThresholdText.Text = $"{value} 秒";
        }
    }

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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        IdleThresholdSeconds = (int)IdleThresholdSlider.Value;
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
