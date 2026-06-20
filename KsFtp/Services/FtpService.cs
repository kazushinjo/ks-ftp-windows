using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;
using Renci.SshNet;
using KsFtp.Models;

namespace KsFtp.Services;

public static class FtpService
{
    // ===== Home Directory =====

    public static async Task<string> GetHomeDirectory(ConnectionProfile profile)
    {
        try
        {
            if (profile.ProtocolType == FtpProtocol.Sftp)
            {
                using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
                await Task.Run(() => client.Connect());
                var wd = client.WorkingDirectory ?? "/";
                return wd.EndsWith("/") ? wd : wd + "/";
            }
            else
            {
                using var client = await ConnectFtpClient(profile);
                var wd = await client.GetWorkingDirectory();
                return string.IsNullOrEmpty(wd) ? "/" : (wd.EndsWith("/") ? wd : wd + "/");
            }
        }
        catch
        {
            return "/";
        }
    }

    // ===== List Directory =====

    public static async Task<List<RemoteFile>> ListDirectory(ConnectionProfile profile, string path)
    {
        if (profile.ProtocolType == FtpProtocol.Sftp)
            return await ListDirectorySftp(profile, path);
        return await ListDirectoryFtp(profile, path);
    }

    private static async Task<List<RemoteFile>> ListDirectoryFtp(ConnectionProfile profile, string path)
    {
        using var client = await ConnectFtpClient(profile);
        var listing = await client.GetListing(path);
        return listing
            .Where(i => i.Name != "." && i.Name != "..")
            .Select(i => new RemoteFile
            {
                Name = i.Name,
                Path = i.FullName + (i.Type == FtpObjectType.Directory ? "/" : ""),
                Size = i.Size,
                IsDirectory = i.Type == FtpObjectType.Directory,
                IsSymlink = i.Type == FtpObjectType.Link,
                ModifiedDate = i.Modified == DateTime.MinValue ? null : i.Modified,
                Permissions = i.Chmod.ToString()
            })
            .OrderByDescending(f => f.IsDirectory)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<List<RemoteFile>> ListDirectorySftp(ConnectionProfile profile, string path)
    {
        var normPath = path.TrimEnd('/');
        if (string.IsNullOrEmpty(normPath)) normPath = "/";

        using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
        await Task.Run(() => client.Connect());
        var items = await Task.Run(() => client.ListDirectory(normPath).ToList());

        return items
            .Where(i => i.Name != "." && i.Name != "..")
            .Select(i => new RemoteFile
            {
                Name = i.Name,
                Path = (normPath == "/" ? "/" : normPath + "/") + i.Name + (i.IsDirectory ? "/" : ""),
                Size = i.Length,
                IsDirectory = i.IsDirectory,
                IsSymlink = i.IsSymbolicLink,
                ModifiedDate = i.LastWriteTime,
                Permissions = ""
            })
            .OrderByDescending(f => f.IsDirectory)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ===== Download (file) =====

    public static async Task Download(ConnectionProfile profile, string remotePath, string localPath)
    {
        if (profile.ProtocolType == FtpProtocol.Sftp)
        {
            using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
            await Task.Run(() =>
            {
                client.Connect();
                using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write);
                client.DownloadFile(remotePath, fs);
            });
        }
        else
        {
            using var client = await ConnectFtpClient(profile);
            var status = await client.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite);
            if (status == FtpStatus.Failed)
                throw new Exception($"ダウンロードに失敗しました: {remotePath}");
        }
    }

    // ===== Download (directory) =====

    public static async Task DownloadDirectory(ConnectionProfile profile, string remotePath, string localPath)
    {
        if (profile.ProtocolType == FtpProtocol.Sftp)
        {
            using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
            await Task.Run(() =>
            {
                client.Connect();
                DownloadDirectorySftp(client, remotePath.TrimEnd('/'), localPath);
            });
        }
        else
        {
            using var client = await ConnectFtpClient(profile);
            var results = await client.DownloadDirectory(localPath, remotePath,
                FtpFolderSyncMode.Update, FtpLocalExists.Overwrite);
            var failCount = results.Count(r => r.IsFailed);
            if (failCount > 0)
                throw new Exception($"{failCount} 件のファイルのダウンロードに失敗しました");
        }
    }

    private static void DownloadDirectorySftp(SftpClient client, string remotePath, string localPath)
    {
        Directory.CreateDirectory(localPath);
        foreach (var item in client.ListDirectory(remotePath))
        {
            if (item.Name == "." || item.Name == "..") continue;
            var remoteItem = remotePath.TrimEnd('/') + "/" + item.Name;
            var localItem = Path.Combine(localPath, item.Name);
            if (item.IsDirectory)
                DownloadDirectorySftp(client, remoteItem, localItem);
            else
            {
                using var fs = new FileStream(localItem, FileMode.Create, FileAccess.Write);
                client.DownloadFile(remoteItem, fs);
            }
        }
    }

