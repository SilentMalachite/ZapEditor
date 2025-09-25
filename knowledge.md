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

## Development Commands
- Build: `dotnet build`
- Run: `dotnet run`
- Clean: `dotnet clean`
- Restore packages: `dotnet restore`

## Notes
- Uses compiled bindings by default for better performance
- Debug builds include Avalonia DevTools
- Targets Windows executable with COM interop support