# ZapEditor ナレッジベース

## プロジェクト概要

ZapEditorは、F#とAvalonia UIで構築されたクロスプラットフォーム対応のコードエディタです。シンタックスハイライト、ファイル操作、コード実行機能を備えています。

## 技術スタック

- **フレームワーク**: .NET 9.0
- **UIフレームワーク**: Avalonia 11.3.6
- **言語**: F#
- **エディタコンポーネント**: AvaloniaEdit + TextMateシンタックスハイライト
- **テストフレームワーク**: NUnit 3.14.0

## プロジェクト構造

```
ZapEditor/
├── Program.fs                      # アプリケーションエントリーポイント
├── App.axaml/.fs                   # メインアプリケーション設定
├── MainWindow.axaml/.fs            # メインウィンドウ実装
├── ViewModels/
│   └── MainWindowViewModel.fs      # MVVMビューモデル
├── Services/
│   ├── IEditorService.fs           # エディタサービスインターフェース
│   ├── IFileService.fs             # ファイルサービスインターフェース
│   ├── ResourceManager.fs          # リソース管理
│   ├── FileService.fs              # ファイル操作実装
│   └── CodeExecutionService.fs     # コード実行サービス
├── Controls/
│   ├── SyntaxHighlightEditor.axaml.fs  # カスタムエディタコントロール
│   └── WritingModeConverter.fs         # 縦書き/横書きコンバーター
├── Resources/
│   ├── Strings.ja.resx             # 日本語リソース
│   ├── Strings.en.resx             # 英語リソース
│   └── Strings.zh.resx             # 中国語リソース
└── Tests/
    ├── ZapEditor.Tests.fsproj      # テストプロジェクト
    └── ZapEditor.Tests.fs          # ユニットテスト
```

## 主要機能

### コアエディタ機能
- AvaloniaEditによるシンタックスハイライト
- TextMate文法サポート
- 行番号表示
- タブのスペース変換
- ハイパーリンクサポート
- テキストドラッグ&ドロップ

### 多言語対応
- 日本語（ja）
- 英語（en）
- 中国語（zh）
- 実行時の言語切替
- リソースベースのローカライゼーション

### ファイル操作
- ファイルを開く/保存ダイアログ
- エラーハンドリング機能
  - UnauthorizedAccessException
  - PathTooLongException
  - DirectoryNotFoundException
  - SecurityException
  - IOException（ファイル使用中の検出）

### コード実行サービス
- F#
- C#
- Python
- JavaScript
- 外部プロセス実行による実装

### 表示モード機能
- **縦書き/横書き切替**（v1.2.0で追加）
- メニューからの切替
- ツールバーボタンからの切替
- RenderTransformによる90度回転実装
- リアルタイム切替対応

## 開発コマンド

```bash
# ビルド
dotnet build

# 実行
dotnet run

# クリーン
dotnet clean

# パッケージ復元
dotnet restore

# テスト実行
dotnet test

# ウォッチモード（ホットリロード）
dotnet watch run

# リリースビルド
dotnet build -c Release

# パブリッシュ（自己完結型）
dotnet publish -c Release -r <RID> --self-contained -p:PublishSingleFile=true
```

### ランタイム識別子（RID）
- Windows x64: `win-x64`
- macOS ARM64: `osx-arm64`
- macOS x64: `osx-x64`
- Linux x64: `linux-x64`

## アーキテクチャパターン

### MVVM（Model-View-ViewModel）
- **View**: XAML（.axaml）ファイル
- **ViewModel**: MainWindowViewModel.fs
- **Model**: Services層

### 依存性注入
- コンストラクタインジェクション
- インターフェースベースの設計
- テスト可能な構造

### データバインディング
- コンパイル済みバインディング（`AvaloniaUseCompiledBindingsByDefault=true`）
- IValueConverterの使用（WritingModeConverter）
- INotifyPropertyChangedの実装

## バージョン履歴と修正内容

### v1.2.0（2024-10） - 縦書き/横書き切替機能の追加

**新機能**
- ✨ 縦書き/横書き表示モード切替機能を実装
  - `WritingModeConverter.fs`: データバインディング用コンバーター
  - `IEditorService`: `IsVerticalWritingMode`プロパティを追加
  - `SyntaxHighlightEditor`: RenderTransformによる表示変換
  - `MainWindowViewModel`: `ToggleWritingModeCommand`を実装
- 🧪 ユニットテストプロジェクトの追加（7テスト）
- 🔄 CI/CDワークフローにテストステップを追加

**バグ修正**
- 🐛 MainWindow.axaml.fsのビルドエラーを修正
  - `AttachDevTools()`呼び出しを削除
  - `Avalonia.Diagnostics`のimportを削除
  - コンパイルエラー（FS0039）を解消

**技術的変更**
- ZapEditor.fsproj: `WritingModeConverter.fs`をコンパイルリストに追加
- MainWindow.axaml: 縦書き/横書き切替UIを追加
  - メニュー項目
  - ツールバーボタン
  - WritingModeConverterリソース
