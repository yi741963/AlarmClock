using System.IO;

namespace AlarmClock;

public class MusicManager
{
    private readonly string _musicFolderPath;
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB (約5分鐘的音樂)
    private static readonly string[] AllowedExtensions = { ".mp3", ".wav", ".wma", ".m4a" };

    public MusicManager(string? customMusicFolderPath = null)
    {
        _musicFolderPath = string.IsNullOrEmpty(customMusicFolderPath)
            ? AlarmConfig.GetDefaultMusicFolderPath()
            : customMusicFolderPath;

        EnsureMusicFolderExists();
    }

    private void EnsureMusicFolderExists()
    {
        if (!Directory.Exists(_musicFolderPath))
        {
            Directory.CreateDirectory(_musicFolderPath);
        }
    }

    public string GetMusicFolderPath() => _musicFolderPath;

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

    public static string GetFileSizeString(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        else
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    public static bool IsMusicFileExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return AllowedExtensions.Contains(extension);
    }
}
