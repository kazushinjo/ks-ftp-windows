using System;
using System.Text.Json.Serialization;

namespace KsFtp.Models;

public enum FtpProtocol { Ftp, Ftps, Sftp }

public class ConnectionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "新しい接続";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 21;
    public FtpProtocol ProtocolType { get; set; } = FtpProtocol.Ftp;
    public string Username { get; set; } = "anonymous";
    public string Password { get; set; } = "";
    public string InitialPath { get; set; } = "/";
    // FTP/FTPS のファイル名エンコーディング ("shift_jis" / "utf-8")。SFTP は常に UTF-8。
    public string FileEncoding { get; set; } = "utf-8";

    [JsonIgnore]
    public string ProtocolDisplay => ProtocolType switch
    {
        FtpProtocol.Ftps => "FTPS",
        FtpProtocol.Sftp => "SFTP",
        _ => "FTP"
    };

    [JsonIgnore]
    public int DefaultPort => ProtocolType == FtpProtocol.Sftp ? 22 : 21;
}
