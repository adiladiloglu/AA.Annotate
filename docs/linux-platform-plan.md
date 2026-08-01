# Linux Platform Extension Plan

Status: implemented; current GNOME Wayland and KDE X11 UI regressions passed
Target release scope: Linux x64 desktop, packaged as a self-contained .NET application
Primary validation lab: `Linux-GNOME-Test` and `Linux-KDE-Test` under `E:\VMs`

## 1. Outcome

Extend AA.Annotate from a Windows-only desktop overlay into a Linux desktop application
that preserves the existing agent-facing session contract:

- launch through `aa-annotate session --wait`;
- capture the selected display without AA.Annotate chrome in the image;
- crop, mask, annotate, scale, and export exactly as on Windows;
- print `SESSION_STATUS`, `REVIEW_MD`, and `ANNOTATIONS_JSON` from the CLI;
- keep private working captures private;
- install as a user-scoped application and bundled Codex skill.

The first Linux release will support:

| Environment | UI backend | Capture backend | Release expectation |
| --- | --- | --- | --- |
| GNOME on X11 | Avalonia X11 | direct X11 capture | implemented; repeat final passive-topmost acceptance |
| KDE Plasma on X11 | Avalonia X11 | direct X11 capture | implemented; current UI regression passed |
| KDE Plasma on Wayland | Avalonia through XWayland | XDG Screenshot portal | supported after VM acceptance |
| GNOME on Wayland | Avalonia through XWayland | XDG Screenshot portal | implemented and accepted in the lab |

Avalonia 11 targets X11 on Linux and therefore runs through XWayland in a Wayland
desktop session. The fullscreen overlay remains an absolute top-level window, but the
active toolbar surface is embedded inside that overlay on Linux so correctness does not
depend on the compositor ordering two independent top-level windows. Passive mode can
still use the detached toolbar window. Native Wayland is explicitly out of scope for
this release.

The application must never fall back to reading the XWayland root window in a Wayland
session: doing so would omit native Wayland windows and create a misleading capture.

## 2. Evidence and constraints

### Existing architecture

- `AA.Annotate.Core` is platform-neutral.
- `AA.Annotate.Platform` already defines capture and window-integration contracts.
- `AA.Annotate.Platform.Windows` cleanly contains the Windows-only implementation.
- `AppPlatformServices` is the single platform dispatch point.
- The CLI already resolves Linux apphosts, executes them directly, and understands Linux
  runtime identifiers.
- The capture workflow already hides the overlay and toolbar, flushes the compositor, and
  waits before invoking the platform capture service.
- The portable image/export pipeline uses SkiaSharp rather than `System.Drawing`.

### Linux-specific constraints

- X11 allows direct capture of a selected root-window rectangle.
- Wayland prevents arbitrary clients from reading the desktop; capture must use a portal.
- XDG Screenshot portal responses are asynchronous and return a URI only after a
  `org.freedesktop.portal.Request.Response` signal.
- A portal parent window under X11 is `x11:<hex XID>`.
- Portal response `0` means success, `1` means user cancellation, and `2` means another
  backend termination.
- Early GNOME 46 probes returned response `2`, but the final same-connection client
  received a correlated success response and passed an unlocked graphical workflow.
- The VM lab exposes one synthetic 1024x768 display. It cannot prove real multi-monitor
  behavior.
- Graphical tests require a genuine active local session. Authentication and session
  selection must remain manual.

### Privacy constraint

The current default `/tmp/AA.Annotate` location can be too permissive on a multi-user
Linux system. Private sessions and crash diagnostics must use the per-user
`XDG_RUNTIME_DIR` when it is valid, with a user-private fallback. Working directories
must be mode `0700` and private files mode `0600`. Explicit export folders remain under
the user's chosen permissions.

## 3. Architecture

Add `AA.Annotate.Platform.Linux` with the following responsibilities.

### 3.1 Session detection

`LinuxDesktopSession` reads injected environment values:

- `XDG_SESSION_TYPE`;
- `DISPLAY`;
- `WAYLAND_DISPLAY`;
- `XDG_CURRENT_DESKTOP`.

It selects:

- direct X11 capture only when the actual desktop session is X11;
- portal capture when the actual desktop session is Wayland;
- an actionable unavailable result when neither environment is usable.

Detection is case-insensitive, deterministic, and unit tested. It must not infer the
session from the mere presence of XWayland's `DISPLAY`.

