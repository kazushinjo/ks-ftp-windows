# KS-FTP for Windows 開発環境セットアップガイド

このドキュメントは、別の PC で KS-FTP for Windows をビルド・開発する際の手順を記載しています。

## 必須要件

### 1. .NET SDK 8.0

KS-FTP for Windows は **ターゲットフレームワーク: net8.0-windows** で構築されています。

#### インストール方法

**Windows:**
```powershell
# Winget で最新の .NET 8.0 をインストール
winget install Microsoft.DotNet.SDK.8

# または、公式サイトからダウンロード
# https://dotnet.microsoft.com/download/dotnet/8.0
```

**バージョン確認:**
```powershell
dotnet --version
# 出力例: 8.0.xxx
```

### 2. Visual Studio 2022（推奨）

IDE での開発を行う場合は、Visual Studio 2022 以上を推奨します。

**必須ワークロード:**
- .NET desktop development
- Windows App SDK development

### 3. Git

ソースコード管理用に Git をインストール

```powershell
winget install Git.Git
```

## リポジトリのクローン

```powershell
git clone https://github.com/kazushinjo/ks-ftp-windows.git
cd ks-ftp-windows
```

## ビルド手順

### コマンドラインビルド

**Debug ビルド:**
```powershell
dotnet build -c Debug
```

**Release ビルド:**
```powershell
dotnet build -c Release
```

### Visual Studio でのビルド

1. `KsFtp.sln` をダブルクリック、または Visual Studio で開く
2. ビルド → ソリューションのビルド（Ctrl+Shift+B）

## ビルドの検証

ビルドが正常に完了する目安：

```
Build succeeded. 0 Warning(s), 0 Error(s)
```

**エラーが発生した場合の確認事項:**

1. **.NET SDK バージョンの確認**
   ```powershell
   dotnet --version
   # 8.0.xxx であることを確認
   ```

2. **NuGet パッケージの復元**
   ```powershell
   dotnet restore
   dotnet build -c Release
   ```

3. **ビルドキャッシュのクリア**
   ```powershell
   dotnet clean
   dotnet build -c Release
   ```

## プロジェクト構成

```
ks-ftp-windows/
├── KsFtp/
│   ├── KsFtp.csproj              # メインプロジェクト
│   ├── ViewModels/               # MVVM ビューモデル
│   ├── Views/                    # WPF ビュー
│   ├── Models/                   # データモデル
│   ├── Services/                 # ビジネスロジック
│   └── Resources/                # リソース
├── FtpServiceTest/               # テストプロジェクト
├── KsFtp.sln                     # ソリューションファイル
├── global.json                   # .NET SDK バージョン指定
├── .editorconfig                 # コードスタイル設定
├── .gitignore                    # Git除外設定
└── README.md                     # このリポジトリについて
```

## 依存パッケージ

KS-FTP for Windows が使用している NuGet パッケージ：

| パッケージ | バージョン | 用途 |
|-----------|-----------|------|
| CommunityToolkit.Mvvm | 8.4.2 | MVVM パターン実装 |
| FluentFTP | 54.2.0 | FTP / FTPS クライアント |
| SSH.NET | 2025.1.0 | SFTP (SSH) サポート |

これらは `KsFtp.csproj` に記載されており、`dotnet restore` で自動インストールされます。

## 開発時の推奨設定

### Visual Studio Code

`.vscode/settings.json` 例:

```json
{
  "editor.rulers": [120],
  "editor.formatOnSave": true,
  "[csharp]": {
    "editor.defaultFormatter": "ms-dotnettools.csharp"
  }
}
```

### Visual Studio 2022

ツール → オプション → テキストエディタ → C# → コードスタイル
- 規則の適用: IntelliSense/コード修正の提案に使用

## ビルド成果物

**Debug ビルド:**
```
KsFtp/bin/Debug/net8.0-windows/
```

**Release ビルド:**
```
KsFtp/bin/Release/net8.0-windows/
```

## トラブルシューティング

### エラー: "ターゲット フレームワーク 'net8.0-windows' が見つかりません"

**原因:** .NET SDK 8.0 がインストールされていない、または PATH が正しく設定されていない

**解決方法:**
```powershell
# インストール済みの .NET SDK を確認
dotnet --list-sdks

# .NET 8.0 が表示されない場合はインストール
winget install Microsoft.DotNet.SDK.8

# PowerShell を再起動して PATH を再読み込み
```

### エラー: "プロジェクト ファイルが見つかりません"

**原因:** リポジトリのクローンが不完全

**解決方法:**
```powershell
# リポジトリを削除してもう一度クローン
cd ..
Remove-Item ks-ftp-windows -Recurse
git clone https://github.com/kazushinjo/ks-ftp-windows.git
cd ks-ftp-windows
```

### ビルド警告が表示される

**警告例:** "The language version or compiler options of project 'X' are not compatible with..."

**対応:** これは通常は無視しても構いませんが、本番ビルド前に確認してください。

```powershell
dotnet build -c Release /p:TreatWarningsAsErrors=true
```

で全ての警告がエラー扱いされる状態で正常にビルドされることを確認します。

## CI/CD パイプライン

このプロジェクトは GitHub Actions で自動ビルド・テストが実行されます。

詳細は `.github/workflows/` を参照してください。

## よくある質問

**Q: Release ビルドと Debug ビルドの違いは？**

| 項目 | Debug | Release |
|------|-------|---------|
| 実行速度 | 遅い | 高速 |
| ファイルサイズ | 大きい | 小さい | 
| デバッグ情報 | あり | なし |
| 最適化 | なし | あり |

本番環境には **Release ビルド** を使用してください。

**Q: インストール後にエラーが出た**

まず以下を確認してください：

1. Windows 10 / 11 (64-bit) を使用しているか
2. .NET 8.0 Runtime がインストールされているか（SDK のみでなく）
3. 必要なランタイムをダウンロード: https://dotnet.microsoft.com/download/dotnet/8.0

**Q: 開発時にコード補完が効かない**

Visual Studio の C# プロジェクトのリロードを試してください：

1. ソリューション → プロジェクトを右クリック → アンロード
2. もう一度右クリック → リロード
3. ビルド → クリーン → ビルド

## サポート

問題が発生した場合は、以下を確認してください：

1. [GitHub Issues](https://github.com/kazushinjo/ks-ftp-windows/issues)
2. [README.md](README.md)

---

**最終更新:** 2026-06-23  
**KS-FTP for Windows Version:** 1.0
