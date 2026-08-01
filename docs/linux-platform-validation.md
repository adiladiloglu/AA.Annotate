# Linux Platform Validation

Date: 2026-08-01
Implementation plan: [linux-platform-plan.md](linux-platform-plan.md)

## Summary

The Linux implementation builds and its current complete automated suite passes on the
Windows host, Ubuntu GNOME VM, and Debian KDE VM: 284 tests per environment across App,
Core, CLI, and Linux platform projects. A self-contained Linux app/CLI bundle was
published on the GNOME VM and its executable/help contract was verified. Earlier guest
runs and their historical load-sensitive timeout remain recorded below.

GNOME Wayland graphical acceptance now passes in an unlocked local desktop session. The
final run exercised capture, annotation, comment editing, privacy masking, crop, finish
confirmation, and export. The toolbar remained visible throughout because active Linux
presentations now host it inside the fullscreen overlay rather than relying on compositor
ordering between independent top-level windows. First-run placement was also verified at
the display's top center.

The final UI regression build uses permanently separate passive and embedded toolbar
visuals. This removed the GNOME cross-`LayoutManager` crash. Current hands-on runs passed
on GNOME Wayland and KDE X11 for ordinary-click capture, selector foregrounds,
annotations, editor controls, and privacy masks. KDE Wayland remains untested, and GNOME
X11 still needs the final passive-topmost scenario repeated. No target is marked passed
merely because the application launched.

## Automated results

| Environment | Result | Evidence |
| --- | --- | --- |
| Windows host, exact 0.5.0 release candidate | passed, 284 tests | isolated `artifacts\release-0.5.0\windows-tests-final` run of `dotnet test AA.Annotate.slnx` |
| Ubuntu GNOME VM, exact 0.5.0 release candidate | passed, 284 tests; Core repeated 3 times; self-contained app/CLI publish and help passed | `/tmp/aa-annotate-0.5.0-validation-0806` |
| Debian KDE VM, exact 0.5.0 release candidate | passed, 284 tests; Core repeated 3 times | `/tmp/aa-annotate-0.5.0-validation-0806` |
| Windows host, earlier complete solution | passed, 265 tests | isolated `artifacts\test-final8` run of `dotnet test AA.Annotate.slnx` |
| Windows host, complete solution | passed, 253 tests | local `dotnet test AA.Annotate.slnx` |
| Ubuntu GNOME VM, exact final source on Wayland | passed, 253 tests | `E:\VMs\TestResults\aa-annotate-gnome-embedded-20260731T160800Z\run-evidence\tests.log` |
| Ubuntu GNOME VM, exact final source on Xorg | passed, 253 tests | `E:\VMs\TestResults\aa-annotate-gnome-xorg-20260731T163319Z\run-evidence\tests.log` |
| Ubuntu GNOME VM, shared Xorg repeat | passed, 253 tests | `E:\VMs\TestResults\aa-annotate-shared-gnome-xorg-20260731T212333Z\run-evidence\tests.log` |
| Debian KDE VM, shared X11 parallel solution run | 252 passed, 1 load-sensitive timeout; isolated test and 40-test Core rerun passed | `E:\VMs\TestResults\aa-annotate-shared-kde-x11-20260731T213115Z\run-evidence` |
| Ubuntu GNOME VM, self-contained app publish | passed | `E:\VMs\TestResults\aa-annotate-gnome-embedded-20260731T160800Z\run-evidence\publish.log` |
| Debian KDE VM, earlier exact source | passed, 251 tests | `E:\VMs\TestResults\aa-annotate-linux-20260731T082400Z` |
| Linux self-contained app and CLI publish | passed | KDE run `aa-annotate-linux-20260731T081900Z` and final GNOME run |
| Linux isolated install/reinstall/help | passed | `E:\VMs\TestResults\aa-annotate-linux-20260731T081900Z` |
| Windows 0.5.0 package rebuild | passed | `artifacts\release-0.5.0\dist\aa-annotate-0.5.0-win-x64.zip` |
| Linux installer safety-marker tests | passed | `E:\VMs\TestResults\aa-annotate-packaging-20260731T080500Z` |

Current host automated test totals:

- Core: 40
- CLI: 25
- Linux platform: 35
- App: 184
- Total: 284

## Graphical target matrix

