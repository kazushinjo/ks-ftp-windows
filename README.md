# KS-FTP for Windows

Windows 11 用 FTP / FTPS / SFTP クライアント。macOS 版 [ks-ftp](https://github.com/kazushinjo/ks-ftp) の Windows 移植版。

WPF / .NET 8 製。インストール不要の単体実行ファイル。

---

## 📚 セットアップ・ビルド（別の PC での準備）

別の PC で KS-FTP for Windows をビルド・開発する場合は、以下を参照してください：

### 🚀 **最も簡単な方法：統合セットアップスクリプト**

```powershell
.\scripts\setup.ps1
```

このスクリプトが自動的に以下を実行します：
1. ✅ 環境検証
2. ✅ Release ビルド実行
3. ✅ アプリ実行確認
4. ✅ 完了メッセージ

**読了時間: 2 分（スクリプトが全て処理）**

---

### 📖 詳細ドキュメント

| 用途 | ドキュメント | 読了時間 |
|------|-------------|--------|
| **最初に読むべき統合ガイド** | [SETUP_GUIDE.md](SETUP_GUIDE.md) | 5分 |
| **ビルド実行前のチェックリスト** | [CHECKLIST.md](CHECKLIST.md) | 5～10分 |
| **ビルド手順の詳細** | [BUILD_GUIDE.md](BUILD_GUIDE.md) | 10～15分 |
| **開発環境セットアップ** | [DEVELOPMENT.md](DEVELOPMENT.md) | 20～30分 |
| **環境検証スクリプト** | [build-verify.ps1](build-verify.ps1) | (自動実行) |

> **クイックスタート:** `.\scripts\setup.ps1` を実行するだけで、すべてのセットアップが完了します。

---

## スクリーンショット

> 接続後のデュアルペイン画面（左：ローカル、右：リモート）

---

## 機能

| 機能 | 詳細 |
|------|------|
| プロトコル | FTP / FTPS (明示的TLS) / SFTP (SSH) |
| デュアルペイン | 左にローカル、右にリモートを同時表示 |
| ファイル操作 | ダウンロード・アップロード・削除・フォルダ作成 |
| フォルダ転送 | フォルダ単位での再帰ダウンロード・アップロード |
| 文字コード | UTF-8（デフォルト）/ Shift-JIS を接続ごとに設定可能 |
| シェルアイコン | Windows のファイルタイプアイコンをそのまま表示 |
| 転送キュー | 転送進捗を一覧表示、完了済みをまとめてクリア |
| 接続プロファイル | 複数の接続先を保存・管理 |
| 初期フォルダ | 接続後に開くフォルダをプロファイルごとに指定 |

---

## ダウンロード

[Releases](https://github.com/kazushinjo/ks-ftp-windows/releases) から最新の `KsFtp-windows-x64.zip` をダウンロードして展開し、`KsFtp.exe` を実行するだけで使えます。

- .NET ランタイム不要（自己完結型）
- インストール不要

**動作要件**: Windows 10 / 11 x64

---

## 使い方

### 接続先を追加する

1. サイドバー右上の **＋** ボタンをクリック
2. 接続名・プロトコル・ホスト・ポート・ユーザー名・パスワードを入力
3. 必要に応じてフォルダ名（ホームディレクトリ配下）と文字コードを設定
4. **保存** をクリック

### 接続する

サイドバーのプロファイル名をクリックすると接続します。

### ファイルをダウンロードする

リモートペインでファイルまたはフォルダを右クリック → **ダウンロード**

複数選択（Ctrl / Shift クリック）も対応しています。

### ファイルをアップロードする

ローカルペインでファイルまたはフォルダを右クリック → **アップロード**

### 文字コードについて

| プロトコル | デフォルト | 変更 |
|-----------|-----------|------|
| FTP / FTPS | UTF-8 | 接続ダイアログで Shift-JIS に変更可能 |
| SFTP | UTF-8 固定 | 変更不可（SFTP 仕様） |

日本語ファイル名が文字化けする場合は、接続を編集して **Shift-JIS** を選択してください。

---

## 接続プロファイルの保存場所

```
%APPDATA%\KsFtp\profiles.json
```

---

## 開発

### ビルド要件

- .NET 8 SDK
- Windows 10 / 11

### ビルド

```powershell
dotnet build KsFtp/KsFtp.csproj -c Release
```

### 実行ファイル作成（単体 exe）

```powershell
dotnet publish KsFtp/KsFtp.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/
```

### 使用ライブラリ

| ライブラリ | 用途 |
|-----------|------|
| [FluentFTP](https://github.com/robinrodricks/FluentFTP) | FTP / FTPS 通信 |
| [SSH.NET](https://github.com/sshnet/SSH.NET) | SFTP 通信 |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVM 基盤 |

---

## ライセンス

MIT
