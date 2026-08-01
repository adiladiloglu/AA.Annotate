# AA.Annotate

AA.Annotate is a desktop overlay for collecting screenshot annotations during AI agent sessions.

It gives agents a small, reliable way to request visual context: capture the screen, crop the relevant area, mask private regions, draw numbered boxes, add comments, and return a generated handoff file.

## Screenshots

### Multiple captures and tabs

Each capture is kept as a separate tab so several windows or application states can be reviewed in one session.

![AA.Annotate capture selector showing two capture tabs](docs/Media/screenshots/gnome-multiple-captures.png)

### Crop

Crop mode limits the handoff to the relevant part of the active capture. The capture-quality selector appears only while a capture is active.

![AA.Annotate crop mode on GNOME](docs/Media/screenshots/gnome-crop-mode.png)

### Privacy masks and annotations

Privacy masks redact selected regions, while numbered annotation boxes open a focused comment editor for review instructions.

![AA.Annotate privacy mask on GNOME](docs/Media/screenshots/gnome-privacy-mask.png)

![AA.Annotate numbered annotation and comment editor on GNOME](docs/Media/screenshots/gnome-annotation-comment.png)

See the [screenshot gallery and capture guide](docs/screenshots.md) for the full set, including the passive KDE toolbar.

## Features

- Full-screen screenshot capture for the selected display.
- Multi-capture sessions for different windows, tabs, displays, or application states.
- Crop support with blurred out-of-scope regions.
- Privacy masks that permanently redact selected regions in exported images.
- Numbered annotation boxes with comments.
- Export image scaling from 20% to 100% with presets for smaller handoff payloads.
- Private working files stored under the OS temp directory by default.
- Final agent-facing exports stored separately from private working files.
- Agent-facing `review.md` handoff plus structured annotation data.
- Bundled Codex skill and command-line launcher.

## Platform

- Windows 10/11 x64.
- Linux x64 on X11.
- Linux x64 on Wayland when the desktop's XDG Screenshot portal works correctly.

AA.Annotate uses XWayland for its overlay windows in a Wayland session and the standard
desktop portal for the screenshot itself. Portal behavior varies by desktop. GNOME
Wayland configurations whose portal terminates a screenshot request without returning
an image are reported as unavailable; AA.Annotate does not bypass Wayland security or
silently capture an incomplete XWayland-only desktop.

macOS support is planned for later.

## Installation

### Windows

Install from the latest GitHub Release:

```powershell
powershell -ExecutionPolicy Bypass -Command "irm https://raw.githubusercontent.com/adiladiloglu/AA.Annotate/master/install.ps1 | iex"
```

The release bundle installs:

```text
App and CLI: %LOCALAPPDATA%\AA.Annotate
Codex skill: %USERPROFILE%\.codex\skills\aa-annotate
```

The default installer does not modify `PATH` and does not set persistent environment variables.

To install the bundled Codex plugin as well:

```powershell
powershell -ExecutionPolicy Bypass -Command "& ([scriptblock]::Create((irm https://raw.githubusercontent.com/adiladiloglu/AA.Annotate/master/install.ps1))) -InstallCodexPlugin"
```

### Manual Install

1. Download `aa-annotate-<version>-win-x64.zip` from the latest GitHub Release.
2. Extract the zip.
3. Run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```

### Optional Registration

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -AddCliToUserPath
powershell -ExecutionPolicy Bypass -File .\install.ps1 -SetUserAppEnvironmentVariable
```

### Uninstall

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

### Linux

Download `aa-annotate-<version>-linux-x64.tar.gz` from the latest GitHub Release,
then install it for the current user:

```bash
tar -xzf "aa-annotate-<version>-linux-x64.tar.gz"
cd "aa-annotate-<version>-linux-x64"
./install.sh
```

The release bundle is self-contained: it includes the .NET runtime, so end users do not
need to install .NET separately. Building or running from the repository requires the
.NET 10 SDK.

Install the native desktop libraries before launching the app on Debian or Ubuntu:

```bash
sudo apt update
sudo apt install libice6 libsm6 libfontconfig1 libx11-6
```

