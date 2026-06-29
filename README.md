# AA Annotate

AA Annotate is a lightweight Windows annotation overlay for AI agents. It lets an agent ask the user to visually mark what matters on screen, wait for the user to finish, and continue from a generated `review.md` handoff.

The app is designed for agent-driven sessions:

- the agent starts the annotation session;
- the user captures one or more screens;
- the user crops, draws numbered boxes, and writes comments;
- the app writes all session files under the OS temp directory;
- the agent reads `review.md` and continues the task.

## Status

Current platform support:

- Windows x64
- .NET 10
- Avalonia UI

The packaged Windows build is self-contained, so end users do not need to install the .NET runtime.

## Install From GitHub Releases

1. Download `aa-annotate-<version>-win-x64.zip` from the latest GitHub Release.
2. Extract the zip.
3. Run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```

Default install locations:

```text
%LOCALAPPDATA%\AA.Annotate
%USERPROFILE%\.codex\skills\aa-annotate
```

The default installer is intentionally non-invasive. It does not modify `PATH` and does not set persistent environment variables.

To run the installed CLI without changing `PATH`:

```powershell
& "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait
```

Optional user-scoped registration:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -AddCliToUserPath
powershell -ExecutionPolicy Bypass -File .\install.ps1 -SetUserAppEnvironmentVariable
```

Uninstall:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

## Agent Usage

Preferred agent command:

```powershell
aa-annotate session --wait
```

If the installer was used without `PATH` changes:

```powershell
& "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait
```

If running from source:

```powershell
dotnet run --project src\AA.Annotate.Cli\AA.Annotate.Cli.csproj -- session --wait
```

When the user sends the session back, the CLI prints:

```text
SESSION_STATUS=completed
REVIEW_MD=<session folder>\review.md
ANNOTATIONS_JSON=<session folder>\annotations.json
```

Agents should read `REVIEW_MD` first. `ANNOTATIONS_JSON` is available when exact coordinates or structured metadata are required.

## User Workflow

1. The agent starts AA Annotate.
2. The floating command bar appears over the selected display.
3. The user can move the idle command bar between displays while still interacting with the underlying desktop.
4. Capture takes a screenshot of the active display.
5. Crop can limit the relevant area; cropped-out regions stay blurred.
6. Annotation mode lets the user draw numbered rectangles and add comments.
7. The green send button completes the session and returns data to the waiting agent.

## Session Output

AA Annotate stores session data under the OS temp directory by default, not in the workspace.

Typical files:

```text
review.md
annotations.json
session.json
captures/
```

The key model is:

```text
Annotation
  Box number
  Box coordinates on the screenshot
  Annotation text
```

Multiple captures are supported in one session, so a user can annotate different windows, tabs, displays, or application states before sending the result back.

## Codex Skill

The package installs a Codex skill at:

```text
%USERPROFILE%\.codex\skills\aa-annotate
```

The skill tells Codex-compatible agents when to launch the app, how to wait for completion, and which output file to read first.

## Development

Prerequisites:

- Windows
- .NET SDK 10
- PowerShell

Build and test:

```powershell
dotnet test AA.Annotate.slnx -v minimal
```

Run from source:

```powershell
dotnet run --project src\AA.Annotate.Cli\AA.Annotate.Cli.csproj -- session --wait
```

Create a Windows release package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-win.ps1 -Version 0.1.0
```

Publish a GitHub Release from a local authenticated checkout:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-github-release.ps1 -Version 0.1.0
```

The repository also includes a tag-driven GitHub Actions workflow. Pushing a tag such as `v0.1.0` builds the Windows package and creates the release asset.

## Distribution Strategy

The current release uses a zip package plus a user-local PowerShell installer. This is the least intrusive path while the app is still evolving.

Planned distribution layers:

- Windows zip release for immediate installs.
- MSIX or winget once the installer behavior stabilizes.
- Agent Skills for instruction-level integration.
- MCP server for cross-agent runtime integration with Codex, Claude, Cursor, GitHub Copilot, and other MCP-capable agents.

## Repository Metadata

Suggested GitHub About values:

- Description: `Desktop annotation overlay for AI agent handoff`
- Topics: `ai-agents`, `annotation-tool`, `avalonia`, `codex`, `dotnet`, `mcp`, `screenshot`, `windows`

