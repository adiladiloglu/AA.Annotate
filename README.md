# AA Annotate

AA Annotate is a Windows screenshot annotation tool for AI agents.

An agent opens an annotation session, waits while the user marks the screen, then continues from the generated `review.md` handoff.

## Platform

Windows is supported now. macOS and Linux support are planned for later.

## Install

Download `aa-annotate-<version>-win-x64.zip` from the latest GitHub Release, extract it, and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Default install locations:

```text
%LOCALAPPDATA%\AA.Annotate
%USERPROFILE%\.codex\skills\aa-annotate
```

The default installer does not modify `PATH` and does not set persistent environment variables.

Optional user-scoped registration:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -AddCliToUserPath
powershell -ExecutionPolicy Bypass -File .\install.ps1 -SetUserAppEnvironmentVariable
```

Uninstall:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

## Agent Command

Preferred command when the CLI is on `PATH`:

```powershell
aa-annotate session --wait
```

Default installed path when `PATH` was not changed:

```powershell
& "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait
```

From source:

```powershell
dotnet run --project src\AA.Annotate.Cli\AA.Annotate.Cli.csproj -- session --wait
```

Successful sessions print:

```text
SESSION_STATUS=completed
REVIEW_MD=<session folder>\review.md
ANNOTATIONS_JSON=<session folder>\annotations.json
```

Agents should read `REVIEW_MD` first. Use `ANNOTATIONS_JSON` only when exact coordinates or structured metadata are needed.

## Session Model

AA Annotate stores session files under the OS temp directory by default.

Typical files:

```text
review.md
annotations.json
session.json
captures/
```

Each annotation contains:

```text
Box number
Box coordinates on the screenshot
Annotation text
```

One session can include multiple captures for different windows, tabs, displays, or application states.

## User Interaction

The user works only inside the annotation overlay opened by the agent:

1. Capture the relevant screen.
2. Crop the capture when only part of the screen matters.
3. Draw numbered annotation boxes.
4. Add comments.
5. Send the session back to the waiting agent.

## Codex Skill

The package installs a Codex skill at:

```text
%USERPROFILE%\.codex\skills\aa-annotate
```

The skill defines when to launch AA Annotate, how to wait for completion, and which output file to read.

## Development

Prerequisites:

```text
Windows
.NET SDK 10
PowerShell
```

Run tests:

```powershell
dotnet test AA.Annotate.slnx -v minimal
```

Create a Windows package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-win.ps1 -Version 0.1.0
```

Publish a GitHub Release from an authenticated checkout:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-github-release.ps1 -Version 0.1.0
```

Pushing a tag such as `v0.1.0` also runs the GitHub Actions release workflow.

## Distribution

Current release format:

```text
aa-annotate-<version>-win-x64.zip
```

Future distribution targets:

```text
MSIX or winget for Windows installation
MCP server for cross-agent runtime integration
Agent-specific packaging for Codex, Claude, Cursor, and GitHub Copilot
```
