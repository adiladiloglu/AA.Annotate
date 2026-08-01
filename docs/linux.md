# Linux installation and requirements

AA.Annotate 0.5.0 supports Linux x64 desktops through Avalonia's X11 backend. It runs
directly on X11 and through XWayland on Wayland. Screen capture uses direct X11 capture
in an X11 session and the standard XDG Screenshot portal in a Wayland session.

## Release package or source build

The GitHub release archive, `aa-annotate-<version>-linux-x64.tar.gz`, is self-contained.
It includes the .NET runtime, so installing .NET separately is not required to run that
archive.

.NET is required in these cases:

- building, testing, or running AA.Annotate from the repository requires the .NET 10 SDK;
- a custom framework-dependent publish created with `-SelfContained:$false` requires the
  .NET 10 runtime on the target computer.

If an installed release reports that no compatible .NET framework was found, verify that
the self-contained GitHub archive was downloaded rather than a framework-dependent
development build.

## Native desktop dependencies

The self-contained archive still relies on native libraries supplied by the Linux
desktop. On Debian and Ubuntu, install the common requirements with:

```bash
sudo apt update
sudo apt install libice6 libsm6 libfontconfig1 libx11-6
```

These packages provide the X11 client, session-management, and font libraries used by
Avalonia and Skia. Their dependencies install supporting libraries such as FreeType.

For a Wayland desktop, AA.Annotate also requires XWayland and a working screenshot portal:

```bash
sudo apt install xwayland xdg-desktop-portal
```

Install the portal backend for the active desktop environment:

```bash
# GNOME
sudo apt install xdg-desktop-portal-gnome

# KDE Plasma
sudo apt install xdg-desktop-portal-kde
```

Only one matching desktop backend is normally needed. Distribution package names can
vary; use equivalent packages on non-Debian distributions.

## Install the release archive

Download the Linux x64 archive from the GitHub release, then run:

```bash
tar -xzf "aa-annotate-<version>-linux-x64.tar.gz"
cd "aa-annotate-<version>-linux-x64"
./install.sh
```

The user-scoped installer does not require root and does not modify `PATH`. It installs:

```text
App and CLI: $HOME/.local/opt/aa-annotate
Codex skill: ${CODEX_HOME:-$HOME/.codex}/skills/aa-annotate
```

Optionally create `$HOME/.local/bin/aa-annotate`:

```bash
./install.sh --add-cli-link
```

Start an agent session without changing `PATH`:

```bash
"$HOME/.local/opt/aa-annotate/cli/aa-annotate" session --wait
```

The app must start inside the user's active local graphical session. An SSH shell without
the desktop's display, runtime directory, and session bus is not a supported way to launch
an interactive annotation session.

## Build and test from source

Install the .NET 10 SDK and confirm it is selected:

```bash
dotnet --version
dotnet --list-runtimes
```

Then, from the repository root:

```bash
dotnet restore AA.Annotate.slnx
dotnet test AA.Annotate.slnx -v minimal
dotnet run --project src/AA.Annotate.App/AA.Annotate.App.csproj
```

Create the self-contained Linux release package from a Linux host with PowerShell 7 and
`tar` available:

```bash
pwsh ./scripts/package-linux.ps1 -Version 0.5.0
```

The package must be created on Linux so executable Unix modes are preserved in the tar
archive.

## Wayland consent and limitations

The desktop may show a screenshot consent dialog. Only the user should approve or cancel
it. Cancellation is not permission to retry through an X11-only screenshot tool.

Some GNOME portal versions show capture UI but terminate the request without returning an
image. AA.Annotate reports this as capture unavailable. Logging into an X11 session is the
supported alternative; the app does not read the Pictures directory, call a private
desktop screenshot API, or export an incomplete XWayland-only image.

Basic VMConnect and similar single-display environments do not validate real
multi-monitor or mixed-scale behavior.

## Troubleshooting

- `libICE.so.6`, `libSM.so.6`, `libfontconfig.so.1`, or `libX11.so.6` missing: install the
  corresponding native packages listed above.
- No compatible .NET runtime: use the self-contained GitHub archive, or install the .NET
  10 runtime when intentionally using a framework-dependent development build.
- Cannot open the display: launch from the active local desktop session rather than an
  SSH-only environment.
- Portal unavailable on Wayland: verify `xdg-desktop-portal` and the backend matching the
  current desktop are installed and running.
- The file exists but is not executable: re-extract the `.tar.gz` archive on Linux so its
  Unix modes are retained.

Do not run AA.Annotate as root or weaken desktop security settings to make capture work.

Primary platform references:

- [Avalonia Desktop Linux](https://docs.avaloniaui.net/docs/platform-specific-guides/linux)
- [.NET application publishing](https://learn.microsoft.com/dotnet/core/deploying/)
- [XDG Screenshot portal](https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Screenshot.html)
