# ZapEditor Knowledge

## Project Overview
ZapEditor is a cross-platform code editor built with F# and Avalonia UI. It features syntax highlighting, file operations, and code execution capabilities.

## Technology Stack
- **Framework**: .NET 9.0
- **UI Framework**: Avalonia 11.3.6
- **Language**: F#
- **Editor Component**: AvaloniaEdit with TextMate syntax highlighting

## Project Structure
- `Program.fs` - Application entry point
- `App.axaml/.fs` - Main application configuration
- `MainWindow.axaml/.fs` - Main window implementation
- `ViewModels/` - MVVM view models
- `Services/` - Business logic services
- `Controls/` - Custom UI controls
- `Resources/` - Localization resources (Japanese, English, Chinese)

## Key Features
- Syntax highlighting with AvaloniaEdit
- Multi-language support (Japanese, English, Chinese)
- File operations (open/save dialogs)
- Code execution service
- Resource management
- Vertical writing mode (縦書き) with horizontal/vertical toggle

## Development Commands
- Build: `dotnet build`
- Run: `dotnet run`
- Clean: `dotnet clean`
- Restore packages: `dotnet restore`

## Recent Fixes and Improvements

### Build System
- **NuGet Audit Warnings**: Added `<NuGetAudit>false</NuGetAudit>` and `<NuGetAuditMode>direct</NuGetAuditMode>` to project file
- **F# Compiler Warnings**: Removed unused `as this` parameter from MainWindowViewModel constructor

### UI Binding Issues
- **Language Selector**: Fixed ComboBox binding by implementing `AvailableLanguages` property in ViewModel
- **Data Binding**: Updated MainWindow.axaml to use proper `ItemsSource` and `SelectedItem` bindings
- **Language Changes**: Added `OnLanguageChanged` method for manual language selection

### File Operations
- **File Opening**: Implemented complete file reading functionality with proper error handling
- **Exception Handling**: Added comprehensive exception handling for:
  - UnauthorizedAccessException
  - PathTooLongException
  - DirectoryNotFoundException
  - SecurityException
  - IOException (file in use detection)

### Code Quality
- **Resource Management**: Cleaned up Japanese resource file formatting and removed duplicate entries
- **Error Messages**: Enhanced status messages for better user feedback

## Vertical Writing Mode
- Implemented using RenderTransform rotation (90 degrees)
- Toggle button in toolbar and menu item under View menu
- Localized button text (縦書き/横書き for Japanese, Vertical/Horizontal for English, 竖排/横排 for Chinese)
- Uses WritingModeConverter for data binding
- State managed in MainWindowViewModel and IEditorService

## Notes
- Uses compiled bindings by default for better performance
- Debug builds include Avalonia DevTools
- Targets Windows executable with COM interop support
- Network-independent builds (NuGet audit disabled)
- AvaloniaEdit doesn't natively support vertical text - rotation transform is used as workaround