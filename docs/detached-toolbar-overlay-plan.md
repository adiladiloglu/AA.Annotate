# Detached Toolbar and Annotation Overlay

## Goal

Replace the shape-shifting single window with two coordinated top-level windows:

- `ToolbarWindow`: a transparent, size-to-content, movable command surface whose
  position survives mode changes, captures, display changes, and app restarts.
- `MainWindow`: the display-sized annotation overlay, hidden while the app is
  passive and shown only for annotation, crop, privacy masking, comment editing,
  idle warning, and confirmation.

The annotation/session workflow remains in `MainWindow` during this change. The
window split must not become a simultaneous rewrite of the existing workflow.

## Reviewed design decisions

The first draft was reviewed for Avalonia/Windows behavior and regression risk.
The final plan incorporates these revisions:

1. A single presentation policy and coordinator own all `Show`, `Hide`, enable,
   activation, and overlay-placement decisions. Mode handlers do not manipulate
   top-level windows directly.
2. Avalonia owned-window behavior was treated as a go/no-go hypothesis.
   Non-interactive Windows validation proved that hiding the passive owner also
   hides its owned toolbar. The toolbar is therefore an independent topmost
   window with lifecycle controlled by the coordinator. Explicit native
   no-activate z-order integration remains a fallback only if broader Windows
   validation finds ordering instability; activation loops are not used.
3. Comment editing, idle warning, and confirmation stay in the overlay. Only
   command chrome and toolbar-anchored popups move to `ToolbarWindow`.
4. Capture is a transaction: close popups, hide both top levels, yield through
   the render queue, wait the bounded capture delay, capture, then restore the
   desired presentation from logical state.
5. User activity is observed from both top levels so toolbar interaction resets
   the existing idle timer.
6. Placement uses physical screen work-area pixels and measured toolbar size.
   Saved coordinates are normalized and clamped on restore, save, size change,
   and display-topology change.
7. Persistent monitor matching uses display name and saved bounds in this
   iteration. Native device-path identity, docking/snapping, keyboard nudging,
   and a follow-capture-display preference are deferred until the two-window
   contract is stable.

## State contract

`OverlayPresentationPolicy` returns one value containing:

- overlay visibility;
- capture-surface visibility;
- toolbar visibility;
- toolbar enabled state.

| State | Overlay | Capture surface | Toolbar |
| --- | --- | --- | --- |
| Idle and toolbar popup states | Hidden | Hidden | Visible and enabled |
| Capturing | Hidden | Hidden | Hidden |
| Annotation, crop, privacy mask, comment | Visible | Visible | Visible and enabled |
| Idle warning and finish/cancel confirmation | Visible | Context-dependent | Hidden |
| Terminal/closing | Hidden | Hidden | Hidden |

When the overlay is visible, it intentionally handles its whole surface. Idle
click-through no longer depends on a screen-sized transparent HWND because the
overlay is hidden.

## Implementation sequence

### 1. Add pure policy and persistence seams

Add:

- `OverlayPresentation` and `OverlayPresentationPolicy`;
- `ToolbarPlacement`, `ToolbarDisplay`, and `ToolbarPlacementProjector`;
- `UiSettings` and `UiSettingsStore`.

Tests cover the full presentation matrix, normalized placement round trips,
negative monitor origins, taskbar work areas, 100–200% scaling, oversized
toolbars, invalid values, missing/corrupt settings, and atomic overwrite.

### 2. Extract toolbar chrome

Add `ToolbarWindow.axaml` and code-behind. Move these existing controls without
duplicating their styles or behavior:

- `FloatingCommandBar`;
- `CaptureScaleSelector`;
- display and capture dropdowns;
- capture status popup;
- about popup.

Add a dedicated grip to `FloatingCommandBar` and call `Window.BeginMoveDrag`
from the original left-button press. Keep the existing monitor button dedicated
to selecting the capture display.

The toolbar exposes temporary internal accessors for its controls and popup
state. `MainWindow` delegates through them, preserving the existing command
wiring while the window boundary changes.

### 3. Add placement management

The toolbar is transparent, chromeless, non-resizable, topmost,
`ShowInTaskbar=False`, and `SizeToContent=WidthAndHeight`.

`ToolbarPlacementController`:

