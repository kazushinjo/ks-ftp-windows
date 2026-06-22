# KS-FTP for Windows ビルドガイド

このドキュメントは KS-FTP for Windows の Release ビルド手順を説明します。

## 前提条件

- Windows 10 / 11 (64-bit)
- .NET SDK 8.0 以上
- Visual Studio 2022 または Visual Studio Code

詳細な開発環境セットアップは [DEVELOPMENT.md](DEVELOPMENT.md) を参照してください。

## クイックスタート

### コマンドラインでのビルド

```powershell
cd C:\Claude\ks-ftp-windows
dotnet build -c Release
```

**成功時の出力:**
```
Build succeeded. 0 Warning(s), 0 Error(s)
```

**ビルド成果物の場所:**
```
KsFtp\bin\Release\net8.0-windows\KsFtp.exe
```

### Visual Studio 2022 でのビルド

1. `KsFtp.sln` を Visual Studio 2022 で開く
2. ビルド → ソリューションのビルド（Ctrl+Shift+B）
3. 出力ウィンドウに "ビルドに成功しました。警告: 0、エラー: 0" と表示されることを確認

## ビルド構成

### Debug ビルド

開発・デバッグ用

```powershell
dotnet build -c Debug
```

### Release ビルド

本番用（配布・インストール用）

```powershell
dotnet build -c Release
```

## ビルド成果物の確認

### Release ビルド出力

```
KsFtp/bin/Release/net8.0-windows/
├── KsFtp.exe                    # メイン実行ファイル
├── KsFtp.dll                    # メイン DLL
├── FluentFTP.dll                # FTP ライブラリ
├── SSH.NET.dll                  # SFTP (SSH) ライブラリ
├── CommunityToolkit.Mvvm.dll    # MVVM ツールキット
└── ... その他の依存ファイル
```

## 一般的なビルドエラーと対処方法

### エラー 1: "ターゲット フレームワーク 'net8.0-windows' が見つかりません"

**原因:** .NET SDK 8.0 がインストールされていない

**解決方法:**
```powershell
# インストール済みの SDK を確認
dotnet --list-sdks

# .NET 8.0 がない場合はインストール
winget install Microsoft.DotNet.SDK.8

# または https://dotnet.microsoft.com/download/dotnet/8.0 から手動インストール
```

### エラー 2: "プロジェクト ファイル 'XXX' が見つかりません"

**原因:** ファイルが欠損、または不正なパス

**解決方法:**
```powershell
# キャッシュをクリアしてリストア
dotnet clean
dotnet restore
dotnet build -c Release
```

### エラー 3: "NuGet パッケージを復元できません"

**原因:** ネットワーク接続問題、または古い NuGet ソース設定

**解決方法:**
```powershell
# 明示的にリストア
dotnet restore --no-cache

# NuGet ソースをリセット
dotnet nuget remove source nuget.org
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

## ビルド検証チェックリスト

ビルド前に以下を確認してください：

- [ ] .NET SDK バージョンが 8.0.x である
- [ ] Visual Studio が最新バージョン（2022以上）である
- [ ] ディスク容量が十分である（最低 500MB）
- [ ] antivirus/firewall が dotnet.exe をブロックしていない
- [ ] インターネット接続がある（NuGet パッケージダウンロード用）

ビルド後に以下を確認してください：

- [ ] ビルドメッセージに "Build succeeded" と表示されている
- [ ] 警告数: 0
- [ ] エラー数: 0
- [ ] `KsFtp.exe` が `KsFtp/bin/Release/net8.0-windows/` に生成されている
- [ ] ファイルサイズが 5MB～30MB の範囲である

## マルチ PC ビルド検証

複数の PC でビルドする場合：

### PC A でのビルド

```powershell
cd C:\Claude\ks-ftp-windows
dotnet build -c Release
# 出力: ビルドに成功しました。警告: 0、エラー: 0
```

### PC B でのビルド（同じコマンド）

```powershell
# 同じリポジトリをクローン
git clone https://github.com/kazushinjo/ks-ftp-windows.git
cd ks-ftp-windows
dotnet build -c Release
# 出力: ビルドに成功しました。警告: 0、エラー: 0
```

### 両 PC での成果物検証

**ファイルハッシュで検証**（オプション）

```powershell
# PC A での KsFtp.exe のハッシュ
Get-FileHash "KsFtp/bin/Release/net8.0-windows/KsFtp.exe"

# PC B での同ファイルのハッシュ
# 同じハッシュ値が出力されれば、ビルド結果が一致しています
```

## リリースビルド

GitHub Release 作成用：

```powershell
# Release ビルドの生成
dotnet build -c Release

# 成果物をパッケージ化（ZIP作成など）
$path = "KsFtp/bin/Release/net8.0-windows"
Compress-Archive -Path $path -DestinationPath "KsFtp-windows-v1.0.zip"
```

## トラブルシューティング

### ビルド完了後も exe が起動しない

**確認事項:**
- Windows 10 / 11 であること
- .NET 8.0 Runtime がインストールされていること

```powershell
# ランタイム確認
dotnet --list-runtimes
# 出力に "Microsoft.WindowsDesktop.App 8.0.x" が含まれる必要があります
```

**インストール方法:**
```powershell
winget install Microsoft.DotNet.Runtime.8
```

### ビルド時間が長い

初回ビルドは NuGet パッケージの初期ダウンロードで 3～5 分かかります。

2 回目以降は 1～2 分程度です。

キャッシュが重い場合はクリア：

```powershell
dotnet nuget locals all --clear
```

## 参考リンク

- [.NET 8 ドキュメント](https://learn.microsoft.com/ja-jp/dotnet/core/whats-new/dotnet-8)
- [WPF の概要](https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/?view=netdesktop-8.0)
- [GitHub - KS-FTP for Windows](https://github.com/kazushinjo/ks-ftp-windows)

---

**最終更新:** 2026-06-23  
**対応バージョン:** v1.0  
**対応 .NET:** 8.0 以上
