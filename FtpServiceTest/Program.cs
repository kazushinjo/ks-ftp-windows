using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;

// Shift-JIS 等のコードページを有効化（.NET Core 以降の必須登録）
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

const string Host = "127.0.0.1";
const int Port = 2121;
const string User = "testuser";
const string Pass = "testpass";
const string FtpRoot = @"C:\claude\ks-ftp-windows\ftproot";

int passed = 0, failed = 0;

async Task RunTest(string name, Func<Task> test)
{
    try
    {
        await test();
        Console.WriteLine($"[PASS] {name}");
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAIL] {name}: {ex.Message}");
        failed++;
    }
}

static AsyncFtpClient MakeClient(string encoding = "shift_jis")
{
    var c = new AsyncFtpClient(Host, User, Pass, Port,
        new FtpConfig { ConnectTimeout = 5000, ValidateAnyCertificate = true });
    try { c.Encoding = Encoding.GetEncoding(encoding); } catch { c.Encoding = Encoding.UTF8; }
    return c;
}

Console.WriteLine("=== KsFtp FtpService Tests (FluentFTP) ===");
Console.WriteLine($"Server: ftp://{User}@{Host}:{Port}");

await RunTest("Connect + ListDirectory", async () =>
{
    using var ftp = MakeClient();
    await ftp.Connect();
    var listing = await ftp.GetListing("/");
    var names = listing.Select(i => i.Name).ToList();
    if (!names.Contains("hello.txt"))
        throw new Exception($"hello.txt not found. Files: {string.Join(", ", names)}");
    if (!names.Contains("subdir"))
        throw new Exception($"subdir not found. Files: {string.Join(", ", names)}");
    Console.Write($"  ({names.Count} items) ");
});

await RunTest("Download file (FtpLocalExists.Overwrite)", async () =>
{
    var localPath = Path.Combine(Path.GetTempPath(), "ksftp_test_download.txt");
    try
    {
        using var ftp = MakeClient();
        await ftp.Connect();
        var status = await ftp.DownloadFile(localPath, "/hello.txt", FtpLocalExists.Overwrite);
        if (status == FtpStatus.Failed)
            throw new Exception("DownloadFile returned FtpStatus.Failed");
        var content = await File.ReadAllTextAsync(localPath);
        if (!content.Contains("Hello from FTP server"))
            throw new Exception($"Unexpected content: {content}");
        Console.Write($"  ({content.Length} bytes) ");
    }
    finally { try { File.Delete(localPath); } catch { } }
});

await RunTest("Download overwrites existing (no stale content)", async () =>
{
    var localPath = Path.Combine(Path.GetTempPath(), "ksftp_test_overwrite.txt");
    try
    {
        await File.WriteAllTextAsync(localPath, new string('X', 1000));
        using var ftp = MakeClient();
        await ftp.Connect();
        await ftp.DownloadFile(localPath, "/hello.txt", FtpLocalExists.Overwrite);
        var content = await File.ReadAllTextAsync(localPath);
        if (content.Contains("XXXX"))
            throw new Exception("Stale content detected - file not fully overwritten!");
        Console.Write($"  (clean overwrite, {content.Length} bytes) ");
    }
    finally { try { File.Delete(localPath); } catch { } }
});

await RunTest("Upload file (FtpRemoteExists.Overwrite)", async () =>
{
    var localPath = Path.Combine(Path.GetTempPath(), "ksftp_test_upload.txt");
    try
    {
        await File.WriteAllTextAsync(localPath, "Uploaded by FtpServiceTest\n");
        using var ftp = MakeClient();
        await ftp.Connect();
        var status = await ftp.UploadFile(localPath, "/ftp_test_upload.txt",
            FtpRemoteExists.Overwrite, true);
        if (status == FtpStatus.Failed)
            throw new Exception("UploadFile returned FtpStatus.Failed");
        var serverPath = Path.Combine(FtpRoot, "ftp_test_upload.txt");
        if (!File.Exists(serverPath))
            throw new Exception($"Uploaded file not found at {serverPath}");
        var remoteContent = await File.ReadAllTextAsync(serverPath);
        if (!remoteContent.Contains("FtpServiceTest"))
            throw new Exception($"Wrong content: {remoteContent}");
        Console.Write($"  ({remoteContent.Length} bytes on server) ");
    }
    finally { try { File.Delete(localPath); } catch { } }
});

await RunTest("Download nonexistent file throws exception", async () =>
{
    var localPath = Path.Combine(Path.GetTempPath(), "ksftp_test_notfound.txt");
    bool threw = false;
    try
    {
        using var ftp = MakeClient();
        await ftp.Connect();
        // FluentFTP throws for non-existent remote files (exception propagates to caller)
        await ftp.DownloadFile(localPath, "/does_not_exist_9999.txt", FtpLocalExists.Overwrite);
    }
    catch
    {
        threw = true;
    }
    finally { try { File.Delete(localPath); } catch { } }
    if (!threw) throw new Exception("Expected exception for non-existent file, but none was thrown");
    Console.Write("  (exception correctly thrown) ");
});

await RunTest("GetWorkingDirectory returns valid path", async () =>
{
    using var ftp = MakeClient();
    await ftp.Connect();
    var wd = await ftp.GetWorkingDirectory();
    if (string.IsNullOrEmpty(wd))
        throw new Exception("GetWorkingDirectory returned empty string");
    if (!wd.StartsWith("/"))
        throw new Exception($"Expected absolute path, got: {wd}");
    Console.Write($"  (WD={wd}) ");
});

