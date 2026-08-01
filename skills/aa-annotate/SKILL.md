---
name: aa-annotate
description: Launch the AA.Annotate desktop overlay on Windows or Linux and continue from the user's exported visual handoff. Use when the user needs to point at screen regions, compare windows or states, crop screenshots, redact private areas, or provide visual context that text alone cannot express.
---

# AA.Annotate

Collect visual input from the user, validate the exported handoff, and continue the original task.

## Launch

Resolve one CLI path without searching the filesystem:

1. For a plugin-provided skill, find `<plugin-root>` two directories above this `SKILL.md`, then use:
   - Windows: `<plugin-root>/cli/aa-annotate.exe`
   - Linux: `<plugin-root>/cli/aa-annotate`
2. For a standalone installation, use:
   - Windows: `%LOCALAPPDATA%/AA.Annotate/cli/aa-annotate.exe`
   - Linux: `$HOME/.local/opt/aa-annotate/cli/aa-annotate`
3. Use a repo-local executable only when explicitly debugging AA.Annotate.

Do not use `Get-Command`, scan directories, rebuild the repository, or set `AA_ANNOTATE_APP` for a normal installed session. If neither installed path exists, report that the AA.Annotate application bundle is missing; a skill file alone is insufficient.

On Linux, the resolved CLI must be a regular executable file. If it exists but is not executable, report that the AA.Annotate installation is incomplete or has lost its executable mode. Do not silently invoke it through `dotnet`, `bash`, or another wrapper, and do not change its permissions unless the user explicitly asks to repair the installation.

Choose the default scale using the guidance below, then launch the resolved CLI directly and wait. These examples use `75` for typical application UI.

Windows PowerShell:

```powershell
& "<absolute-cli-path>" session --wait --default-scale 75
```

Linux shell:

```bash
"<absolute-cli-path>" session --wait --default-scale 75
```

- Replace `75` with the chosen integer percentage. Valid values are `20` through `100`.
- Invoke the executable directly in the form shown. On Windows PowerShell, use the shell call operator. Do not wrap it in `Start-Process`, `System.Diagnostics.Process`, `ProcessStartInfo`, `nohup`, a background job, a polling loop, or a timing helper.
- In particular, do not use `ProcessStartInfo.ArgumentList`; it is `$null` in Windows PowerShell 5.1 on .NET Framework and the launch will fail before the CLI starts.
- Choose `--default-scale` from the expected content:
  - `100` for source code, terminals, dense tables, small text, fine visual defects, or uncertain content.
  - `75` for typical application UI where labels must remain readable.
  - `50` for broad layouts, dashboards with large labels, photos, or composition feedback.
  - `33` or `25` only when large shapes and overall placement matter more than text or fine detail.
- Prefer the higher scale when uncertain. Adjust future defaults using readability of earlier AA.Annotate handoffs in the current task; do not reduce scale after an earlier handoff was hard to read.
- The default applies only to newly created captures. Tell the user they can override quality independently at the top of every capture; the capture's current selection controls its live preview and exported dimensions.
- Add `--output "<folder>"` only when the user requests a specific export destination.
- Add `--session-root "<folder>"` only when debugging AA.Annotate itself.
- Use a tool timeout that covers expected user interaction plus the app's inactivity warning period.
- Keep `--wait` for real annotation sessions. For a short launch-only diagnostic, explain that completion will not be collected before omitting it.
- Do not inspect private session folders while waiting.
- On Linux, launch from the user's active graphical desktop session. If no local graphical session or screen-capture portal is available, report the launch or capture error instead of weakening desktop permissions or substituting an SSH-only session.
- Linux X11 sessions use the supported direct X11 capture path. Wayland sessions use the desktop's XDG Screenshot portal and may show a consent dialog; only the user may allow or cancel that prompt. Portal cancellation is not permission to retry through X11 or another capture utility.
- Some GNOME Wayland portal versions terminate screenshot requests without returning an image even after showing capture UI. AA.Annotate reports that behavior as capture unavailable. Tell the user that the GNOME Wayland portal did not provide a screenshot and that an X11 login session is the supported alternative; do not read the user's Pictures directory, invoke a private desktop screenshot API, or capture only XWayland windows as a fallback.

### Linux launch diagnostics

- Treat the installed GitHub release as self-contained. It includes the .NET runtime and should not require a separate `dotnet` installation.
- Treat a repo-local build or framework-dependent custom package as a development workflow. It requires the .NET 10 SDK or runtime, respectively.
- If Linux reports missing `libICE.so.6`, `libSM.so.6`, `libfontconfig.so.1`, or `libX11.so.6`, tell the user that the corresponding desktop packages are required. On Debian/Ubuntu these are `libice6`, `libsm6`, `libfontconfig1`, and `libx11-6`.
- On Wayland, require XWayland, `xdg-desktop-portal`, and the backend matching the active desktop, normally `xdg-desktop-portal-gnome` or `xdg-desktop-portal-kde` on Debian/Ubuntu.
- Do not install packages, start privileged services, alter portal configuration, or change the login session without the user's explicit approval. Never launch AA.Annotate as root.

## Validate Completion

Read CLI stdout even when the exit code is nonzero.

A valid completion contains:

```text
SESSION_STATUS=completed
REVIEW_MD=<path>
ANNOTATIONS_JSON=<path>
```

- For `SESSION_STATUS=cancelled`, report cancellation and stop.
- For `SESSION_STATUS=error`, report `ERROR_MESSAGE` and stop.
- Treat missing status, missing completion paths, or missing exported files as a malformed handoff. Do not guess paths.

## Use the Handoff

Read `REVIEW_MD` first. Read `ANNOTATIONS_JSON` only when exact structured geometry is required.

For each capture:

- Treat the exported `Image:` as the source of truth after crop, privacy masking, and scaling.
- Prefer `Annotated image:` for a quick numbered overview.
- Apply each comment to its numbered rectangle within that capture; do not merge numbering across captures.
- Resolve relative artifact paths from the directory containing the review or JSON file.
- Refer to findings as `Capture N, annotation M`.
- If a comment conflicts with the image and rectangle, trust the visual evidence and state the ambiguity.

Read [references/handoff-format.md](references/handoff-format.md) only when exact crop, scaling, coordinate, snippet, or JSON interpretation is needed.

## Privacy Boundary

Use only exported handoff artifacts. Do not inspect `session.json`, `status.json`, private screenshots, thumbnails, original unscaled captures, or private working directories unless explicitly debugging a launch or malformed-handoff failure.

Do not create screenshots or annotation artifacts in the workspace unless the user explicitly requests it.
