AA Annotate Windows Package

This package is distributed as:

  aa-annotate-<version>-win-x64.zip

It installs the desktop app, CLI, and Codex skill together. The skill is not
useful without the bundled executable.

Install, user-local and non-invasive:

  powershell -ExecutionPolicy Bypass -File .\install.ps1

Default install locations:

  App and CLI: %LOCALAPPDATA%\AA.Annotate
  Skill:       %USERPROFILE%\.codex\skills\aa-annotate

The default installer does not modify PATH and does not set persistent
environment variables. Agents can run the tool directly with:

  & "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait --timeout-seconds 60

Store sessions outside the OS temp directory:

  & "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait --session-root "D:\AA Annotate Sessions"

Show command help:

  & "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" --help
  & "$env:LOCALAPPDATA\AA.Annotate\app\AA.Annotate.App.exe" --help

Optional user-scoped registration:

  powershell -ExecutionPolicy Bypass -File .\install.ps1 -AddCliToUserPath
  powershell -ExecutionPolicy Bypass -File .\install.ps1 -SetUserAppEnvironmentVariable

Uninstall:

  powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