| Target | Outcome | Evidence / first actionable blocker |
| --- | --- | --- |
| `linux-gnome-wayland` | **passed** | Full historical workflow passed in local Wayland session 58. The current final7 UI regression additionally passed top-center launch, ordinary capture, capture-menu foregrounds, annotation/editor, privacy mask, and process-survival checks. Evidence: `E:\VMs\TestResults\aa-annotate-gnome-embedded-20260731T160800Z` plus the final7 run documented below. |
| `linux-gnome-x11` | **failed** | Capture/edit/export functionality passed, but a shared repeat demonstrated that opening another application in the passive non-capture state can cover AA.Annotate. Evidence: `E:\VMs\TestResults\aa-annotate-shared-gnome-xorg-20260731T212333Z`; the actionable note is in Capture 2. |
| `linux-kde-wayland` | skipped | The VM had no active local `vmtest` desktop session. A local Plasma Wayland login is required before graphical validation. |
| `linux-kde-x11` | **passed for current UI regression** | Plain-click capture, capture menu, annotation/editor, privacy mask, topmost active toolbar, and color checks passed on final7 after clearing an externally stuck guest `Mod4` key state. The earlier full export evidence remains `E:\VMs\TestResults\aa-annotate-shared-kde-x11-20260731T213115Z`. |

The lab exposed one synthetic 1024x768 display, so these runs do not establish real
multi-monitor compatibility.

## GNOME Wayland graphical evidence

### Regression discovery and resolution

Two initial unlocked-session runs reproduced these issues:

- activating annotation, crop, or privacy-mask work could move the fullscreen surface
  and hide the independent toolbar window behind it;
- a completed export could race with a stale status update and leave `status.json` at
  `waiting`;
- first-run placement depended on the window manager accepting a requested position.

Evidence for the discovery runs is retained at:

- `E:\VMs\TestResults\aa-annotate-gnome-20260731T092000Z`
- `E:\VMs\TestResults\aa-annotate-gnome-review-20260731T093800Z`
- `E:\VMs\TestResults\aa-annotate-gnome-fix-20260731T094800Z`

The final architecture removes the GNOME stacking dependency: passive mode can use the
normal toolbar window, while an active fullscreen Linux presentation detaches the same
toolbar surface and hosts it in the overlay's own visual tree. Status mutations are
serialized, and placement keeps an explicit preferred position so the first-run
top-center request is not replaced by compositor-reported coordinates.

### Final acceptance run

The final self-test used `Linux-GNOME-Test`, GNOME Wayland local session 58. It verified:

1. The first toolbar appearance was horizontally centered near the display top.
2. Annotation mode left the toolbar visible.
3. A 300x200 annotation was drawn at x=300, y=300.
4. The comment editor and toolbar were simultaneously visible while entering
   `toolbar visible during comment`.
5. A 200x150 privacy mask was drawn at x=650, y=250; selecting it did not hide the
   toolbar.
6. Crop mode and crop-boundary interaction did not hide the toolbar.
7. Finish confirmation completed and the application exited normally.
8. `status.json`, `review.md`, and `annotations.json` agreed on completion and result
   paths.

The exported result contains a 1024x768 capture, a full-frame crop, the expected
annotation/comment, and privacy-mask data. Screenshots and machine-readable artifacts
are under:

`E:\VMs\TestResults\aa-annotate-gnome-embedded-20260731T160800Z`

The portal implementation in this run used one D-Bus connection to subscribe to the
predicted request object, issue the Screenshot request with a unique handle token,
receive the correlated success response, copy the local PNG, and decode the expected
1024x768 image.

## GNOME Xorg graphical evidence

The Xorg run used Ubuntu 24.04.4 local session 2 with `Type=x11`, `Remote=no`,
`Active=yes`, and `LockedHint=no`. The staged source archive SHA-256 was
`63f4a52d17e9a13be617b6a0fc57bc19f10b124e7755d3a9fd94640d3829a013`, identical to
the accepted Wayland build.

The primary workflow verified:

1. first-run toolbar placement at the display top center;
2. direct X11 capture of the 1024x768 `Virtual-1` display;
3. a 300x200 annotation at x=280, y=235;
4. a 200x150 privacy mask at x=650, y=200;
5. an adjusted crop of x=0, y=0, width=954, height=768;
6. toolbar visibility throughout annotation, mask, crop, and finish modes;
7. embedded-toolbar dragging and persisted normalized placement;
8. completed CLI, status, review, and annotation-document results;
9. exported images with crop/mask/annotation effects and no AA.Annotate chrome.

A focused second workflow exported the exact comment `gnome xorg comment works` and
completed with consistent CLI/status/document results. VMConnect did not reliably carry
held-button or text state into the guest, so a temporary run-local XTest helper supplied
only deterministic drag and keystroke gestures. It used already-installed X11 libraries,
made no package or baseline changes, and was removed after testing.

The Cancel command displayed its destructive confirmation correctly. The final discard
was not executed without a separate action-time confirmation; its disposable probe
processes were terminated during scoped cleanup. This does not affect the completed
capture/export acceptance result.

