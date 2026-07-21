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

Launch the resolved CLI and wait:

```powershell
& "<absolute-cli-path>" session --wait
```

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