// Test 7: DownloadDirectory
await RunTest("DownloadDirectory (recursive)", async () =>
{
    var localDir = Path.Combine(Path.GetTempPath(), "ksftp_test_dldir_" + Guid.NewGuid().ToString("N")[..6]);
    try
    {
        using var ftp = MakeClient();
        await ftp.Connect();
        var results = await ftp.DownloadDirectory(localDir, "/testdir",
            FtpFolderSyncMode.Update, FtpLocalExists.Overwrite);
        var failed2 = results.Count(r => r.IsFailed);
        if (failed2 > 0) throw new Exception($"{failed2} files failed");

        var file1 = Path.Combine(localDir, "file1.txt");
        var deep  = Path.Combine(localDir, "nested", "deep.txt");
        if (!File.Exists(file1)) throw new Exception($"file1.txt not downloaded at {file1}");
        if (!File.Exists(deep))  throw new Exception($"nested/deep.txt not downloaded at {deep}");
        var content = await File.ReadAllTextAsync(file1);
        if (!content.Contains("File 1")) throw new Exception($"Wrong content: {content}");
        Console.Write($"  ({results.Count} items, file1={File.Exists(file1)}, deep={File.Exists(deep)}) ");
    }
    finally { try { Directory.Delete(localDir, true); } catch { } }
});

// Test 8: UploadDirectory
await RunTest("UploadDirectory (recursive)", async () =>
{
    var localDir = Path.Combine(Path.GetTempPath(), "ksftp_test_uldir_" + Guid.NewGuid().ToString("N")[..6]);
    var remoteDir = "/upload_dir_test";
    try
    {
        Directory.CreateDirectory(Path.Combine(localDir, "sub"));
        await File.WriteAllTextAsync(Path.Combine(localDir, "root.txt"), "root content");
        await File.WriteAllTextAsync(Path.Combine(localDir, "sub", "child.txt"), "child content");

        using var ftp = MakeClient();
        await ftp.Connect();
        var results = await ftp.UploadDirectory(localDir, remoteDir,
            FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite);
        var failed2 = results.Count(r => r.IsFailed);
        if (failed2 > 0) throw new Exception($"{failed2} files failed");

        // Verify on server
        var rootOnServer = Path.Combine(FtpRoot, "upload_dir_test", "root.txt");
        var childOnServer = Path.Combine(FtpRoot, "upload_dir_test", "sub", "child.txt");
        if (!File.Exists(rootOnServer))  throw new Exception($"root.txt not on server at {rootOnServer}");
        if (!File.Exists(childOnServer)) throw new Exception($"sub/child.txt not on server");
        Console.Write($"  ({results.Count} items uploaded) ");
    }
    finally { try { Directory.Delete(localDir, true); } catch { } }
});

// Test 9: Shift-JIS filename listing
await RunTest("Shift-JIS: list Japanese filenames", async () =>
{
    using var ftp = MakeClient("shift_jis");
    await ftp.Connect();
    var listing = await ftp.GetListing("/");
    var names = listing.Select(i => i.Name).ToList();
    // テスト.txt, 日本語ファイル.csv, 画像データ.png が存在するはず
    var jpNames = names.Where(n => n.Any(c => c > 0x7F)).ToList();
    if (jpNames.Count == 0)
        throw new Exception($"No Japanese filenames found. Files: {string.Join(", ", names)}");
    foreach (var n in jpNames) Console.Write($"\n    [{n}]");
    Console.Write(" ");
});

// Test 10: Verify Shift-JIS encoding is set on the client object
await RunTest("Shift-JIS: client.Encoding is set correctly", async () =>
{
    using var ftp = MakeClient("shift_jis");
    var enc = ftp.Encoding;
    if (enc == null)
        throw new Exception("client.Encoding is null");
    // Shift-JIS has multiple code page aliases: 932, shift_jis, shift-jis, sjis
    var codepage = enc.CodePage;
    if (codepage != 932)
        throw new Exception($"Expected Shift-JIS (932), got codepage {codepage} ({enc.EncodingName})");
    Console.Write($"  (CodePage={codepage}, {enc.EncodingName}) ");
});

// Test 11: UTF-8 vs Shift-JIS shows different results
await RunTest("Shift-JIS vs UTF-8: encoding matters", async () =>
{
    using var ftpSjis = MakeClient("shift_jis");
    await ftpSjis.Connect();
    var sjisListing = await ftpSjis.GetListing("/");
    var sjisJp = sjisListing.Where(i => i.Name.Any(c => c > 0x7F)).Select(i => i.Name).ToList();

    using var ftpUtf8 = MakeClient("utf-8");
    await ftpUtf8.Connect();
    var utf8Listing = await ftpUtf8.GetListing("/");
    var utf8Jp = utf8Listing.Where(i => i.Name.Any(c => c > 0x7F)).Select(i => i.Name).ToList();

    // Both should find Japanese names, but they may differ depending on server encoding
    Console.Write($"  (sjis={sjisJp.Count} jp files, utf8={utf8Jp.Count} jp files) ");
    if (sjisJp.Count == 0 && utf8Jp.Count == 0)
        throw new Exception("Neither encoding found Japanese filenames");
});

Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed");
if (failed > 0) Environment.Exit(1);