### 3.2 Capture routing

`LinuxScreenCaptureService` implements `IScreenCaptureService` and delegates to:

- `X11ScreenCaptureService` for X11 sessions;
- `XdgPortalScreenCaptureService` for Wayland sessions.

Common behavior:

- require a preferred display;
- reject cursor inclusion until explicitly implemented;
- honor cancellation before capture and before commit;
- write into a temporary sibling file and atomically move it into place;
- remove partial output on cancellation or failure;
- validate that the output is a readable, non-empty PNG;
- return authoritative pixel dimensions;
- map permission, cancellation, disconnected-display, unavailable-backend, and unexpected
  failures into the existing `ScreenCaptureOutcome` values.

### 3.3 Direct X11 capture

Use `libX11` through a narrow internal native facade:

1. open the current display;
2. get the root window;
3. flush/synchronize pending X operations;
4. call `XGetImage` for the selected `DisplayDescriptor.Bounds`;
5. convert the returned XImage into BGRA pixels using its byte order, stride,
   bits-per-pixel, and RGB masks;
6. encode the result as PNG with SkiaSharp;
7. destroy the XImage and close the display in `finally`.

The converter is pure managed code and receives a copied byte buffer plus explicit image
metadata. Unit tests cover:

- little- and big-endian byte order;
- 24- and 32-bit storage;
- row padding;
- common RGB/BGR masks;
- invalid sizes, masks, and truncated buffers.

No command-line screenshot utility is a runtime dependency.

### 3.4 XDG Screenshot portal

Use one D-Bus connection for the method call and response subscription to avoid the portal
request race. `Tmds.DBus` is the only new managed runtime dependency; it provides the
typed proxy layer over the same D-Bus request/response protocol.

Flow:

1. connect to the user session bus;
2. subscribe to `org.freedesktop.portal.Request.Response`;
3. call `org.freedesktop.portal.Screenshot.Screenshot` with a unique `handle_token`,
   `interactive=false`, and a valid X11 parent identifier when available;
4. correlate only the returned request object path;
5. wait with cancellation and a bounded timeout;
6. on response `0`, require a local `file:` URI and copy it into the private session;
7. on response `1`, return cancelled;
8. on response `2`, return unavailable with a backend-specific diagnostic;
9. close a pending portal request when application cancellation wins.

The portal result file is treated as untrusted input:

- reject non-file URIs;
- reject missing or unreadable files;
- validate it as an image;
- never delete a source file unless the portal documents it as application-owned and the
  implementation created it in an application-controlled location.

Portal version 2 cannot request a specific display. For this release, Wayland acceptance
is limited to a single active display. If multiple displays are reported, the app returns
an explicit unavailable result rather than silently exporting the wrong screen.

### 3.5 Window integration

Keep Avalonia's `SystemDecorations=None` and `Topmost=True` as the primary Linux behavior.
The implemented Linux integration:

- performs an X11 synchronization for `FlushCompositor`;
- validates that native handles are X11 handles before using them;
- uses two permanently owned toolbar surfaces: one belongs to the passive toolbar window
  and one belongs to the fullscreen overlay;
- synchronizes command state and popup state between those surfaces, while opening
  popups only on the currently active surface;
- positions and drags the embedded toolbar in overlay coordinates, while persisting a
  compositor-independent preferred position;
- initializes the first-run preferred position at the display's top center;
- serializes session-status mutations so a stale waiting update cannot overwrite a
  completed export.

