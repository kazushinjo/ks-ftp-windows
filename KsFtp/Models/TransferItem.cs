using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KsFtp.Models;

public enum TransferStatus { Waiting, InProgress, Completed, Failed }

public partial class TransferItem : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required bool IsUpload { get; init; }

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private TransferStatus _status = TransferStatus.Waiting;

    [ObservableProperty]
    private string _errorMessage = "";

    public string DirectionIcon => IsUpload ? "⬆" : "⬇";
    public bool IsFinished => Status is TransferStatus.Completed or TransferStatus.Failed;

    public string StatusText => Status switch
    {
        TransferStatus.Waiting => "待機中",
        TransferStatus.InProgress => IsUpload ? "アップロード中..." : "ダウンロード中...",
        TransferStatus.Completed => "完了",
        TransferStatus.Failed => $"エラー: {ErrorMessage}",
        _ => ""
    };
}