    // ===== Upload (file) =====

    public static async Task Upload(ConnectionProfile profile, string localPath, string remotePath)
    {
        if (profile.ProtocolType == FtpProtocol.Sftp)
        {
            using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
            await Task.Run(() =>
            {
                client.Connect();
                using var fs = File.OpenRead(localPath);
                client.UploadFile(fs, remotePath, true);
            });
        }
        else
        {
            using var client = await ConnectFtpClient(profile);
            var status = await client.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, true);
            if (status == FtpStatus.Failed)
                throw new Exception($"アップロードに失敗しました: {remotePath}");
        }
    }

    // ===== Upload (directory) =====

    public static async Task UploadDirectory(ConnectionProfile profile, string localPath, string remotePath)
    {
        if (profile.ProtocolType == FtpProtocol.Sftp)
        {
            using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
            await Task.Run(() =>
            {
                client.Connect();
                UploadDirectorySftp(client, localPath, remotePath);
            });
        }
        else
        {
            using var client = await ConnectFtpClient(profile);
            var results = await client.UploadDirectory(localPath, remotePath,
                FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite);
            var failCount = results.Count(r => r.IsFailed);
            if (failCount > 0)
                throw new Exception($"{failCount} 件のファイルのアップロードに失敗しました");
        }
    }

    private static void UploadDirectorySftp(SftpClient client, string localPath, string remotePath)
    {
        EnsureRemoteDirectorySftp(client, remotePath);
        foreach (var entry in Directory.GetFileSystemEntries(localPath))
        {
            var name = Path.GetFileName(entry);
            var remoteItem = remotePath.TrimEnd('/') + "/" + name;
            if (Directory.Exists(entry))
                UploadDirectorySftp(client, entry, remoteItem);
            else
            {
                using var fs = File.OpenRead(entry);
                client.UploadFile(fs, remoteItem, true);
            }
        }
    }

    private static void EnsureRemoteDirectorySftp(SftpClient client, string remotePath)
    {
        if (client.Exists(remotePath)) return;
        var segments = remotePath.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToList();
        var current = "";
        foreach (var seg in segments)
        {
            current += "/" + seg;
            if (!client.Exists(current))
                try { client.CreateDirectory(current); } catch { }
        }
    }

    // ===== Create Directory =====

    public static async Task CreateDirectory(ConnectionProfile profile, string path)
    {
        var safePath = $"/{path.Trim('/')}";
        if (profile.ProtocolType == FtpProtocol.Sftp)
        {
            using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
            await Task.Run(() =>
            {
                client.Connect();
                client.CreateDirectory(safePath);
            });
        }
        else
        {
            using var client = await ConnectFtpClient(profile);
            await client.CreateDirectory(safePath);
        }
    }

    // ===== Delete =====

    public static async Task DeleteFile(ConnectionProfile profile, string path)
    {
        if (profile.ProtocolType == FtpProtocol.Sftp)
        {
            using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
            await Task.Run(() =>
            {
                client.Connect();
                client.DeleteFile(path);
            });
        }
        else
        {
            using var client = await ConnectFtpClient(profile);
            await client.DeleteFile(path);
        }
    }

    public static async Task DeleteDirectory(ConnectionProfile profile, string path)
    {
        var safePath = path.TrimEnd('/');
        if (profile.ProtocolType == FtpProtocol.Sftp)
        {
            using var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
            await Task.Run(() =>
            {
                client.Connect();
                client.DeleteDirectory(safePath);
            });
        }
        else
        {
            using var client = await ConnectFtpClient(profile);
            await client.DeleteDirectory(safePath);
        }
    }

    // ===== Helpers =====

    // Connect し、その後でエンコーディングを上書き設定する。
    // Connect() 中にサーバーの FEAT UTF8 応答でエンコードが UTF-8 に自動切替されるため、
    // Connect 後に明示設定することで Shift-JIS を確実に維持する。
    private static async Task<AsyncFtpClient> ConnectFtpClient(ConnectionProfile profile)
    {
        var config = new FtpConfig
        {
            ConnectTimeout = 10000,
            ReadTimeout = 30000,
            EncryptionMode = profile.ProtocolType == FtpProtocol.Ftps
                ? FtpEncryptionMode.Explicit
                : FtpEncryptionMode.None,
            ValidateAnyCertificate = true,
        };
        var client = new AsyncFtpClient(profile.Host, profile.Username, profile.Password, profile.Port, config);
        await client.Connect();

        // Connect() 完了後にエンコーディングを強制設定（UTF-8 自動切替を上書き）
        try
        {
            var encName = string.IsNullOrEmpty(profile.FileEncoding) ? "utf-8" : profile.FileEncoding;
            client.Encoding = Encoding.GetEncoding(encName);
        }
        catch
        {
            client.Encoding = Encoding.UTF8;
        }
        return client;
    }
}
