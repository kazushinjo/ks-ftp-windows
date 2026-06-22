#!/usr/bin/env pwsh
<#
.SYNOPSIS
KS-FTP for Windows ビルド環境検証スクリプト

.DESCRIPTION
.NET 8.0 開発環境をチェック

.EXAMPLE
./build-verify.ps1
#>

param(
    [switch]$ShowDetails = $false
)

$ErrorActionPreference = "Continue"
$checks = @{
    Passed = 0
    Failed = 0
    Warnings = 0
}

function Test-Check {
    param([string]$Name, [scriptblock]$Test)

    Write-Host "確認中: $Name ... " -NoNewline

    try {
        $result = & $Test
        if ($result) {
            Write-Host "OK" -ForegroundColor Green
            $checks.Passed++
            return $true
        } else {
            Write-Host "失敗" -ForegroundColor Red
            $checks.Failed++
            return $false
        }
    } catch {
        Write-Host "エラー" -ForegroundColor Red
        if ($ShowDetails) {
            Write-Host "  詳細: $_"
        }
        $checks.Failed++
        return $false
    }
}

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   KS-FTP for Windows ビルド環境検証ツール                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# 1. PowerShell バージョン確認
Write-Host "[1] システム要件" -ForegroundColor Yellow
Test-Check "PowerShell 5.1以上" {
    $PSVersionTable.PSVersion.Major -ge 5 -and $PSVersionTable.PSVersion.Minor -ge 1
}

# 2. Windows バージョン確認
Test-Check "Windows 10 / 11 (64-bit)" {
    [System.Environment]::Is64BitOperatingSystem
}

# 3. .NET SDK 確認
Write-Host ""
Write-Host "[2] .NET SDK" -ForegroundColor Yellow

Test-Check ".NET SDK がインストール済み" {
    $null -ne (dotnet --version 2>$null)
}

$dotnetVersion = dotnet --version 2>$null
if ($dotnetVersion) {
    $majorVersion = [int]($dotnetVersion.Split('.')[0])
    Test-Check ".NET 8.0 以上がインストール済み" {
        $majorVersion -ge 8
    }
    Write-Host "  インストール済みバージョン: $dotnetVersion"
}

# 4. .NET Runtime 確認
Write-Host ""
Write-Host "[3] .NET Runtime" -ForegroundColor Yellow

Test-Check ".NET Runtime (Windows Desktop) がインストール済み" {
    $runtimes = dotnet --list-runtimes 2>$null
    $runtimes -match "Microsoft\.WindowsDesktop\.App"
}

$desktopRuntime = (dotnet --list-runtimes 2>$null | Where-Object { $_ -match "Microsoft\.WindowsDesktop\.App" } | Select-Object -First 1)
if ($desktopRuntime) {
    Write-Host "  インストール済み: $desktopRuntime"
}

# 5. 必須ファイル確認
Write-Host ""
Write-Host "[4] プロジェクトファイル" -ForegroundColor Yellow

$projectFiles = @(
    "KsFtp.sln",
    "KsFtp\KsFtp.csproj",
    "global.json",
    ".editorconfig"
)

foreach ($file in $projectFiles) {
    $fullPath = Join-Path (Get-Location) $file
    Test-Check "存在: $file" {
        Test-Path $fullPath
    }
}

# 6. ディスク容量確認
Write-Host ""
Write-Host "[5] ディスク容量" -ForegroundColor Yellow

$drive = Get-PSDrive -Name (Get-Location).Drive.Name
$freeSpaceGB = [math]::Round($drive.Free / 1GB, 1)
$requiredSpaceGB = 1

if ($freeSpaceGB -gt $requiredSpaceGB) {
    Write-Host "  利用可能: $freeSpaceGB GB (要件: ${requiredSpaceGB}GB以上) ✓" -ForegroundColor Green
} else {
    Write-Host "  警告: ディスク容量が不足しています (利用可能: $freeSpaceGB GB)" -ForegroundColor Yellow
    $checks.Warnings++
}

# 7. Git 確認（オプション）
Write-Host ""
Write-Host "[6] Git（オプション）" -ForegroundColor Yellow

Test-Check "Git がインストール済み" {
    $null -ne (git --version 2>$null)
}

if (Test-Path .git) {
    Write-Host "  リポジトリ状態: Git リポジトリ" -ForegroundColor Green
}

# サマリー表示
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   検証結果                                                 ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "✓ 成功: $($checks.Passed)" -ForegroundColor Green
Write-Host "✗ 失敗: $($checks.Failed)" -ForegroundColor Red
Write-Host "⚠ 警告: $($checks.Warnings)" -ForegroundColor Yellow

Write-Host ""
if ($checks.Failed -eq 0) {
    Write-Host "✓ すべての確認に合格しました。ビルド可能です。" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ 確認に失敗しました。上記のエラーを修正してください。" -ForegroundColor Red
    Write-Host ""
    Write-Host "参考: DEVELOPMENT.md を確認してください"
    exit 1
}
