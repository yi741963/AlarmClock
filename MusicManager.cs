using System.IO;

namespace AlarmClock;

/// <summary>
/// 音樂管理器，負責管理音樂檔案的驗證、複製和刪除操作
/// </summary>
public class MusicManager
{
    private readonly string _musicFolderPath;
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB (約5分鐘的音樂)
    private static readonly string[] AllowedExtensions = { ".mp3", ".wav", ".wma", ".m4a" };

    /// <summary>
    /// 建立音樂管理器
    /// </summary>
    /// <param name="customMusicFolderPath">自訂音樂資料夾路徑，若為空則使用預設路徑</param>
    public MusicManager(string? customMusicFolderPath = null)
    {
        _musicFolderPath = string.IsNullOrEmpty(customMusicFolderPath)
            ? AlarmConfig.GetDefaultMusicFolderPath()
            : customMusicFolderPath;

        EnsureMusicFolderExists();
    }

    /// <summary>
    /// 確保音樂資料夾存在，若不存在則建立
    /// </summary>
    private void EnsureMusicFolderExists()
    {
        if (!Directory.Exists(_musicFolderPath))
        {
            Directory.CreateDirectory(_musicFolderPath);
        }
    }

    /// <summary>
    /// 取得音樂資料夾路徑
    /// </summary>
    /// <returns>音樂資料夾的完整路徑</returns>
    public string GetMusicFolderPath() => _musicFolderPath;

    /// <summary>
    /// 驗證音樂檔案是否有效（檢查檔案存在、副檔名及檔案大小）
    /// </summary>
    /// <param name="filePath">要驗證的檔案路徑</param>
    /// <returns>true 表示檔案有效，false 表示無效</returns>
    public bool IsValidMusicFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        // 檢查副檔名
        var extension = Path.GetExtension(filePath).ToLower();
        if (!AllowedExtensions.Contains(extension))
            return false;

        // 檢查檔案大小
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxFileSizeBytes)
            return false;

        return true;
    }

    /// <summary>
    /// 複製音樂檔案到音樂資料夾，若檔案已存在則加上時間戳記
    /// </summary>
    /// <param name="sourceFilePath">來源檔案路徑</param>
    /// <returns>成功時返回目標檔案路徑，失敗時返回 null</returns>
    public string? CopyMusicFileToFolder(string sourceFilePath)
    {
        if (!IsValidMusicFile(sourceFilePath))
            return null;

        try
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var destinationPath = Path.Combine(_musicFolderPath, fileName);

            // 如果檔案已存在，加上時間戳記
            if (File.Exists(destinationPath))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                fileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
                destinationPath = Path.Combine(_musicFolderPath, fileName);
            }

            File.Copy(sourceFilePath, destinationPath, false);
            return destinationPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 取得音樂資料夾中所有支援的音樂檔案
    /// </summary>
    /// <returns>音樂檔案路徑清單，依檔案名稱排序</returns>
    public List<string> GetAllMusicFiles()
    {
        var musicFiles = new List<string>();

        if (!Directory.Exists(_musicFolderPath))
            return musicFiles;

        foreach (var extension in AllowedExtensions)
        {
            var files = Directory.GetFiles(_musicFolderPath, $"*{extension}");
            musicFiles.AddRange(files);
        }

        return musicFiles.OrderBy(f => Path.GetFileName(f)).ToList();
    }

    /// <summary>
    /// 刪除指定的音樂檔案
    /// </summary>
    /// <param name="filePath">要刪除的檔案路徑</param>
    /// <returns>true 表示刪除成功，false 表示失敗或檔案不存在</returns>
    public bool DeleteMusicFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 將檔案大小（位元組）轉換為易讀的字串格式
    /// </summary>
    /// <param name="bytes">檔案大小（位元組）</param>
    /// <returns>格式化的檔案大小字串（B, KB, 或 MB）</returns>
    public static string GetFileSizeString(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        else
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    /// <summary>
    /// 檢查檔案的副檔名是否為支援的音樂格式
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <returns>true 表示是支援的音樂格式，false 表示不支援</returns>
    public static bool IsMusicFileExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return AllowedExtensions.Contains(extension);
    }
}