Permanent ownership avoids Avalonia's `Attempt to call InvalidateArrange on wrong
LayoutManager` crash, which occurred when the same visual control was moved between two
live top-level windows. Embedding keeps active editing independent of compositor stacking.
The passive X11 toolbar advertises an interactive normal window type plus above/stays-on-
top state, and is activated when revealed so KDE does not treat it as passive chrome.
GNOME Xorg still needs a final repeat of the open-another-window scenario with this exact
implementation before that distinct target is marked accepted.

Toolbar presentation and color are application-owned rather than inherited from the host
desktop theme. Both permanent toolbar surfaces use the same high-contrast interaction
palette for icon, text, panel-item, destructive, confirmation, hover, and pressed states.
This keeps foregrounds readable on Windows, GNOME, and KDE even when the desktop color
scheme differs. Every icon path derives its foreground from its owning button, including
capture/new-capture, annotation delete, and privacy-mask delete controls. Compound
icon-plus-danger selectors take precedence over the generic icon hover state.

The capture-quality row is presentation-dependent. It is hidden while the application is
passive or no current capture exists, and is shown only while a current capture is active
on the capture surface. The two toolbar surfaces apply this rule independently whenever
the current presentation or capture changes, so a stale capture model cannot leave the
quality selector visible in passive mode.

The native-window contract will retain handle descriptors when passing handles to platform
code, preventing accidental interpretation of a non-X11 pointer as an XID.

## 4. Application integration

### Platform registration

- Reference `AA.Annotate.Platform.Linux` from the app.
- Select Linux services only under `OperatingSystem.IsLinux()`.
- Preserve Windows behavior unchanged.
- Keep unsupported operating systems on the existing unavailable/no-op path.

### User-facing capture failures

Pass the safe platform error detail into the capture feedback UI. Messages must distinguish:

- missing X11 display;
- missing session D-Bus/portal;
- user cancellation;
- denied portal permission;
- multiple displays unsupported on the Wayland MVP;
- portal backend termination, including the known GNOME lab defect.

Do not expose filesystem paths, D-Bus addresses, stack traces, or other sensitive
diagnostics in the UI. Detailed failures belong in the existing crash/debug log.

### Private paths

Add a shared application-data path resolver:

- Linux private runtime: valid `XDG_RUNTIME_DIR/aa-annotate`;
- Linux settings: `XDG_CONFIG_HOME/aa-annotate`, falling back to
  `~/.config/aa-annotate`;
- Windows paths remain unchanged;
- all private runtime directories and files receive restrictive Unix modes.

## 5. Test strategy

### Automated tests

Add a Linux platform test project and keep tests runnable on Windows through injected
facades.

Required test groups:

1. desktop-session detection and backend routing;
2. X11 pixel conversion and error mapping;
3. portal request/response correlation, URI validation, cancellation, timeout, and cleanup;
4. platform-service selection;
5. native-handle descriptor preservation;
6. private path selection and Unix modes;
7. CLI executable discovery and `ProcessStartInfo` behavior with explicit platform inputs;
8. Linux package layout and executable names.

All existing Windows tests must continue to pass.

### VM acceptance matrix

Each VM run uses a unique directory:

`/home/vmtest/vm-lab-runs/aa-annotate-linux-<UTC timestamp>`

Evidence is collected under:

`E:\VMs\TestResults\aa-annotate-linux-<UTC timestamp>`

Run sequentially:

| Target | Required result |
| --- | --- |
| `linux-gnome-x11` | repeat passive topmost scenario on the final dual-surface build |
| `linux-kde-x11` | capture, selector, annotation, privacy mask, colors, and ordinary click pass |
| `linux-kde-wayland` | full pass if the portal remains healthy |
| `linux-gnome-wayland` | full pass (achieved in final lab run) |

Per-target checks:

1. record OS, desktop, local/remote state, and actual session type;
2. hash the deployed package;
3. run all test assemblies;
4. verify app and CLI `--help`;
5. launch the installed CLI with a distinct session/export root;
6. verify toolbar render, drag, restore, borderless appearance, and topmost behavior;
7. verify the capture-quality row is absent in passive mode, present for an active capture,
   and absent again when capture presentation is no longer active;
8. capture a known high-contrast desktop scene;
9. prove AA.Annotate chrome is absent from the capture;
10. verify PNG dimensions match the selected display;
11. crop, add a privacy mask, add a numbered annotation/comment, and scale the export;
12. verify visible, distinct hover/pressed states for every toolbar, selector, dropdown,
    comment-editor, confirmation, and destructive control;
13. complete the session and validate all CLI output paths and exported artifacts;
14. inspect the exported image to confirm crop, permanent mask, annotation, and scaling;
15. cancel a second session and verify terminal status and process exit;
16. verify private directory/file modes and absence of leaked capture processes;
17. collect app logs, portal service status, and the first actionable failure;
18. remove only processes and files created for the run;
19. gracefully stop a VM when it was started for this run.

Automation may drive build, deployment, launch, file probes, and log collection. Visual
interaction and portal consent remain user actions when the desktop requires them.

## 6. Packaging and distribution

Produce `aa-annotate-<version>-linux-x64.tar.gz` with:

- `app/AA.Annotate.App`;
- `cli/aa-annotate`;
- bundled `skills/aa-annotate`;
- `install.sh`, `uninstall.sh`, Linux README, manifest, and license.

The archive must preserve executable bits. The default installation is:

- app/CLI: `~/.local/opt/aa-annotate`;
- optional CLI link: `~/.local/bin/aa-annotate`;
- skill: `${CODEX_HOME:-~/.codex}/skills/aa-annotate`.

The installer:

- is non-root and idempotent;
- replaces only AA.Annotate-owned destinations;
- preserves unrelated user files;
- verifies executability and required package files;
- prints exact installed paths and an invocation example.

Document runtime libraries:

- `libx11-6`;
- `libice6`;
- `libsm6`;
- `libfontconfig1`;
- a working XDG desktop portal for Wayland capture.

Extend the release workflow with a Linux build/test/package job and a final release job
that uploads both Windows and Linux assets. Do not let independent jobs race to create the
same release.

Update the bundled skill with OS-specific fixed paths and shell examples while preserving
its no-search/no-rebuild rule.

The bundled skill now also documents Linux executable checks, X11/Wayland session
handling, portal consent and cancellation, and GNOME portal failure reporting. These
remarks are part of the implementation rather than VM-lab infrastructure: the generic
lab skill remains application-neutral.

### Bundled skill Linux remarks

The shipped `skills/aa-annotate/SKILL.md` is part of the Linux release gate, not a
documentation follow-up. Its launch section must branch by operating system:

- Linux plugin bundle: `<plugin-root>/cli/aa-annotate`;
- Linux standalone install: `~/.local/opt/aa-annotate/cli/aa-annotate`;
- Windows paths remain unchanged;
- invoke the Linux CLI directly from a POSIX shell and retain `session --wait`;
- never suggest the Windows `.exe`, PowerShell call operator, `%LOCALAPPDATA%`, or
  `%USERPROFILE%` in the Linux branch;
- treat a present but non-executable Linux CLI as an incomplete installation and report
  the exact fixed path rather than scanning the filesystem;
- retain the existing rule that normal skill use must not rebuild the repository or set
  `AA_ANNOTATE_APP`.

The skill must also tell an agent what is normal on Linux:

- a Wayland desktop may show a system screenshot-consent dialog;
- user cancellation of that dialog is a cancelled capture, not permission to try an
  unsafe fallback;
- the agent must keep waiting while the user is interacting with the app or portal;
- portal/backend failures should be reported as unavailable without attempting an unsafe
  fallback; GNOME Wayland is supported when the standard portal succeeds;
- exported handoff validation and privacy boundaries are identical on Windows and Linux;
- private runtime screenshots must not be inspected while the CLI is waiting.

## 7. Implementation order

1. Make existing CLI tests platform-explicit and establish a green baseline.
2. Add private Linux path and Unix-permission behavior with tests.
3. Add the Linux project, environment detection, capture router, and service registration.
4. Implement and unit-test X11 conversion/capture.
5. Preserve native handle descriptors and add minimal Linux X11 synchronization.
6. Implement and unit-test the XDG portal client.
7. Surface actionable safe failure details.
8. Build and test on the Windows host.
9. Publish and stage a Linux x64 artifact.
10. Run GNOME/KDE VM validation sequentially, starting with each VM's active session.
11. Fix application-owned failures and repeat the affected target.
12. Add installer, package, skill, README, and release workflow changes.
13. install the final package in the VMs and repeat the end-to-end acceptance path.

## 8. Review and revision record

The initial concept was “add a Linux screenshot service.” Architecture review found that
this was incomplete in five important ways:

1. Wayland capture cannot use X11 even though Avalonia itself runs through XWayland.
2. GNOME and KDE portal behavior differs in the actual lab.
3. Linux private temp permissions are part of the product's privacy promise.
4. Linux CLI/package support must be tested from the installed bundle, not only `dotnet run`.
5. Multi-window overlay behavior and native handle types require explicit validation.

The plan was revised to:

- route by the real desktop session;
- use direct X11 capture and a standard Wayland portal path;
- mark the current GNOME Wayland portal as a measurable external blocker rather than
  fabricating a fallback from the user's Pictures directory;
- preserve native handle descriptors;
- include Unix permissions, packaging, skill updates, and installed-bundle tests in the
  MVP;
- constrain Wayland to one display until multi-monitor semantics can be proven;
- define pass/blocked criteria for every lab target.

Implementation validation later showed that the same-connection client received a
correlated success response from the live GNOME Wayland portal. An unlocked full UI run
then exposed a separate compositor issue: GNOME could place an activated fullscreen
overlay over the detached topmost toolbar. A transient-owner revision reproduced the
failure, so the plan was revised again to embed a toolbar surface in the active Linux
overlay. Reparenting one live control later caused an Avalonia cross-`LayoutManager`
crash on GNOME, so the solid implementation uses two permanently owned surfaces and
synchronizes their state. The final unlocked run passed annotation, comment, privacy-mask,
crop, finish, and export with the toolbar continuously visible. It also verified exact
first-run top-center persistence and agreement between session status and exported
documents.

The subsequent GNOME Xorg run used the identical source archive in an active, unlocked,
non-remote X11 session. It passed direct capture, crop, mask, annotation, comment export,
toolbar visibility/drag persistence, finish, and artifact validation. Visual inspection
confirmed that AA.Annotate chrome was absent from the exported images.

A later user-driven repeat added a missing acceptance case: opening an unrelated
application while AA.Annotate is passive. Both GNOME Xorg and Plasma X11 allowed the
unrelated window above the detached toolbar. Active capture/editing remains correct
because the toolbar is embedded in the fullscreen overlay. The plan is therefore revised
again: the passive toolbar now uses interactive normal-window semantics plus explicit
above/stays-on-top state. The current KDE X11 UI regression passed. Its initial
Ctrl-required behavior was not application-specific: XKB reported a stuck physical
`Mod4/Super` modifier, causing KWin to move every application window; releasing that one
guest key state restored ordinary clicks. GNOME X11 still needs the final open-window
repeat. The earlier KDE run also revealed that the status-race test's
five-second blocking synchronization is load-sensitive; it must be made deterministic
even though the test and full Core suite passed immediately on rerun.

A final interaction-palette review found that Avalonia's host-theme button states could
override class-based pseudo selectors. The implementation was revised to use explicit
pointer/pressed property selectors at runtime, plus higher-specificity compound selectors
for icon danger buttons. A separate presentation policy now gates the capture-quality row
on both current-capture existence and active capture-surface visibility. These are shared
application rules, so Windows and Linux receive the same UI behavior; the VM matrix still
checks both GNOME Wayland and KDE X11 for compositor-specific rendering differences.

## 9. Definition of done

Linux support is ready to describe as released only when:

- host and Linux automated tests pass;
- the Linux package installs without root;
- GNOME X11 completes the full workflow and keeps its passive toolbar above newly opened
  application windows;
- KDE X11 completes the same workflow and passive-toolbar stacking scenario;
- KDE Wayland completes the workflow through the portal or is removed from the advertised
  support matrix;
- GNOME Wayland completes the workflow through the portal (achieved in the current lab);
- no target is called passed merely because the process launched;
- private working artifacts have restrictive permissions;
- Windows behavior and packaging remain green;
- the capture-quality selector never appears in passive mode and appears for an active
  capture;
- hover, pressed, destructive, and confirmation states retain readable foregrounds on
  Windows, GNOME, and KDE;
- all VM evidence identifies the actual desktop/session and is stored under
  `E:\VMs\TestResults`.

## 10. Deferred work

- native Avalonia Wayland backend and a compositor-native overlay/window model;
- ScreenCast/PipeWire fallback for broken Screenshot portal backends;
- real multi-monitor Wayland capture and display selection;
- ARM64 packages;
- RPM, DEB, Flatpak, or AppImage distribution;
- automated accessibility testing;
- macOS support.

## 11. Primary references

- XDG Screenshot portal:
  <https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Screenshot.html>
- XDG portal request lifecycle:
  <https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Request.html>
- XDG portal window identifiers:
  <https://flatpak.github.io/xdg-desktop-portal/docs/window-identifiers.html>
- Avalonia desktop Linux:
  <https://docs.avaloniaui.net/docs/platform-specific-guides/linux>
- VM inventory:
  `E:\VMs\.agents\skills\use-cross-platform-vm-lab\references\lab-inventory.md`
- VM installation evidence:
  `E:\VMs\Documentation\linux-test-lab-installation-report.md`
