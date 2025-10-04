# ZapEditor

F#とAvalonia UIで構築されたクロスプラットフォーム対応のコードエディタです。高速起動、TextMateベースのシンタックスハイライト、そして主要なスクリプト言語向けの軽量なコード実行機能を提供します。

[English](./docs/README.en.md) | 日本語 | [中文](./docs/README.zh.md)

## 目次

1. [主な機能](#主な機能)
2. [スクリーンショット](#スクリーンショット)
3. [クイックスタート](#クイックスタート)
4. [設定方法](#設定方法)
5. [プロジェクト構成](#プロジェクト構成)
6. [多言語対応](#多言語対応)
7. [開発ワークフロー](#開発ワークフロー)
8. [貢献方法](#貢献方法)
9. [ライセンス](#ライセンス)

## 主な機能

- 🎨 **TextMateシンタックスハイライト** - `Avalonia.AvaloniaEdit` 11.0.5 + TextMate文法による美しいコードハイライト
- 📝 **縦書き/横書き切替** - メニューまたはツールバーから瞬時に表示モードを切替可能
- 🌐 **多言語UI対応** - 日本語/英語/中国語のリアルタイム切替に対応
- 🗂️ **ファイル操作** - Avalonia storage providerを使用した安全なファイルの開く・保存機能
- ▶️ **コード実行機能** - F#、C#、Python、JavaScriptのインライン実行をサポート
- 💻 **デスクトップ最適化** - Avalonia 11.3.6によるWindows、macOS、Linux対応

## スクリーンショット

_準備中 — コントリビューション歓迎！_

## クイックスタート

### 必要な環境

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- コード実行機能を使用する場合（オプション）：
  - Python 3.x（`python3`がPATHに含まれていること）
  - Node.js 18+（`node`がPATHに含まれていること）

### クローン＆実行

```bash
git clone https://github.com/SilentMalachite/ZapEditor.git
cd ZapEditor

# パッケージの復元（オフライン環境でも動作）
dotnet restore --ignore-failed-sources

# ビルドしてアプリケーションを起動
dotnet run
```

> ℹ️ ネットワーク制限により脆弱性データの取得に失敗する場合は `--ignore-failed-sources` オプションを使用してください。ビルド自体には影響しません。

### バイナリのダウンロード

最新のリリース版は[Releases](https://github.com/SilentMalachite/ZapEditor/releases)ページからダウンロードできます。

- **Windows**: `ZapEditor-v1.2.0-windows-x64.zip`
- **macOS (Apple Silicon)**: `ZapEditor-v1.2.0-macos-arm64.zip`
- **macOS (Intel)**: `ZapEditor-v1.2.0-macos-x64.zip`

ZIPファイルを解凍して実行するだけです。.NETランタイムのインストールは不要です（自己完結型バイナリ）。

### パッケージング

```bash
# 自己完結型ビルドの作成（Windows 64bit版）
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# macOS ARM64版
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true

# macOS Intel版
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

ランタイム識別子（`-r`）を変更することで、他のプラットフォーム（`linux-x64`など）向けにもビルドできます。

## 設定方法

| 項目 | カスタマイズ方法 |
| ---- | ---------------- |
| デフォルト言語 | `Resources/Strings.*.resx`を更新するか、起動時に`ResourceManager.SetLanguage`を呼び出す |
| シンタックステーマ | `Controls/SyntaxHighlightEditor.axaml.fs`の`ThemeName.DarkPlus`を他の`ThemeName`に変更 |
| サポート文法 | `SyntaxHighlightEditor`の`SetLanguage`マッピングと`MainWindowViewModel`の`DetectLanguage`を拡張 |
| コード実行バックエンド | `Services/CodeExecutionService.fs`に追加のヘルパーを実装 |
| 表示モード | メニューの「表示」→「縦書き/横書き切替」、またはツールバーのボタンで切替 |

## プロジェクト構成

```
ZapEditor/
├── App.axaml(.fs)              # アプリケーションシェルとライフサイクル
├── Controls/
│   ├── SyntaxHighlightEditor   # TextMate対応カスタムエディタコントロール
│   └── WritingModeConverter.fs # 縦書き/横書き表示切替用コンバーター
├── Resources/                  # 多言語対応リソース文字列
├── Services/
│   ├── CodeExecutionService.fs # 外部プロセス実行サービス
│   ├── IEditorService.fs       # エディタサービスインターフェース
│   └── ResourceManager.fs      # カルチャ対応リソースアクセサ
├── ViewModels/
│   └── MainWindowViewModel.fs  # MVVMロジックとコマンドバインディング
├── MainWindow.axaml(.fs)       # メインウィンドウビュー + コードビハインド
├── Tests/                      # ユニットテストプロジェクト
└── Program.fs                  # エントリーポイント
```

## 多言語対応

UI文字列は`Resources/Strings.<culture>.resx`に配置されています。新しい言語を追加するには、ニュートラルリソースファイルを複製し、文字列を翻訳して、`ResourceManager.SetLanguage`でカルチャコードを設定します。

現在のUIカルチャは、`MainWindowViewModel.SetLanguage`から実行時に切り替えられ、メニュー、ステータステキスト、ダイアログがすぐに更新されます。

### サポート言語

- 🇯🇵 日本語（ja）
- 🇬🇧 英語（en）
- 🇨🇳 中国語（zh）

## 開発ワークフロー

```bash
# ビルド
dotnet build

# 中間成果物のクリーンアップ
dotnet clean

# ウォッチモードでエディタを実行（ホットリロード）
dotnet watch run

# テストの実行
dotnet test

# 継続的ビルド
dotnet build --no-incremental
```

TextMate文法を作業する場合は、Avalonia診断機能を有効にして（`dotnet run -c Debug`）、アプリ内でコントロールツリーを検査できます。

## 最近の更新

### v1.2.0 - 縦書き/横書き切替機能の追加

**新機能**
- ✨ **縦書き/横書き表示モード切替機能**を実装
  - メニューバー「表示」→「縦書き/横書き切替」から切替
  - ツールバーに専用の切替ボタンを追加
  - リアルタイムでテキストの表示方向を変更
- 🧪 **ユニットテストプロジェクト**を追加（7テスト）
- 🔄 **CI/CDワークフロー**にテストステップを追加

**バグ修正**
- 🐛 ビルドエラーの修正（AttachDevTools呼び出しを削除）
- 🔧 コンパイル警告の解消

**技術的変更**
- `WritingModeConverter.fs`: 縦書き/横書き表示用データバインディングコンバーター
- `IEditorService.fs`: `IsVerticalWritingMode`プロパティを追加
- `SyntaxHighlightEditor.axaml.fs`: RenderTransformによる90度回転を実装
- `MainWindowViewModel.fs`: `ToggleWritingModeCommand`を追加

### v1.1.0 - UIの改善とバグ修正

**修正内容**
- 🔧 **言語セレクタの修正**: ComboBoxが正しくViewModelにバインドされるように修正
- 📁 **ファイル操作の強化**: 包括的なエラーハンドリングを備えた完全なファイル開く機能
- ⚠️ **ビルド警告の解決**: F#コンパイラ警告とNuGet監査警告を解消
- 🛡️ **エラーハンドリングの改善**: ファイルアクセス操作に適切な例外処理を追加
- 🎨 **コード品質**: 未使用の再帰オブジェクト参照を削除し、リソースファイルをクリーンアップ

**技術的変更**
- MainWindowViewModel: FS1183警告を解消するため`as this`パラメータを削除
- MainWindow.axaml: ComboBoxバインディングを`ItemsSource`と`SelectedItem`を使用するように更新
- ZapEditor.fsproj: `NuGetAudit=false`と`NuGetAuditMode=direct`設定を追加
- File Service: UnauthorizedAccessException、PathTooLongException等の例外処理を強化
- Resource Management: 日本語リソースファイルのフォーマットをクリーンアップ

## 貢献方法

**🎉 コントリビューター大募集中！**

ZapEditorは、あなたの貢献を心から歓迎します。初心者から上級者まで、誰でも参加できるタスクがあります！

### 貢献の始め方

1. 📖 [CONTRIBUTING.md](CONTRIBUTING.md)で貢献ガイドラインを確認
2. 🎯 [CONTRIBUTORS_WANTED.md](CONTRIBUTORS_WANTED.md)で募集中のタスクを確認
3. 💬 [Issues](https://github.com/SilentMalachite/ZapEditor/issues)で作業するタスクを見つける
4. 🚀 Pull Requestを送信

### 特に募集中

- 🧪 **テストの追加**: テストカバレッジの向上
- 🔍 **検索・置換機能**: 基本的な検索機能の実装
- 📑 **タブ機能**: 複数ファイルの同時編集
- 🎨 **テーマシステム**: カスタムテーマのサポート
- 📝 **ドキュメント**: 翻訳や改善

小さな貢献でも大歓迎です！[初めての方向けガイド](CONTRIBUTORS_WANTED.md#初めての貢献ガイド)もご用意しています。

## ライセンス

[Apache License 2.0](LICENSE)でライセンスされています。

## サポート

- 🐛 **バグ報告**: [Issues](https://github.com/SilentMalachite/ZapEditor/issues)ページで報告してください
- 💡 **機能リクエスト**: 新機能のアイデアがあれば、Issueで提案してください
- 📖 **ドキュメント**: ドキュメントの改善提案も歓迎します
- 🌟 **スター**: プロジェクトが気に入ったら、GitHubでスターをお願いします！

## ロードマップ

今後の予定：
- [ ] プラグインシステムの実装
- [ ] より多くの言語のシンタックスハイライト対応
- [ ] テーマのカスタマイズ機能
- [ ] 検索・置換機能の強化
- [ ] Git統合機能
- [ ] コード補完機能

ご意見・ご要望があれば、[Discussions](https://github.com/SilentMalachite/ZapEditor/discussions)でお聞かせください。
