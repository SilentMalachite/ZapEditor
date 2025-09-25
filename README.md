# ZapEditor

A cross-platform code editor built with F# and Avalonia UI featuring syntax highlighting and multi-language code execution.

## Features

- 🎨 Syntax highlighting with AvaloniaEdit
- 🌍 Multi-language support (Japanese, English, Chinese)
- 📁 File operations (open/save dialogs)
- ▶️ Code execution for multiple languages:
  - Python
  - F#
  - C#
  - JavaScript
- 🎯 Modern cross-platform UI with Avalonia

## Technology Stack

- **Framework**: .NET 9.0
- **UI Framework**: Avalonia 11.3.6
- **Language**: F#
- **Editor Component**: AvaloniaEdit 0.10.12
- **Syntax Highlighting**: TextMate integration

## Prerequisites

- .NET 9.0 SDK
- For code execution features:
  - Python 3 (for Python code execution)
  - Node.js (for JavaScript code execution)

## Building

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

## Project Status

⚠️ **Note**: This project is currently in development. Some features may require additional work due to API compatibility issues between Avalonia versions.

### Recent Fixes

- ✅ Fixed package version conflicts (AvaloniaEdit)
- ✅ Resolved duplicate resource compilation issues
- ✅ Fixed F# syntax errors in core services
- 🔧 Additional API compatibility work needed for full functionality

## Architecture

- `Program.fs` - Application entry point
- `App.axaml/.fs` - Main application configuration
- `MainWindow.axaml/.fs` - Main window implementation
- `ViewModels/` - MVVM view models
- `Services/` - Business logic services
- `Controls/` - Custom UI controls
- `Resources/` - Localization resources

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is open source. Please add your preferred license here.
