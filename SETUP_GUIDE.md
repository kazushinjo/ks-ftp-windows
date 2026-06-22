# KS-FTP for Windows セットアップガイド

別の PC で KS-FTP for Windows をビルド・開発するためのガイドです。

---

## 🚀 クイックスタート（5分）

### ステップ 1: 環境確認

```powershell
dotnet --version
# 出力: 8.0.xxx （8.0 以上であることを確認）
```

### ステップ 2: リポジトリクローン

```powershell
git clone https://github.com/kazushinjo/ks-ftp-windows.git
cd ks-ftp-windows
```

### ステップ 3: セットアップ実行

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
.\scripts\setup.ps1
```

### ステップ 4: アプリ実行確認

```powershell
.\KsFtp\bin\Release\net8.0-windows\KsFtp.exe
```

✅ **完了！**

---

## 📚 ドキュメント

| ドキュメント | 説明 |
|-------------|------|
| [BUILD_GUIDE.md](BUILD_GUIDE.md) | ビルド手順の詳細 |
| [DEVELOPMENT.md](DEVELOPMENT.md) | 開発環境セットアップ |
| [CHECKLIST.md](CHECKLIST.md) | ビルド前後のチェックリスト |
| [README.md](README.md) | プロジェクト概要 |

---

## ⚙️ 最小要件

| 項目 | 要件 |
|------|------|
| OS | Windows 10 / 11 (64-bit) |
| .NET SDK | 8.0 以上 |
| ディスク | 500 MB 以上 |

---

## 🛠️ よく使うコマンド

```powershell
# Release ビルド
dotnet build -c Release

# Debug ビルド
dotnet build -c Debug

# キャッシュクリア
dotnet clean

# 環境検証
.\build-verify.ps1

# アプリ実行
.\KsFtp\bin\Release\net8.0-windows\KsFtp.exe
```

---

## 🐛 よくあるエラー

| エラー | 対処 |
|--------|------|
| "ターゲット フレームワーク 'net8.0-windows' が見つかりません" | .NET SDK 8.0 をインストール |
| "プロジェクト ファイルが見つかりません" | リポジトリを再クローン |

詳細は [BUILD_GUIDE.md](BUILD_GUIDE.md) を参照。

---

**作成:** 2026-06-23
**対応バージョン:** v1.0
