using System;
using System.IO;
using System.Windows.Media;
using KsFtp.Services;

namespace KsFtp.Models;

public class RemoteFile
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public long Size { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsSymlink { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string Permissions { get; set; } = "";

    public string SizeFormatted => IsDirectory ? "--" : FormatSize(Size);
    public string ModifiedFormatted => ModifiedDate?.ToString("yyyy/MM/dd HH:mm") ?? "--";
    public ImageSource? ShellIcon => ShellIconHelper.GetIconByExtension(IsSymlink ? ".lnk" : Name, IsDirectory);

    public string TypeIcon => IsDirectory ? "📁" : IsSymlink ? "🔗" : GetFileIcon(Name);

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }

    private static string GetFileIcon(string name)
    {
        var ext = System.IO.Path.GetExtension(name).ToLowerInvariant();
        return ext switch
        {
            ".txt" or ".md" or ".log" or ".csv" => "📄",
            ".pdf" => "📕",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "🖼",
            ".mp3" or ".wav" or ".aac" or ".flac" => "🎵",
            ".mp4" or ".mov" or ".avi" or ".mkv" => "🎬",
            ".zip" or ".tar" or ".gz" or ".7z" or ".rar" => "📦",
            ".cs" or ".swift" or ".py" or ".js" or ".ts" or ".html" or ".css" => "💻",
            _ => "📄"
        };
    }
}
