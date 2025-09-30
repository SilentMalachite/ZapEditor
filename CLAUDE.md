# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

```bash
# Build the project
dotnet build

# Run the editor
dotnet run

# Run with hot reload
dotnet watch run

# Clean build artifacts
dotnet clean

# Run tests (if available)
dotnet test

# Package for distribution
dotnet publish -c Release -r win-x64 --self-contained false
```

## Project Architecture

ZapEditor is a cross-platform code editor built with F# and Avalonia UI using the MVVM pattern.

### Core Architecture Components

**Application Structure:**
- `Program.fs` - Entry point that configures Avalonia and starts the desktop lifetime
- `App.axaml(.fs)` - Application shell with lifecycle hooks
- `MainWindow.axaml(.fs)` - Main window view and code-behind coordination

**MVVM Architecture:**
- `ViewModels/MainWindowViewModel.fs` - Central business logic, command handling, and state management
- Implements `INotifyPropertyChanged` for data binding
- Uses `RelayCommand` for command binding to UI actions
- Manages file operations, code execution, and language switching

**Services Layer:**
- `Services/ResourceManager.fs` - Localization/culture management with runtime language switching
- `Services/FileService.fs` - File I/O operations with comprehensive error handling
- `Services/CodeExecutionService.fs` - Secure code execution for F#, C#, Python, JavaScript
- `Services/IEditorService.fs` & `Services/IFileService.fs` - Service abstractions

**Custom Controls:**
- `Controls/SyntaxHighlightEditor.axaml.fs` - TextMate-based syntax highlighting editor
- Implements `IEditorService` interface
- Supports 15+ programming languages with Dark+ theme
- Provides standard editor operations (undo/redo, cut/copy/paste, select all)

**TextMate Integration:**
- Uses `AvaloniaEdit.TextMate` for syntax highlighting
- Language mapping in `SyntaxHighlightEditor.SetLanguage()` method
- Automatic language detection based on file extensions in `MainWindowViewModel.DetectLanguage()`

**Localization System:**
- Resource files in `Resources/Strings.*.resx`
- Runtime language switching via `ResourceManager.SetLanguage()`
- Supports Japanese, English, and Chinese languages
- UI culture changes propagate immediately to all UI elements

**Code Execution Security:**
- `CodeExecutionService` validates executable paths and restricts to safe directories
- Creates temporary files with GUID names for execution
- Limits output to 200 characters for display in status bar
- Comprehensive error handling for execution failures

### Key Patterns

**Async/Await Usage:** The ViewModel uses `async { }` blocks with `Async.Start` for file operations and `task { }` with `Dispatcher.UIThread.InvokeAsync` for UI updates from background threads.

**Dependency Injection:** Services are injected into the ViewModel constructor with default implementations for loose coupling.

**Error Handling:** Comprehensive exception handling with localized error messages for file access, security, and I/O operations.

**Command Binding:** All UI actions are bound through `RelayCommand` instances in the ViewModel, enabling proper separation of concerns.

### Development Notes

- The project uses F# compilation order specified in `.fsproj` - files must be listed in dependency order
- TextMate grammars are loaded automatically through `AvaloniaEdit.TextMate`
- Language switching updates both UI strings and editor syntax highlighting simultaneously
- File dialogs use Avalonia's storage provider abstraction for cross-platform compatibility