## Shared X11 passive-toolbar regression

A user-driven repeat compared GNOME Xorg and Plasma X11 with the identical source archive
SHA-256 `63f4a52d17e9a13be617b6a0fc57bc19f10b124e7755d3a9fd94640d3829a013`.
Both local sessions were active, unlocked, and non-remote. Each completed two captures,
crop, annotations/comments, privacy masking, finish, and export with consistent CLI and
status documents.

Both desktops reproduced the same blocker: while AA.Annotate is passive, before an active
capture presentation embeds the toolbar into the overlay, opening another application can
put that application above the detached toolbar. The active capture/editing toolbar stays
visible because it is embedded. This narrows the remaining defect to passive Linux window
integration/topmost enforcement rather than capture routing or the embedded-toolbar path.

Evidence:

- GNOME Xorg: `E:\VMs\TestResults\aa-annotate-shared-gnome-xorg-20260731T212333Z`
- KDE X11: `E:\VMs\TestResults\aa-annotate-shared-kde-x11-20260731T213115Z`

The KDE solution-wide automated run also exposed a synchronization-test reliability
issue: `ConcurrentActivityWriteCannotOverwriteCompletedStatus` timed out after five
seconds under parallel load. It passed in 149 ms in isolation, and all 40 Core tests
passed on immediate rerun. The original failure and both passing reruns are retained; the
test should be made deterministic rather than simply increasing its timeout.

## Final7 UI regression run

The exact deployed source archive was:

`artifacts\aa-annotate-linux-ui-final7-20260731T231959Z-source.tar.gz`

SHA-256:

`AF94291B5BBCE20EC216B9215D469E99455C37584AA7ADD4712CDCDA032809E8`

The run used the two approved VMs concurrently at the user's request. Both remained
running. GNOME used its live local Wayland desktop while the app probed the actual
Wayland socket; KDE used its local Plasma X11 session.

The regression fixed two independent issues:

- moving one `ToolbarSurface` between two live Avalonia top-levels crashed GNOME with
  `Attempt to call InvalidateArrange on wrong LayoutManager`; passive and embedded
  surfaces are now distinct permanent controls with synchronized state;
- KDE's passive toolbar now starts as an interactive normal X11 window and is activated
  when revealed, while preserving the above/stays-on-top and skip-taskbar behavior.

Hands-on Computer Use checks passed on both desktops:

1. toolbar appeared near the display's top center;
2. ordinary capture completed and exposed Capture 1;
3. the capture selector opened after leaving the active drawing mode;
4. capture number, remove-capture trash, and new-capture camera used light foregrounds;
5. annotation drag created a box and opened the comment editor;
6. the editor delete control used a visible light trash icon;
7. saving the annotation returned to the capture surface;
8. privacy-mask drag created and displayed the mask;
9. the completion control used a dark button surface with the green completion mark;
10. both app processes remained alive; GNOME did not reproduce the prior crash.

During KDE validation, ordinary clicks initially moved every application window and only
worked while Ctrl was held. A read-only `XkbGetState` probe proved that the guest's
physical modifier state was `base_mods=64` (`Mod4/Super`), while no mouse button was
held. KWin therefore treated every drag as its global move-window shortcut; adding Ctrl
prevented the exact shortcut from matching. Releasing only the stuck `Super_L` X11 key
state changed the mask to zero. An immediate ordinary click then captured successfully,
and the remaining AA.Annotate checks passed without Ctrl. No KDE setting or VM baseline
was changed. A temporary `xkbwatch` diagnostic was terminated after the check.

Final7 guest paths retained for review:

- GNOME: `/home/vmtest/vm-lab-runs/aa-annotate-linux-ui-final7-gnome-20260731T231959Z`
- KDE: `/home/vmtest/vm-lab-runs/aa-annotate-linux-ui-final7-kde-20260731T231959Z`

The fresh GNOME UI repeat runs as PID 15738 under the same GNOME run directory with a
separate `gnome-retest` session/export root. KDE final7 remains open as PID 12623. The
applications and both VMs were intentionally left running for continued user testing.
Collected status and capture evidence is available at
`E:\VMs\TestResults\aa-annotate-linux-ui-final7-20260731T231959Z`. The GNOME log was
empty (no crash output); the KDE launch did not create the expected `app.log`, so the
live process and session artifacts are the retained evidence for that run.

## Final11 interaction palette and capture-quality regression

The final interaction package was:

`artifacts\aa-annotate-linux-ui-hover-final11-20260801T003212Z-source.tar.gz`

SHA-256:

