# ZapEditor インストールガイド

このガイドでは、ZapEditorのインストール方法を説明します。

## 目次

- [ビルド済みバイナリを使用](#ビルド済みバイナリを使用)
- [ソースからビルド](#ソースからビルド)
- [トラブルシューティング](#トラブルシューティング)

## ビルド済みバイナリを使用

最も簡単な方法は、[Releases](https://github.com/SilentMalachite/ZapEditor/releases)ページから最新版をダウンロードすることです。

### Windows

1. `ZapEditor-v1.2.0-windows-x64.zip`をダウンロード
2. ZIPファイルを任意の場所に解凍
3. `ZapEditor.exe`をダブルクリックして起動

**注意事項**:
- .NETランタイムのインストールは不要です（自己完結型）
- Windows Defenderで警告が表示される場合は、「詳細情報」→「実行」をクリック

### macOS

#### Apple Silicon (M1/M2/M3)

1. `ZapEditor-v1.2.0-macos-arm64.zip`をダウンロード
2. ZIPファイルを解凍
3. `ZapEditor`を実行

#### Intel Mac

1. `ZapEditor-v1.2.0-macos-x64.zip`をダウンロード
2. ZIPファイルを解凍
3. `ZapEditor`を実行

**初回起動時の注意事項**:

macOSで初めて起動する際、「開発元が未確認のため開けません」という警告が表示される場合があります。

**解決方法**:
1. アプリケーションを右クリック（またはControlキーを押しながらクリック）
2. 「開く」を選択
3. 警告ダイアログで「開く」をクリック

または、ターミナルで以下のコマンドを実行：
```bash
xattr -d com.apple.quarantine /path/to/ZapEditor
```

### Linux

現在、Linux向けのビルド済みバイナリは提供していません。[ソースからビルド](#ソースからビルド)してください。

## ソースからビルド

### 前提条件

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)以上
- Git

### 手順

1. **リポジトリのクローン**

```bash
git clone https://github.com/SilentMalachite/ZapEditor.git
cd ZapEditor
```

2. **依存関係の復元**

```bash
dotnet restore
```

ネットワーク制限がある場合：
```bash
dotnet restore --ignore-failed-sources
```

3. **ビルド**

**開発用ビルド**:
```bash
dotnet build
```

**リリースビルド**:
```bash
dotnet build -c Release
```

4. **実行**

```bash
dotnet run
```

または、ビルド済みの実行可能ファイルを直接実行：
```bash
# Windows
./bin/Debug/net9.0/ZapEditor.exe

# macOS/Linux
./bin/Debug/net9.0/ZapEditor
```

### 自己完結型バイナリの作成

配布可能な単一ファイルの実行可能ファイルを作成：

**Windows x64**:
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish/win-x64
```

**macOS ARM64 (Apple Silicon)**:
```bash
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o ./publish/osx-arm64
```

**macOS x64 (Intel)**:
```bash
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -o ./publish/osx-x64
```

**Linux x64**:
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o ./publish/linux-x64
```

ビルドされたバイナリは`./publish/<platform>/`ディレクトリに作成されます。

## オプション機能のセットアップ

### コード実行機能

ZapEditorでコード実行機能を使用する場合、以下の言語ランタイムをインストールする必要があります：

#### Python

```bash
# Windows (Chocolatey)
choco install python

# macOS (Homebrew)
brew install python3

# Linux (Ubuntu/Debian)
sudo apt install python3
```

インストール後、コマンドラインで確認：
```bash
python3 --version
```

#### Node.js

```bash
# Windows (Chocolatey)
choco install nodejs

# macOS (Homebrew)
brew install node

# Linux (Ubuntu/Debian)
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt install -y nodejs
```

インストール後、コマンドラインで確認：
```bash
node --version
```

## アンインストール

### ビルド済みバイナリ版

解凍したフォルダを削除するだけです。設定ファイルは作成されません。

### ソースからビルドした場合

1. クローンしたリポジトリのディレクトリを削除
2. （オプション）NuGetキャッシュをクリア：
```bash
dotnet nuget locals all --clear
```

## トラブルシューティング

### Windows: 「WindowsによってPCが保護されました」

1. 「詳細情報」をクリック
2. 「実行」をクリック

### macOS: 「開発元が未確認」エラー

上記の[macOSセクション](#macos)の手順を参照してください。

### Linux: 「Permission denied」エラー

実行権限を付与：
```bash
chmod +x ZapEditor
```

### ビルドエラー: 「The framework 'Microsoft.NETCore.App', version '9.0.0' was not found」

.NET SDK 9.0がインストールされていません。[.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)をダウンロードしてインストールしてください。

### 実行時エラー: 「Unable to load shared library 'libSkiaSharp'」

Linuxの場合、追加のライブラリが必要な場合があります：

```bash
# Ubuntu/Debian
sudo apt install libfontconfig1 libfreetype6 libx11-6

# Fedora/CentOS
sudo dnf install fontconfig freetype libX11
```

### その他の問題

問題が解決しない場合は、[GitHub Issues](https://github.com/SilentMalachite/ZapEditor/issues)で報告してください。

## 次のステップ

- [README.md](README.md)でZapEditorの機能を確認
- [CONTRIBUTING.md](CONTRIBUTING.md)で開発に参加する方法を確認
- [リリースノート](https://github.com/SilentMalachite/ZapEditor/releases)で最新の変更を確認
