using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KsFtp.Models;
using KsFtp.Services;

namespace KsFtp.ViewModels;

public partial class AppState : ObservableObject
{
    private static readonly string ProfilesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KsFtp", "profiles.json");

    public ObservableCollection<ConnectionProfile> Profiles { get; } = new();

    [ObservableProperty]
    private ConnectionProfile? _selectedProfile;

    public ObservableCollection<RemoteFile> RemoteFiles { get; } = new();

    [ObservableProperty]
    private string _currentPath = "/";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isUploading;

    public ObservableCollection<LocalFile> LocalFiles { get; } = new();

    [ObservableProperty]
    private string _localCurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public ObservableCollection<TransferItem> Transfers { get; } = new();

    private readonly List<string> _pathHistory = new() { "/" };
    private int _historyIndex = 0;

    public bool CanGoBack => _historyIndex > 0;
    public bool CanGoForward => _historyIndex < _pathHistory.Count - 1;
    public bool CanGoUp => CurrentPath.TrimEnd('/').Contains('/');
    public bool LocalCanGoUp => new DirectoryInfo(LocalCurrentPath).Parent != null;
    public int PendingTransferCount => Transfers.Count(t => !t.IsFinished);

    public AppState()
    {
        LoadProfiles();
        LoadLocalDirectory(LocalCurrentPath);
    }

    public void LoadProfiles()
    {
        try
        {
            if (File.Exists(ProfilesPath))
            {
                var json = File.ReadAllText(ProfilesPath);
                var profiles = JsonSerializer.Deserialize<List<ConnectionProfile>>(json);
                if (profiles != null)
                    foreach (var p in profiles)
                        Profiles.Add(p);
            }
        }
        catch { }
    }