Wayland additionally requires XWayland, the XDG portal, and the backend matching the
desktop. For example:

```bash
# GNOME
sudo apt install xwayland xdg-desktop-portal xdg-desktop-portal-gnome

# KDE Plasma
sudo apt install xwayland xdg-desktop-portal xdg-desktop-portal-kde
```

The bundle installs to:

```text
App and CLI: $HOME/.local/opt/aa-annotate
Codex skill: ${CODEX_HOME:-$HOME/.codex}/skills/aa-annotate
```

The default installer does not modify `PATH` and does not require root. To create
`$HOME/.local/bin/aa-annotate`, use:

```bash
./install.sh --add-cli-link
```

See [Linux installation and requirements](docs/linux.md) for source-build requirements,
framework-dependent deployment, graphical-session requirements, portal behavior, and
troubleshooting.

Uninstall with:

```bash
"$HOME/.local/opt/aa-annotate/uninstall.sh"
```

If the optional CLI link was installed:

```bash
"$HOME/.local/opt/aa-annotate/uninstall.sh" --remove-cli-link
```

## Agent Usage

Agents should launch AA.Annotate through the bundled CLI:

Windows:

```powershell
& "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait --timeout-seconds 60
```

Linux:

```bash
"$HOME/.local/opt/aa-annotate/cli/aa-annotate" session --wait --timeout-seconds 60
```

To store final exported handoff files somewhere specific, pass an output root:

```powershell
& "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait --output "D:\AA.Annotate Exports"
```

```bash
"$HOME/.local/opt/aa-annotate/cli/aa-annotate" session --wait --output "$HOME/AA.Annotate Exports"
```

`--session-root` is for controlling private working files while debugging AA.Annotate itself. Normal agent workflows should use `--output` when they need a custom final export location.

Command help:

```powershell
& "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" --help
& "$env:LOCALAPPDATA\AA.Annotate\app\AA.Annotate.App.exe" --help
```

```bash
"$HOME/.local/opt/aa-annotate/cli/aa-annotate" --help
"$HOME/.local/opt/aa-annotate/app/AA.Annotate.App" --help
```

When the session completes, the CLI prints:

```text
SESSION_STATUS=completed
REVIEW_MD=<export folder>\review.md
ANNOTATIONS_JSON=<export folder>\annotations.json
```

The printed paths point to the final export folder. Agents should not inspect private working files while the annotation window is still open. The final export images are the agent-facing source of truth; private original captures are not part of the normal handoff.

## User Workflow

1. The agent opens AA.Annotate.
2. The user captures the relevant screen.
3. The user crops the capture if only part of the screen matters.
4. The user draws privacy masks over sensitive regions.
5. The user sets export scale when full-resolution images are unnecessary.
6. The user draws numbered annotation boxes.
7. The user adds comments.
8. The user completes the session and exports the handoff.

## Export Behavior

AA.Annotate applies export operations in this order:

1. Crop filtering and coordinate normalization.
2. Privacy mask redaction.
3. Image scaling.
4. Annotated overview and annotation crop generation.

Privacy masks are exported as black rectangles labeled `Privacy mask`. The label is part of the exported image so downstream agents can distinguish intentional redaction from missing image content.

When export scale is below 100%, exported images and exported coordinates use the scaled image dimensions. Original unscaled working captures are private session files and are not part of the normal agent handoff.

## Agent Skill

The bundled Codex skill is installed with the app package.

Windows:

```text
%USERPROFILE%\.codex\skills\aa-annotate
```

Linux:

```text
${CODEX_HOME:-$HOME/.codex}/skills/aa-annotate
```

Agent-facing workflow details are documented in [skills/aa-annotate/SKILL.md](skills/aa-annotate/SKILL.md).

## Releasing

Tagged releases are tested and packaged on both Windows and Linux before GitHub
publishes them. See [Releasing AA.Annotate](docs/releasing.md) for the versioning,
validation, publication, and verification checklist.

## License

AA.Annotate is licensed under the [Apache License 2.0](LICENSE).
