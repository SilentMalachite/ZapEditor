# 変更履歴

ZapEditorの変更履歴を記録します。

このプロジェクトは[セマンティックバージョニング](https://semver.org/lang/ja/)に従っています。

## [1.2.0] - 2024-10-05

### 追加

- ✨ **縦書き/横書き表示モード切替機能**
  - メニューバー「表示」→「縦書き/横書き切替」から切替可能
  - ツールバーに専用の切替ボタンを追加
  - リアルタイムでテキストの表示方向を変更
  - 多言語対応（日本語、英語、中国語）
- 🧪 **ユニットテストプロジェクト**を追加
  - NUnit 3.14.0ベースのテストフレームワーク
  - 7つのテストケースを実装
  - ResourceManager、WritingModeConverterのテスト
- 🔄 **CI/CDワークフロー**にテストステップを追加
  - 自動テスト実行機能
  - ビルドプロセスの品質保証

### 変更

- 📝 多言語リソースファイルに縦書き/横書き関連の文字列を追加
  - `WritingMode_Vertical`: 縦書き/Vertical/竖排
  - `WritingMode_Horizontal`: 横書き/Horizontal/横排

### 修正

- 🐛 **ビルドエラーの修正**
  - MainWindow.axaml.fsの`AttachDevTools()`呼び出しを削除
  - Avalonia.Diagnosticsのimport問題を解決
  - F#コンパイラエラー（FS0039）を解消

### 技術的詳細

- `WritingModeConverter.fs`: IValueConverterを実装した新しいコンバーター
- `IEditorService.fs`: `IsVerticalWritingMode`プロパティを追加
- `SyntaxHighlightEditor.axaml.fs`: `ApplyWritingMode()`メソッドを実装
- `MainWindowViewModel.fs`: `ToggleWritingModeCommand`コマンドを追加
- `MainWindow.axaml`: UIコントロールと ResourceDictionary を更新

## [1.1.0] - 2024年初期

### 追加

- 📁 完全なファイル開く機能の実装
- 🌐 言語切替機能の改善
- 🗂️ AvaloniaLanguages プロパティの実装

### 修正

- 🔧 **言語セレクタのバインディング修正**
  - ComboBoxが正しくViewModelにバインドされるように修正
  - `ItemsSource`と`SelectedItem`を使用したバインディング
  - `OnLanguageChanged`メソッドの追加
- ⚠️ **ビルド警告の解決**
  - F#コンパイラ警告（FS1183）の解消
  - MainWindowViewModelから`as this`パラメータを削除
  - NuGet監査警告の解消
- 🛡️ **エラーハンドリングの強化**
  - UnauthorizedAccessException
  - PathTooLongException
  - DirectoryNotFoundException
  - SecurityException
  - IOException（ファイル使用中の検出）

### 変更

- 📦 プロジェクトファイルの更新
  - `<NuGetAudit>false</NuGetAudit>`を追加
  - `<NuGetAuditMode>direct</NuGetAuditMode>`を追加
- 🎨 日本語リソースファイルのフォーマットをクリーンアップ
- 📝 重複エントリの削除

## [1.0.0] - 初期リリース

### 追加

- 🎨 TextMateシンタックスハイライト機能
  - AvaloniaEdit 11.0.5ベース
  - 複数の言語サポート（F#、C#、Python、JavaScript等）
- 🌐 多言語UI対応
  - 日本語、英語、中国語
  - 実行時の言語切替
- 🗂️ ファイル操作機能
  - ファイルを開く
  - ファイルを保存
  - 名前を付けて保存
- ▶️ コード実行機能
  - F#スクリプト実行
  - C#コード実行
  - Pythonスクリプト実行
  - JavaScriptコード実行
- 💻 クロスプラットフォーム対応
  - Windows
  - macOS
  - Linux
- 🎯 Avalonia 11.3.6ベースのモダンUI
- 📦 .NET 9.0対応

---

## 凡例

- ✨ 新機能
- 🔧 修正
- 🐛 バグ修正
- 📝 ドキュメント
- 🎨 UIの改善
- ⚡ パフォーマンス改善
- 🔒 セキュリティ
- 📦 依存関係の更新
- 🧪 テスト
- 🔄 CI/CD

---

[1.2.0]: https://github.com/SilentMalachite/ZapEditor/releases/tag/v1.2.0
[1.1.0]: https://github.com/SilentMalachite/ZapEditor/releases/tag/v1.1.0
[1.0.0]: https://github.com/SilentMalachite/ZapEditor/releases/tag/v1.0.0
