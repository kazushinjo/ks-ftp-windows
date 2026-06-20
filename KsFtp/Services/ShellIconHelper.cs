using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KsFtp.Services;

public static class ShellIconHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    private const uint SHGFI_LARGEICON = 0x0;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

    private static readonly ConcurrentDictionary<string, ImageSource?> _cache = new();

    // For remote files: use extension only (no real file needed)
    public static ImageSource? GetIconByExtension(string fileName, bool isDirectory)
    {
        var key = isDirectory
            ? "__dir__"
            : (Path.GetExtension(fileName).ToLowerInvariant() is { Length: > 0 } ext ? ext : "__file__");

        return _cache.GetOrAdd(key, _ => isDirectory
            ? FetchIcon("folder", FILE_ATTRIBUTE_DIRECTORY)
            : FetchIcon("file" + key, FILE_ATTRIBUTE_NORMAL));
    }

    // For local files: use actual path for exact icon (e.g. .exe with app icon)
    public static ImageSource? GetIconByPath(string fullPath, bool isDirectory)
    {
        var key = isDirectory
            ? "__dir__"
            : (Path.GetExtension(fullPath).ToLowerInvariant() is { Length: > 0 } ext ? ext : "__file__");

        return _cache.GetOrAdd(key, _ =>
        {
            // Try actual path first (gives app-specific icons for .exe etc.)
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                var icon = FetchIconFromPath(fullPath, isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL);
                if (icon != null) return icon;
            }
            return isDirectory
                ? FetchIcon("folder", FILE_ATTRIBUTE_DIRECTORY)
                : FetchIcon("file" + key, FILE_ATTRIBUTE_NORMAL);
        });
    }

    private static ImageSource? FetchIcon(string fakeName, uint fileAttributes)
    {
        var shfi = new SHFILEINFO();
        var result = SHGetFileInfo(fakeName, fileAttributes, ref shfi,
            (uint)Marshal.SizeOf(shfi),
            SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | SHGFI_LARGEICON);

        if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero) return null;
        return CreateAndFreeIcon(shfi.hIcon);
    }

    private static ImageSource? FetchIconFromPath(string path, uint fileAttributes)
    {
        var shfi = new SHFILEINFO();
        var result = SHGetFileInfo(path, fileAttributes, ref shfi,
            (uint)Marshal.SizeOf(shfi),
            SHGFI_ICON | SHGFI_LARGEICON);

        if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero) return null;
        return CreateAndFreeIcon(shfi.hIcon);
    }

    private static ImageSource? CreateAndFreeIcon(IntPtr hIcon)
    {
        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }
}
