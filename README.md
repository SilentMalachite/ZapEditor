# ZapEditor

Cross-platform code editor built with F# and Avalonia UI. ZapEditor focuses on fast startup, TextMate-based syntax highlighting, and lightweight code execution helpers for popular scripting languages.

## Table of Contents

1. [Feature Highlights](#feature-highlights)
2. [Screenshots](#screenshots)
3. [Quick Start](#quick-start)
4. [Configuration](#configuration)
5. [Project Layout](#project-layout)
6. [Localization](#localization)
7. [Development Workflow](#development-workflow)
8. [Contributing](#contributing)
9. [License](#license)

## Feature Highlights

- 🎨 **TextMate syntax highlighting** powered by `Avalonia.AvaloniaEdit` 11.0.5 + TextMate grammars
- 🌐 **Instant language switching** (Japanese / English / Chinese) with localized resource strings
- 🗂️ **File operations** backed by Avalonia storage provider (open, save, save-as)
- ▶️ **Inline code execution** for F#, C#, Python, and JavaScript via pluggable `CodeExecutionService`
- 💻 **Desktop-first UX** using Avalonia 11.3.6 targeting Windows, macOS, and Linux

## Screenshots

_Coming soon — contributions welcome!_

## Quick Start

### Prerequisites

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- Optional runtime dependencies for code execution features:
  - Python 3.x (`python3` on PATH)
  - Node.js 18+ (`node` on PATH)

### Clone & Run

```bash
git clone https://github.com/SilentMalachite/ZapEditor.git
cd ZapEditor

# Restore packages (offline-friendly)
dotnet restore --ignore-failed-sources

# Build and start the app
dotnet run
```

> ℹ️ 脆弱性データの取得がネットワーク制限で失敗する場合は `--ignore-failed-sources` を付けてください。ビルド自体には影響しません。

### Packaging

```bash
# Produce a framework-dependent build
dotnet publish -c Release -r win-x64 --self-contained false
```

Adjust the runtime identifier (`-r`) for `osx-x64`, `linux-x64`, etc.

## Configuration

| Area | How to customize |
| ---- | ---------------- |
| Default culture | Update `Resources/Strings.*.resx` or call `ResourceManager.SetLanguage` on startup |
| Syntax themes | Replace `ThemeName.DarkPlus` in `Controls/SyntaxHighlightEditor.axaml.fs` with another `ThemeName` |
| Supported grammars | Extend `SetLanguage` mapping in `SyntaxHighlightEditor` and `DetectLanguage` in `MainWindowViewModel` |
| Execution backends | Implement additional helpers in `Services/CodeExecutionService.fs` |

## Project Layout

```
ZapEditor/
├── App.axaml(.fs)              # Application shell and lifecycle hooks
├── Controls/
│   └── SyntaxHighlightEditor   # Custom TextMate-enabled editor control
├── Resources/                  # Localized string resources
├── Services/
│   ├── CodeExecutionService.fs # External process executor
│   └── ResourceManager.fs      # Culture-aware resource accessor
├── ViewModels/
│   └── MainWindowViewModel.fs  # MVVM logic and command bindings
├── MainWindow.axaml(.fs)       # Main window view + code-behind
└── Program.fs                  # Entry point
```

## Localization

UI strings live under `Resources/Strings.<culture>.resx`. Add new cultures by duplicating the neutral resource file, translating strings, and wiring the culture code in `ResourceManager.SetLanguage`.

The current UI culture is toggled at runtime from `MainWindowViewModel.SetLanguage`, ensuring the menu, status text, and dialogs are refreshed immediately.

## Development Workflow

```bash
# Lint / build continuously
dotnet build

# Clean intermediate artifacts
dotnet clean

# Run the editor in watch mode (hot reload)
dotnet watch run

# Run tests (if available)
dotnet test
```

When working on TextMate grammars, enable Avalonia diagnostics (`dotnet run -c Debug`) to inspect control trees in-app.

## Recent Improvements

### v1.1.0 - UI Enhancements and Bug Fixes
- **Fixed Language Selector**: ComboBox now properly binds to ViewModel with AvailableLanguages property
- **Enhanced File Operations**: Complete file opening functionality with comprehensive error handling
- **Resolved Build Warnings**: Eliminated F# compiler warnings and NuGet audit warnings
- **Improved Error Handling**: Added proper exception handling for file access operations
- **Code Quality**: Removed unused recursive object references and cleaned up resource files

### Key Technical Changes
- MainWindowViewModel: Removed `as this` parameter to eliminate FS1183 warning
- MainWindow.axaml: Updated ComboBox binding to use ItemsSource and SelectedItem
- ZapEditor.fsproj: Added `NuGetAudit=false` and `NuGetAuditMode=direct` settings
- File Service: Enhanced error handling for UnauthorizedAccessException, PathTooLongException, etc.
- Resource Management: Cleaned up Japanese resource file formatting

## Contributing

Contributions are very welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on setting up your environment, opening issues, and submitting pull requests.

## License

Licensed under the [Apache License 2.0](LICENSE).
