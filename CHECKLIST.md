# KS-FTP for Windows マルチ PC ビルド チェックリスト

別の PC でビルドする前に確認してください。

## 環境セットアップ確認

### PC 側の準備

- [ ] Windows 10 / 11 (64-bit)
- [ ] ディスク容量 500 MB 以上
- [ ] インターネット接続

### システムソフトウェア

- [ ] .NET SDK 8.0 以上がインストール済み
- [ ] .NET 8.0 Runtime (Windows Desktop) がインストール済み
- [ ] Git がインストール済み

### インストール確認

```powershell
dotnet --version
# 出力: 8.0.xxx であることを確認

dotnet --list-runtimes
# 出力に "Microsoft.WindowsDesktop.App 8.0.x" があることを確認
```

---

## リポジトリ準備

```powershell
git clone https://github.com/kazushinjo/ks-ftp-windows.git
cd ks-ftp-windows
```

### 環境検証

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
.\build-verify.ps1
```

**確認:** 全て "✓ OK" になることを確認

---

## ビルド実行チェック

### コマンドラインビルド

```powershell
.\scripts\setup.ps1
```

**確認項目:**
- [ ] エラーなく完了
- [ ] "Build succeeded" が表示される
- [ ] 警告: 0、エラー: 0

### IDE ビルド（Visual Studio 2022）

1. [ ] `KsFtp.sln` をダブルクリックで開く
2. [ ] ビルド → ソリューションのビルド（Ctrl+Shift+B）
3. [ ] "ビルドに成功しました。警告: 0、エラー: 0" が表示される

---

## ビルド成功確認

### 成果物確認

```powershell
ls KsFtp\bin\Release\net8.0-windows\
```

**確認項目:**
- [ ] `KsFtp.exe` が存在する
- [ ] `FluentFTP.dll` が存在する
- [ ] `SSH.NET.dll` が存在する

### アプリ実行確認

```powershell
.\KsFtp\bin\Release\net8.0-windows\KsFtp.exe
```

**確認項目:**
- [ ] アプリが起動する
- [ ] 接続先ウィンドウが表示される
- [ ] エラーダイアログがない

---

## マルチ PC ビルド検証

### PC A
- [ ] ビルド成功
- [ ] exe 実行確認
- [ ] 成果物をメモ

### PC B
- [ ] build-verify.ps1 で全確認が成功
- [ ] ビルド成功
- [ ] exe 実行確認

---

## よくあるエラーと対処

### エラー 1: ".NET SDK 8.0 が見つかりません"

```powershell
winget install Microsoft.DotNet.SDK.8
```

### エラー 2: "プロジェクト ファイルが見つかりません"

```powershell
cd ..
Remove-Item ks-ftp-windows -Recurse
git clone https://github.com/kazushinjo/ks-ftp-windows.git
cd ks-ftp-windows
```

### エラー 3: NuGet エラー

```powershell
dotnet clean
dotnet restore
dotnet build -c Release
```

詳細は [BUILD_GUIDE.md](BUILD_GUIDE.md) を参照。

---

**最終更新:** 2026-06-23
**対応バージョン:** v1.0
