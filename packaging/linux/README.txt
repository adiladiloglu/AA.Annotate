AA.Annotate Linux Package

This package is distributed as:

  aa-annotate-<version>-linux-x64.tar.gz

It contains a self-contained .NET desktop app, CLI, and Codex skill. A system-wide
.NET installation is not required for this release archive. Building from source
requires the .NET 10 SDK; a custom framework-dependent publish requires the .NET
10 runtime. The skill is not useful without the bundled executables.

Supported desktop paths:

  - X11 capture uses the X11 desktop.
  - Wayland capture uses the XDG desktop Screenshot portal.
  - Wayland requires a working portal backend and may show a consent dialog.

Native desktop requirements on Debian and Ubuntu:

  sudo apt update
  sudo apt install libice6 libsm6 libfontconfig1 libx11-6

Wayland sessions also use XWayland and require the XDG Screenshot portal plus the
backend matching the current desktop:

  GNOME:
    sudo apt install xwayland xdg-desktop-portal xdg-desktop-portal-gnome

  KDE Plasma:
    sudo apt install xwayland xdg-desktop-portal xdg-desktop-portal-kde

Package names can vary on other distributions. Launch the app from the user's
active local graphical session, not from an SSH-only shell. Do not run it as root.

Install for the current user:

  ./install.sh

Default install locations:

  App and CLI: $HOME/.local/opt/aa-annotate
  Skill:       ${CODEX_HOME:-$HOME/.codex}/skills/aa-annotate

The default installer does not modify PATH. To add a link under
$HOME/.local/bin:

  ./install.sh --add-cli-link

Run without PATH changes:

  "$HOME/.local/opt/aa-annotate/cli/aa-annotate" session --wait --timeout-seconds 60

Show command help:

  "$HOME/.local/opt/aa-annotate/cli/aa-annotate" --help
  "$HOME/.local/opt/aa-annotate/app/AA.Annotate.App" --help

Uninstall:

  "$HOME/.local/opt/aa-annotate/uninstall.sh"

If installation created the optional CLI link:

  "$HOME/.local/opt/aa-annotate/uninstall.sh" --remove-cli-link

License: Apache-2.0
