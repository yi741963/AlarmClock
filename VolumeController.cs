using System.Runtime.InteropServices;

namespace AlarmClock;

/// <summary>
/// 音量控制類別，用於控制 Windows 系統音量
/// </summary>
public static class VolumeController
{
    [DllImport("winmm.dll")]
    private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    [DllImport("winmm.dll")]
    private static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

    /// <summary>
    /// 設定系統音量
    /// </summary>
    /// <param name="volume">音量大小 (0-100)</param>
    public static void SetVolume(int volume)
    {
        try
        {
            // 確保音量在0-100範圍內
            volume = Math.Max(0, Math.Min(100, volume));

            // 計算音量值 (0x0000 - 0xFFFF)
            // 左聲道和右聲道都設定為相同音量
            uint volumeValue = (uint)((volume * 0xFFFF) / 100);
            uint stereoVolume = (volumeValue << 16) | volumeValue;

            waveOutSetVolume(IntPtr.Zero, stereoVolume);
        }
        catch
        {
            // 忽略錯誤
        }
    }

    /// <summary>
    /// 取得目前的系統音量
    /// </summary>
    /// <returns>音量大小 (0-100)</returns>
    public static int GetVolume()
    {
        try
        {
            uint volumeValue;
            waveOutGetVolume(IntPtr.Zero, out volumeValue);

            // 取得左聲道音量
            uint leftChannel = volumeValue & 0xFFFF;

            // 轉換為 0-100 範圍
            return (int)((leftChannel * 100) / 0xFFFF);
        }
        catch
        {
            return 50; // 預設音量
        }
    }
}