    private void SaveProfiles()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProfilesPath)!);
            var json = JsonSerializer.Serialize(Profiles.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProfilesPath, json);
        }
        catch { }
    }

    public void AddProfile(ConnectionProfile profile)
    {
        Profiles.Add(profile);
        SaveProfiles();
    }

    public void UpdateProfile(ConnectionProfile profile)
    {
        var existing = Profiles.FirstOrDefault(p => p.Id == profile.Id);
        if (existing != null)
        {
            var idx = Profiles.IndexOf(existing);
            Profiles[idx] = profile;
            if (SelectedProfile?.Id == profile.Id)
                SelectedProfile = profile;
            SaveProfiles();
        }
    }

    public void DeleteProfile(ConnectionProfile profile)
    {
        Profiles.Remove(profile);
        if (SelectedProfile?.Id == profile.Id)
            Disconnect();
        SaveProfiles();
    }

    public async Task Connect(ConnectionProfile profile)
    {
        SelectedProfile = profile;
        IsConnected = false;
        ErrorMessage = null;

        try
        {
            var homeDir = await FtpService.GetHomeDirectory(profile);
            if (!homeDir.EndsWith("/")) homeDir += "/";

            string startPath;
            var sub = profile.InitialPath?.Trim().Trim('/');
            startPath = string.IsNullOrEmpty(sub) ? homeDir : homeDir + sub + "/";

            // List directory first (throws on auth failure / connection error)
            var files = await FtpService.ListDirectory(profile, startPath);

            RemoteFiles.Clear();
            foreach (var f in files) RemoteFiles.Add(f);
            CurrentPath = startPath;
            _pathHistory.Clear();
            _pathHistory.Add(startPath);
            _historyIndex = 0;
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            OnPropertyChanged(nameof(CanGoUp));
            IsConnected = true;
        }
        catch
        {
            SelectedProfile = null;
            IsConnected = false;
            throw;
        }
    }

    public void Disconnect()
    {
        SelectedProfile = null;
        IsConnected = false;
        RemoteFiles.Clear();
        CurrentPath = "/";
        _pathHistory.Clear();
        _pathHistory.Add("/");
        _historyIndex = 0;
        ErrorMessage = null;
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
        OnPropertyChanged(nameof(CanGoUp));
    }

    public async Task LoadDirectory(string path)
    {
        if (SelectedProfile == null) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var files = await FtpService.ListDirectory(SelectedProfile, path);
            RemoteFiles.Clear();
            foreach (var f in files) RemoteFiles.Add(f);
            CurrentPath = path.EndsWith("/") ? path : path + "/";
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            OnPropertyChanged(nameof(CanGoUp));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        IsLoading = false;
    }

    public async Task NavigateTo(string path)
    {
        if (_historyIndex < _pathHistory.Count - 1)
            _pathHistory.RemoveRange(_historyIndex + 1, _pathHistory.Count - _historyIndex - 1);
        _pathHistory.Add(path);
        _historyIndex = _pathHistory.Count - 1;
        await LoadDirectory(path);
    }

    public async Task NavigateBack()
    {
        if (!CanGoBack) return;
        _historyIndex--;
        await LoadDirectory(_pathHistory[_historyIndex]);
    }

    public async Task NavigateForward()
    {
        if (!CanGoForward) return;
        _historyIndex++;
        await LoadDirectory(_pathHistory[_historyIndex]);
    }

    public async Task NavigateUp()
    {
        var trimmed = CurrentPath.TrimEnd('/');
        var lastSlash = trimmed.LastIndexOf('/');
        if (lastSlash < 0) return;
        var parent = lastSlash == 0 ? "/" : trimmed[..lastSlash] + "/";
        await NavigateTo(parent);
    }

    public void LoadLocalDirectory(string path)
    {
        LocalCurrentPath = path;
        var files = LocalFile.Load(path);
        LocalFiles.Clear();
        foreach (var f in files) LocalFiles.Add(f);
        OnPropertyChanged(nameof(LocalCanGoUp));
    }

    public void LocalNavigateUp()
    {
        var parent = Directory.GetParent(LocalCurrentPath)?.FullName;
        if (parent != null) LoadLocalDirectory(parent);
    }

    public void OpenLocalItem(LocalFile file)
    {
        if (file.IsDirectory)
            LoadLocalDirectory(file.Path);
        else
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.Path) { UseShellExecute = true });
    }

    public void CreateLocalDirectory(string name)
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(LocalCurrentPath, name));
            LoadLocalDirectory(LocalCurrentPath);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"フォルダ作成に失敗しました: {ex.Message}";
        }
    }

    public void DeleteLocalItems(IEnumerable<LocalFile> files)
    {
        foreach (var file in files.ToList())
        {
            try
            {
                if (file.IsDirectory) Directory.Delete(file.Path, true);
                else File.Delete(file.Path);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"削除に失敗しました: {ex.Message}";
            }
        }
        LoadLocalDirectory(LocalCurrentPath);
    }

    public async Task DownloadFiles(IEnumerable<RemoteFile> files)
    {
        if (SelectedProfile == null) return;
        foreach (var file in files)
        {
            var item = new TransferItem { Name = file.Name, IsUpload = false, Status = TransferStatus.InProgress };
            Transfers.Add(item);
            OnPropertyChanged(nameof(PendingTransferCount));
            try
            {
                if (file.IsDirectory)
                {
                    var localDir = Path.Combine(LocalCurrentPath, file.Name);
                    await FtpService.DownloadDirectory(SelectedProfile, file.Path.TrimEnd('/'), localDir);
                }
                else
                {
                    await FtpService.Download(SelectedProfile, file.Path, Path.Combine(LocalCurrentPath, file.Name));
                }
                item.Status = TransferStatus.Completed;
                item.Progress = 1.0;
            }
            catch (Exception ex)
            {
                item.Status = TransferStatus.Failed;
                item.ErrorMessage = ex.Message;
            }
            OnPropertyChanged(nameof(PendingTransferCount));
        }
        LoadLocalDirectory(LocalCurrentPath);
    }

    public async Task UploadFiles(IEnumerable<string> localPaths)
    {
        if (SelectedProfile == null || IsUploading) return;
        IsUploading = true;
        try
        {
            foreach (var localPath in localPaths)
            {
                var name = Path.GetFileName(localPath);
                var item = new TransferItem { Name = name, IsUpload = true, Status = TransferStatus.InProgress };
                Transfers.Add(item);
                OnPropertyChanged(nameof(PendingTransferCount));
                try
                {
                    var remoteDest = CurrentPath.TrimEnd('/') + "/" + name;
                    if (Directory.Exists(localPath))
                        await FtpService.UploadDirectory(SelectedProfile, localPath, remoteDest);
                    else
                        await FtpService.Upload(SelectedProfile, localPath, remoteDest);
                    item.Status = TransferStatus.Completed;
                    item.Progress = 1.0;
                }
                catch (Exception ex)
                {
                    item.Status = TransferStatus.Failed;
                    item.ErrorMessage = ex.Message;
                }
                OnPropertyChanged(nameof(PendingTransferCount));
            }
        }
        finally { IsUploading = false; }
        await LoadDirectory(CurrentPath);
    }

    public async Task CreateRemoteDirectory(string name)
    {
        if (SelectedProfile == null) return;
        try
        {
            await FtpService.CreateDirectory(SelectedProfile, CurrentPath + name);
            await LoadDirectory(CurrentPath);
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    public async Task CreateRemoteFile(string name)
    {
        if (SelectedProfile == null) return;
        var tmp = Path.Combine(Path.GetTempPath(), name);
        File.WriteAllBytes(tmp, Array.Empty<byte>());
        try
        {
            await FtpService.Upload(SelectedProfile, tmp, CurrentPath + name);
            await LoadDirectory(CurrentPath);
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { try { File.Delete(tmp); } catch { } }
    }

    public async Task DeleteRemoteItems(IEnumerable<RemoteFile> files)
    {
        if (SelectedProfile == null) return;
        foreach (var file in files.ToList())
        {
            try
            {
                if (file.IsDirectory) await FtpService.DeleteDirectory(SelectedProfile, file.Path);
                else await FtpService.DeleteFile(SelectedProfile, file.Path);
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
        }
        await LoadDirectory(CurrentPath);
    }

    public async Task Refresh() { if (SelectedProfile != null) await LoadDirectory(CurrentPath); }

    public void ClearFinishedTransfers()
    {
        foreach (var t in Transfers.Where(t => t.IsFinished).ToList())
            Transfers.Remove(t);
        OnPropertyChanged(nameof(PendingTransferCount));
    }
}
