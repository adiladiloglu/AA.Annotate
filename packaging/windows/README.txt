AA Annotate Windows Package

This package is distributed as:

  aa-annotate-<version>-win-x64.zip

Install, user-local and non-invasive:

  powershell -ExecutionPolicy Bypass -File .\install.ps1

Default install locations:

  App and CLI: %LOCALAPPDATA%\AA.Annotate
  Skill:       %USERPROFILE%\.codex\skills\aa-annotate

The default installer does not modify PATH and does not set persistent
environment variables. Agents can run the tool directly with:

  & "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait

Optional user-scoped registration:

  powershell -ExecutionPolicy Bypass -File .\install.ps1 -AddCliToUserPath
  powershell -ExecutionPolicy Bypass -File .\install.ps1 -SetUserAppEnvironmentVariable

Uninstall:

  powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