- リソースファイル: WritingMode_Vertical/Horizontal文字列を追加

### v1.1.0 - UIの改善とバグ修正

**ビルドシステム**
- NuGet監査警告の解決
  - `<NuGetAudit>false</NuGetAudit>`を追加
  - `<NuGetAuditMode>direct</NuGetAuditMode>`を追加
- F#コンパイラ警告の解消
  - MainWindowViewModelから未使用の`as this`パラメータを削除

**UIバインディングの修正**
- 言語セレクタの修正
  - ViewModelに`AvailableLanguages`プロパティを実装
  - MainWindow.axamlでComboBoxバインディングを更新
  - `ItemsSource`と`SelectedItem`を使用
- 言語変更処理
  - `OnLanguageChanged`メソッドを追加
  - 手動言語選択に対応

**ファイル操作**
- ファイル読み込み機能の完全実装
- 包括的な例外処理の追加
  - UnauthorizedAccessException
  - PathTooLongException
  - DirectoryNotFoundException
  - SecurityException
  - IOException（ファイル使用中の検出）

**コード品質**
- 日本語リソースファイルのフォーマットをクリーンアップ
- 重複エントリの削除
- エラーメッセージの改善

## 技術的な詳細

### 縦書き/横書き表示モード

**実装アプローチ**
- AvaloniaEditはネイティブで縦書きをサポートしていないため、RenderTransformを使用
- 90度の回転変換を適用して縦書きを実現
- RenderTransformOriginを中心点（0.5, 0.5）に設定

**コンポーネント**
1. **WritingModeConverter**
   - IValueConverterの実装
   - bool値を表示文字列に変換
   - リソース文字列を使用した多言語対応

2. **IEditorService**
   - `IsVerticalWritingMode: bool with get, set`プロパティ
   - エディタサービスインターフェースに追加

3. **SyntaxHighlightEditor**
   - `ApplyWritingMode()`メソッド
   - RenderTransformの適用/解除
   - TextEditorインスタンスへの変換適用

4. **MainWindowViewModel**
   - `ToggleWritingModeCommand`コマンド
   - `IsVerticalWritingMode`プロパティ
   - PropertyChangedイベントの発火

**制限事項**
- 回転変換による実装のため、スクロールバーも回転する
- 完全な縦書きレイアウトではなく、横書きテキストを回転させた表示
- 複雑な縦書き固有の機能（縦中横など）は未サポート

### ビルド設定

**プロジェクトファイル（ZapEditor.fsproj）**
- `OutputType`: WinExe（Windows実行可能ファイル）
- `TargetFramework`: net9.0
- `BuiltInComInteropSupport`: true
- `AvaloniaUseCompiledBindingsByDefault`: true（パフォーマンス向上）
- `NuGetAudit`: false（ネットワーク非依存ビルド）

**コンパイル順序**
F#では依存関係に基づいた厳密なコンパイル順序が必要：
1. Services（インターフェース → 実装）
2. Controls
3. ViewModels
4. Views（MainWindow、App）
5. Program

### テスト

**テストプロジェクト**: Tests/ZapEditor.Tests.fsproj
- NUnit 3.14.0
- NUnit3TestAdapter 4.2.0
- 現在7つのテストを実装
- ResourceManager、WritingModeConverter等の単体テスト

**テスト実行**
```bash
# すべてのテストを実行
dotnet test

# 特定のプロジェクトのみ
dotnet test Tests/ZapEditor.Tests.fsproj

# 詳細な出力
dotnet test --verbosity normal
```

### CI/CD

**ワークフロー**: .github/workflows/ci.yml
- .NET SDK 9.xを使用
- Ubuntu最新版で実行
- ステップ：
  1. リポジトリのチェックアウト
  2. .NET SDKのセットアップ
  3. パッケージ復元
  4. ビルド（Release構成）
  5. テスト実行
  6. 脆弱性監査

## 既知の問題

### 解決済み
- ✅ FS1183警告（MainWindowViewModelの`as this`）
- ✅ NuGet監査警告
- ✅ 言語セレクタのバインディング
- ✅ AttachDevTools()のビルドエラー

### 未解決
- ⚠️ 縦書き表示時のスクロールバーの向き
- ⚠️ 完全な縦書きレイアウトエンジンの欠如

## 注意事項

### 開発環境
- デバッグビルドではAvalonia DevToolsを使用可能（F12キーで開く）
- ただし、現在AttachDevTools()は削除されているため、手動での有効化が必要

### ネットワーク制限
- `--ignore-failed-sources`を使用することで、NuGet脆弱性データベースへのアクセスなしでビルド可能
- プロジェクトファイルで監査を無効化済み

### プラットフォーム固有
- macOSで初回起動時に「開発元が未確認」警告が表示される場合あり
- Windows: 管理者権限は不要
- Linux: .NET 9.0ランタイムが必要（自己完結型ビルドの場合は不要）