`260CDFB6FBCFBB7139BD9E7275730EF60D60E2B52AE223238809D3130EDA0238`

This package includes the shared application palette, explicit runtime pointer/pressed
selectors, higher-specificity icon-plus-danger states, parent-foreground icon bindings,
and the capture-quality presentation policy. The same application styles are used on
Windows and Linux; they do not depend on GNOME, KDE, or Windows button-foreground
defaults.

Windows was tested first. The isolated Windows App test run passed all 184 tests,
including palette contrast/state tests and active/passive capture-quality policy tests.
The preceding final10 regression ran every project sequentially: App 184, Core 40, CLI
25, and Linux 35 (284 total), all passing. The final11 delta changes only selector
specificity and passed a fresh App build/test.

The exact final11 package was then built and tested inside both approved running VMs:

| Target | Actual session | Automated result | Live presentation result |
| --- | --- | --- | --- |
| `linux-gnome-wayland` | Ubuntu 24.04.4, local active Wayland session 2 | App 184/184 passed; Release `linux-x64` publish passed | passive quality row absent; active-capture row present; crop, mask, annotate, and Complete hover foregrounds remained visible |
| `linux-kde-x11` | Debian 12, local active Plasma X11 session 5 | App 184/184 passed; Release `linux-x64` publish passed | passive quality row absent; active-capture row present; crop, mask, and annotate hover states remained visible |

The GNOME capture menu on final10 reproduced one last defect: the dynamic delete button
still inherited the generic teal hover instead of the danger hover. Final11 fixes this by
matching `iconButton` and `dangerIconButton` together, which also covers annotation and
privacy-mask delete buttons. The final11 build and tests validate the selector and palette
definitions. VMConnect click injection did not reopen that menu after the fresh process
launch, so the final danger-hover color still needs one short human pointer check; this is
recorded rather than overstated as a visual pass.

KDE's desktop-wide click obstruction remains external to AA.Annotate: ordinary VMConnect
clicks can be interpreted as window moves unless Ctrl is held, and it affects other KDE
windows as well. This limited the fresh-process menu/editor interaction but did not affect
package build, tests, process readiness, passive/active selector rendering, or hover
observation.

Retained guest runs and processes:

- GNOME: `/home/vmtest/vm-lab-runs/aa-annotate-linux-ui-hover-final11-gnome-20260801T003212Z`, PID 20481;
- KDE: `/home/vmtest/vm-lab-runs/aa-annotate-linux-ui-hover-final11-kde-20260801T003212Z`, PID 18675.

Both VMs and both final11 app processes were intentionally left running. The two verified
final10 app processes were stopped after exact executable-path checks. Collected logs,
session identity, hashes, process probes, and test/publish output are under
`E:\VMs\TestResults\aa-annotate-linux-ui-hover-final11-20260801T003212Z`.

## Package evidence

The Linux bundle validation confirmed:

- direct app and CLI apphosts are mode `0755`;
- the installer is user-scoped and requires no root;
- installed app/CLI `--help` works;
- the optional CLI symlink works;
- an ownership marker prevents replacement or recursive uninstall of an unrelated
  custom directory;
- the bundled skill is installed at the requested skills root.

The KDE guest did not have PowerShell Core, so `scripts/package-linux.ps1` itself was
syntax/YAML validated and its resulting layout was reproduced with the same published
artifacts for the isolated install test. The GitHub Linux release job remains the
authoritative execution environment for that PowerShell packaging script.

## Cleanup

- Evidence was copied to `E:\VMs\TestResults` before cleanup.
- The three exact application-specific staging directories for the final, transient,
  and preceding fix runs were removed from `/home/vmtest/vm-lab-runs` after verification.
- The exact GNOME Xorg staging directory was likewise removed after its 25-file host
  evidence bundle was validated.
- The shared GNOME and KDE X11 staging directories were removed after their host evidence
  bundles were validated.
- User-created output beneath `/home/vmtest/smoke` is preserved.
- Historical shared-run staging was cleaned as described above. The final7 GNOME and KDE
  staging directories and their app processes are intentionally retained and running for
  continued user testing.
- No checkpoint, VM configuration, switch, disk, firmware, desktop-session selection,
  authentication setting, or guest package baseline was changed.

## Remaining release gate

Before advertising validation across all supported Linux desktops:

1. repeat the final passive-toolbar open-another-window scenario on GNOME X11;
2. make the status-race concurrency test deterministic under parallel test load;
3. log into KDE Wayland and repeat the full workflow;
4. repeat on real multi-monitor hardware, including mixed scale factors;
5. verify toolbar placement/drag restoration and absence of AA.Annotate chrome from
   captured images on each remaining compositor/session combination.
