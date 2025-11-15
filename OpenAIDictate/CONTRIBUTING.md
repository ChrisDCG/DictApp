# Contributing to OpenAIDictate

Thank you for your interest in improving OpenAIDictate! We strive to maintain enterprise-grade quality, deterministic builds, and a respectful community. This guide explains how to contribute effectively.

## Code of Conduct

Participation in this project is governed by the [Code of Conduct](CODE_OF_CONDUCT.md). Please review it before engaging in discussions or submitting changes.

## Getting Started

1. Fork the repository and clone your fork.
2. Install the latest [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows. Cross-compiling from Linux/macOS is supported thanks to `EnableWindowsTargeting`, but testing the GUI requires Windows.
3. Copy `.env.example` to `.env` (or export the equivalent environment variables) and supply your own OpenAI credentials.
4. Restore dependencies: `dotnet restore`.
5. Build the solution: `dotnet build`.

## Development Workflow

1. **Create a topic branch** from `main`. Use descriptive names such as `feature/improve-vad-config` or `fix/hotkey-null-reference`.
2. **Make focused commits** following [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).
3. **Write tests**:
   - Unit tests belong in `OpenAIDictate.Tests` using xUnit.
   - Integration/UI tests should be placed under `tests/integration` (create when needed).
4. **Run quality gates** before opening a pull request:
   ```powershell
   dotnet format
   dotnet test
   ```
   Include the exact commands and results in your pull request description.
5. **Update documentation** whenever behavior changes:
   - `README.md` for user-facing information
   - `docs/api.md` for public API changes
   - `CHANGELOG.md` for noteworthy releases

## Reporting Bugs

Use [GitHub issues](https://github.com/yourrepo/OpenAIDictate/issues) and provide the following template:

- Environment (Windows version, .NET runtime)
- Steps to reproduce
- Expected behavior
- Actual behavior
- Logs (`%APPDATA%/OpenAIDictate/logs`)
- Optional screenshots

Critical security issues should be reported privately to `security@yourrepo.invalid`.

## Requesting Features

When suggesting new functionality, explain the business case and any alternatives considered. Feature requests that align with the product vision are prioritized.

## Pull Request Guidelines

- Fill out the PR template (automatically generated).
- Ensure local builds, tests, and analyzers pass before requesting review.
- Squash commits when merging unless there is a strong reason to preserve history.
- A maintainer must approve every PR. Reviews focus on correctness, security, accessibility, performance, and maintainability.

## Style Guide

- Follow the `.editorconfig` rules enforced by `dotnet format`.
- Favor clear, well-structured code with XML documentation where public APIs are exposed.
- Handle exceptions explicitly and avoid swallowing errors silently.

## Thanks

Your contributions make OpenAIDictate better for everyone. We appreciate your help!
