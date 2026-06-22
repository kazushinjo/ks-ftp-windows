#!/usr/bin/env pwsh
<#
.SYNOPSIS
KS-FTP for Windows 初期化セットアップスクリプト

.DESCRIPTION
リポジトリを開いたときに実行する自動セットアップ

.EXAMPLE
.\scripts\setup.ps1

.EXAMPLE
.\scripts\setup.ps1 -SkipBuild
#>

param(
    [switch]$SkipBuild = $false,
    [switch]$SkipVerify = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Continue"
$checks = @{
    Passed = 0
    Failed = 0
}

function Write-Step {
    param([int]$Number, [string]$Title)
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ ステップ $Number: $Title" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   KS-FTP for Windows セットアップスクリプト v1.0        ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green

# ステップ 1: 環境検証
if (-not $SkipVerify) {
    Write-Step 1 "環境検証"

    if (Test-Path ".\build-verify.ps1") {
        Write-Host "環境検証スクリプトを実行中..."
        & ".\build-verify.ps1"

        if ($LASTEXITCODE -ne 0) {
            Write-Error "環境検証に失敗しました"
            Write-Host ""
            Write-Host "詳細は BUILD_GUIDE.md を参照してください"
            exit 1
        }
        Write-Success "環境検証完了"
    } else {
        Write-Error "build-verify.ps1 が見つかりません"
        exit 1
    }
} else {
    Write-Step 1 "環境検証（スキップ）"
    Write-Host "--SkipVerify オプションで検証をスキップしました"
}

# ステップ 2: ビルド
if (-not $SkipBuild) {
    Write-Step 2 "Release ビルド実行"

    Write-Host "Release ビルドを開始します..."
    dotnet build -c Release

    if ($LASTEXITCODE -ne 0) {
        Write-Error "ビルドに失敗しました"
        Write-Host ""
        Write-Host "詳細は BUILD_GUIDE.md を参照してください"
        exit 1
    }
    Write-Success "ビルド完了"
} else {
    Write-Step 2 "Release ビルド実行（スキップ）"
    Write-Host "--SkipBuild オプションでビルドをスキップしました"
}

# ステップ 3: アプリ実行確認（オプション）
Write-Step 3 "アプリ実行確認（オプション）"

$response = Read-Host "アプリケーションを起動して確認しますか？ (y/n)"

if ($response -eq "y" -or $response -eq "Y") {
    $exePath = ".\KsFtp\bin\Release\net8.0-windows\KsFtp.exe"

    if (Test-Path $exePath) {
        Write-Host "アプリケーションを起動中..."
        & $exePath

        Write-Success "アプリケーション起動確認完了"
    } else {
        Write-Error "実行ファイルが見つかりません: $exePath"
    }
} else {
    Write-Host "アプリ起動をスキップしました"
}

# ステップ 4: 完了メッセージ
Write-Step 4 "セットアップ完了"

Write-Success "KS-FTP for Windows セットアップが完了しました！"
Write-Host ""
Write-Host "次のステップ：" -ForegroundColor Yellow
Write-Host "  1. コード修正を実施"
Write-Host "  2. テストを実行（必要に応じて）"
Write-Host "  3. コミット & プッシュ"
Write-Host ""
Write-Host "参考資料：" -ForegroundColor Yellow
Write-Host "  - README.md: プロジェクト概要"
Write-Host "  - SETUP_GUIDE.md: セットアップガイド"
Write-Host "  - BUILD_GUIDE.md: ビルド詳細 & トラブルシューティング"
Write-Host ""
Write-Host "開発を開始してください！🚀" -ForegroundColor Green