- restores after the window is measured;
- identifies the containing display from current screen data;
- stores normalized X/Y over the available work-area travel distance;
- debounces native `PositionChanged` saves;
- re-clamps when toolbar size or `Screens.Changed` changes;
- uses the primary display and a 24-DIP inset for first launch;
- guarantees a reachable toolbar after monitor removal or corrupt settings.

Settings live at `%LocalAppData%\AA.Annotate\ui-settings.json`, separate from
private session and exported annotation data.

### 4. Centralize two-window presentation

Add `OverlayWindowCoordinator`. It is the only component that:

- positions the overlay to selected display bounds;
- shows or hides the overlay;
- shows, hides, or enables the toolbar;
- closes toolbar popups before hiding;
- applies state idempotently.

Keep `MainWindow` as the desktop lifetime main window. Show `ToolbarWindow` as
an independent modeless top-level window after startup; the coordinator closes
it during terminal shutdown and routes its OS close command through the session
cancellation path. Only the toolbar occupies the Windows topmost band. The
activated fullscreen overlay remains a normal top-level window, preventing
pointer interaction with crop and privacy-mask surfaces from covering the
toolbar. Suppress native borders on both handles. The overlay remains in
`WindowState.Normal` and is sized directly to `Screen.Bounds / Screen.Scaling`.

Remove compact footprint measurement, counter-translation, and mode-driven
native window resizing. Remove transparent hit-test integration only after the
idle overlay is confirmed hidden in all passive states.

### 5. Make capture coordinated

Before capture:

1. snapshot logical UI state;
2. close toolbar popups and tooltips;
3. apply the capturing presentation, hiding both top levels;
4. yield through the dispatcher render priority;
5. wait the existing bounded compositor/capture delay;
6. re-resolve the selected display and capture it.

After success, cancellation, or failure, restore presentation from the resulting
logical state. Never blindly restore previous `IsVisible` flags, and never move
the toolbar as part of capture recovery.

Closing the toolbar through OS commands is intercepted and routed through the
same session cancellation path. Terminal shutdown closes both windows and
flushes the final placement write.

### 6. Remove obsolete infrastructure

After all callers migrate:

- retire compact/fullscreen geometry branches;
- retire toolbar rectangles from overlay hit testing;
- remove `EnableTransparentHitTest` and its Windows WndProc hook if repository
  search confirms no remaining consumer;
- update the old crop/fullscreen tests to assert presentation state instead.

## Verification gates

1. **Baseline:** full solution build and tests pass before changes.
2. **Policy gate:** presentation, placement, and settings tests pass before XAML
   extraction.
3. **Window gate:** both XAML windows build and can be created without leaking
   timers or closing the session unexpectedly.
4. **Capture gate:** automated coordinator tests prove popup-close → hide both →
   capture → logical-state restore ordering for success, cancellation, and
   failure.
5. **Topology gate:** placement tests cover negative coordinates, mixed scaling,
   taskbar work areas, display removal, and corrupt saved state.
6. **Final:** full build/tests pass; repository search finds no unintended
   compact-window, fullscreen-switch, or transparent-hit-test callers.

Non-interactive Windows smoke checks should validate that:

- idle leaves only the toolbar visible;
- active modes place the overlay on the selected display;
- the toolbar remains above the overlay without stealing the first annotation
  click;
- capture contains neither top-level window nor popup;
- toolbar close cannot orphan a session.

If independent topmost ordering fails this gate on broader Windows coverage,
add an explicit no-activate Windows z-order operation. Do not compensate with
repeated activation.

## Acceptance criteria

- The toolbar can be moved freely and remains at the same desktop location
  through idle, annotation, crop, privacy masking, capture success/failure, and
  restart.
- Selecting a capture display moves only the overlay/capture target.
- The overlay exactly matches the selected display and preserves screenshot
  coordinate fidelity.
- Idle mode has no display-sized window consuming input.
- Modal states cannot trigger commands behind their dimmer.
- Captured images exclude all AA Annotate windows.
- Missing, corrupt, or inaccessible UI settings never block startup.
- A removed or reconfigured display cannot strand the toolbar off-screen.
- Existing annotation, session, export, CLI, and capture behavior remains
  covered and passing.
