# Contributing to ZapEditor

Thanks for your interest in improving ZapEditor! This document outlines how to set up your environment, report issues, and submit patches.

## 📋 Code of Conduct

Respectful and inclusive communication is essential. By participating in this project you agree to uphold the values described in the [Contributor Covenant](https://www.contributor-covenant.org/version/2/1/code_of_conduct/). If you experience or witness unacceptable behavior, please open a confidential issue.

## 🛠️ Development Environment

1. Install the [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0).
2. (Optional) Install Python 3.x and Node.js 18+ if you plan to work on the code execution service.
3. Restore packages:
   ```bash
   dotnet restore --ignore-failed-sources
   ```
4. Run the application locally:
   ```bash
   dotnet run
   ```

### Recommended Tools

- IDE: [JetBrains Rider](https://www.jetbrains.com/rider/) or [Visual Studio Code](https://code.visualstudio.com/) with the Ionide extension pack
- Formatter: `dotnet fantomas` (if available) for keeping F# code consistent
- Linting: `dotnet build` (compilation warnings are treated as lint feedback)

## 🔁 Workflow

1. **Fork** the repository and create a feature branch (`feature/<short-description>`).
2. Keep your branch focused—split large work into small, reviewable commits.
3. Run `dotnet build` before pushing to ensure the project compiles.
4. Update or add documentation and tests when altering behavior.
5. Open a **Pull Request** targeting `main`.
   - Provide a clear description of the change and screenshots/gifs when UI is affected.
   - Reference related issues (e.g. `Closes #123`).

## 🧪 Testing

- Use `dotnet watch run` for rapid UI iteration.
- For code execution changes, add smoke tests or scripts that exercise the new language support where possible.
- Manual verification checklist for UI PRs:
  - File open/save dialogs work on your platform
  - Syntax highlighting loads for at least one language
  - Status bar updates when switching UI language

## 🐞 Reporting Bugs

When filing an issue, please include:

- Environment (OS, .NET SDK version)
- Steps to reproduce
- Expected vs. actual behavior
- Logs or screenshots when applicable

## ✨ Feature Requests

Describe the user problem you are solving, not just the solution. Include:

- Motivation / use case
- Proposed UX (mockups welcome)
- Suggested implementation details (optional)

## 📄 Licensing

By contributing, you agree that your contributions will be licensed under the terms of the [Apache License 2.0](LICENSE).
