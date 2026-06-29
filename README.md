# AA Annotate

AA Annotate is a Windows screenshot annotation tool used by AI agents when they need visual feedback from the user.

The agent opens the overlay. The user captures, crops, marks, comments, and sends the result back.

## Platform

Windows is supported now. macOS and Linux support are planned for later.

## Install

AA Annotate must be installed as an app bundle. The bundle installs the desktop app, the CLI, and the agent skill together.

One-command install from the latest GitHub Release:

```powershell
powershell -ExecutionPolicy Bypass -Command "irm https://raw.githubusercontent.com/adiladiloglu/AA.Annotate/master/install.ps1 | iex"
```

The one-command install requires the repository and release asset to be publicly accessible. For private repository testing, clone the repo and run `install.ps1` locally with `-GitHubToken` or set `GITHUB_TOKEN`/`GH_TOKEN`.

Default install locations:

```text
App and CLI: %LOCALAPPDATA%\AA.Annotate
Codex skill: %USERPROFILE%\.codex\skills\aa-annotate
```

Manual install:

1. Download `aa-annotate-<version>-win-x64.zip` from the latest GitHub Release.
2. Extract the zip.
3. Run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```

The default install does not modify `PATH` and does not set persistent environment variables.

Optional user-scoped registration:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -AddCliToUserPath
powershell -ExecutionPolicy Bypass -File .\install.ps1 -SetUserAppEnvironmentVariable
```

Uninstall:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

## How It Works

AA Annotate is not meant to be started manually during normal use. An AI agent starts it when screen context is needed.

After install, agents should run the bundled CLI directly:

```powershell
& "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait --timeout-seconds 60
```

The app stores capture and annotation files under the OS temp directory by default, so it does not clutter the current workspace.

## User Interaction

When the overlay opens:

1. Capture the relevant screen.
2. Crop the capture when only part of the screen matters.
3. Draw numbered annotation boxes.
4. Add comments.
5. Send the session back to the waiting agent.

One session can include multiple captures for different windows, tabs, displays, or application states.

## Agent Skill

The app bundle installs the Codex skill at:

```text
%USERPROFILE%\.codex\skills\aa-annotate
```

The skill defines when to launch AA Annotate, how to wait for completion, and which output file to read.

Agent-facing command and output details are documented in [skills/aa-annotate/SKILL.md](skills/aa-annotate/SKILL.md).
