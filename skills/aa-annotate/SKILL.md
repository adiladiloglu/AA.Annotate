---
name: aa-annotate
description: Launch the AA Annotate desktop overlay and continue from the user's exported visual handoff. Use when the user needs to point at screen regions, compare windows or states, crop screenshots, redact private areas, or provide visual context that text alone cannot express.
---

# AA Annotate

Collect visual input from the user, validate the exported handoff, and continue the original task.

## Launch

Resolve one CLI path without searching the filesystem:

1. For a plugin-provided skill, use `<plugin-root>/cli/aa-annotate.exe`, where `<plugin-root>` is two directories above this `SKILL.md`.
2. For a standalone Windows installation, use `%LOCALAPPDATA%/AA.Annotate/cli/aa-annotate.exe`.
3. Use a repo-local executable only when explicitly debugging AA Annotate.

Do not use `Get-Command`, scan directories, rebuild the repository, or set `AA_ANNOTATE_APP` for a normal installed session. If neither installed path exists, report that the AA Annotate application bundle is missing; a skill file alone is insufficient.

Choose the default scale using the guidance below, then launch the resolved CLI and wait. This example uses `75` for typical application UI:

```powershell
& "<absolute-cli-path>" session --wait --default-scale 75
```

- Replace `75` with the chosen integer percentage. Valid values are `20` through `100`.
- Invoke the executable directly with the shell call operator in the form shown. Do not wrap it in `Start-Process`, `System.Diagnostics.Process`, `ProcessStartInfo`, a polling loop, or a timing helper.
- In particular, do not use `ProcessStartInfo.ArgumentList`; it is `$null` in Windows PowerShell 5.1 on .NET Framework and the launch will fail before the CLI starts.
- Choose `--default-scale` from the expected content:
  - `100` for source code, terminals, dense tables, small text, fine visual defects, or uncertain content.
  - `75` for typical application UI where labels must remain readable.
  - `50` for broad layouts, dashboards with large labels, photos, or composition feedback.
  - `33` or `25` only when large shapes and overall placement matter more than text or fine detail.
- Prefer the higher scale when uncertain. Adjust future defaults using readability of earlier AA Annotate handoffs in the current task; do not reduce scale after an earlier handoff was hard to read.
- The default applies only to newly created captures. Tell the user they can override quality independently at the top of every capture; the capture's current selection controls its live preview and exported dimensions.
- Add `--output "<folder>"` only when the user requests a specific export destination.
- Add `--session-root "<folder>"` only when debugging AA Annotate itself.
- Use a tool timeout that covers expected user interaction plus the app's inactivity warning period.
- Keep `--wait` for real annotation sessions. For a short launch-only diagnostic, explain that completion will not be collected before omitting it.
- Do not inspect private session folders while waiting.

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
