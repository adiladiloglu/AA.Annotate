---
name: aa-annotate
description: Launch the AA Annotate desktop overlay to collect visual screenshot annotations from the user and continue from the exported handoff. Use when a task needs the user to point at UI elements, mark screen regions, compare windows or tabs, crop screenshots, or provide visual context that text alone cannot describe.
---

# AA Annotate

Use AA Annotate when the task depends on the user's visual selection or comments on their screen.

## Start a Session

Launch the annotation window and wait while the user captures, crops, masks, annotates, and sends the session back.

Use the first available launch method:

1. Release-bundled Codex plugin: resolve the directory containing this `SKILL.md`; the plugin root is two directories up.

   ```powershell
   & "<plugin-root>\cli\aa-annotate.exe" session --wait
   ```

2. Registered on `PATH`:

   ```powershell
   aa-annotate session --wait
   ```

3. User-local install:

   ```powershell
   & "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait
   ```

4. Repo-local packaged output, from the repository root:

   ```powershell
   & ".\artifacts\publish\cli-win-x64\aa-annotate.exe" session --wait
   ```

Add `--output "<folder>"` only when the user asks to store final exported handoff files somewhere specific. Do not use or inspect the private working session folder; it is intentionally separate from the final export folder and is not part of the agent handoff.

Add `--session-root "<folder>"` only when debugging AA Annotate itself and you need to control where private working files are stored. For normal agent use, let AA Annotate keep private working files in the OS temp directory.

Use a shell/tool timeout longer than the expected human annotation time. If `--timeout-seconds` is omitted, the CLI uses 60 seconds. `--timeout-seconds` controls app inactivity and the CLI waiter's inactivity bound. User interaction with the app resets the inactivity timer. The outer shell/tool timeout is separate and must be longer than the AA Annotate inactivity window plus expected user time.

If the outer shell/tool times out before AA Annotate returns, report that the agent-side wait timed out or rerun the same launch method with a longer outer timeout. Do not infer completion from private session files.

If no executable path exists, tell the user the AA Annotate release bundle must be installed. A skill-only install is not enough.

## Read Completion

When the command exits, inspect stdout even if the process exit code is nonzero. Cancelled and errored sessions return a nonzero exit code.

Completed sessions print:

```text
SESSION_STATUS=completed
REVIEW_MD=<path>
ANNOTATIONS_JSON=<path>
```

If `SESSION_STATUS=cancelled`, stop and tell the user the session was cancelled.

If `SESSION_STATUS=error`, read `ERROR_MESSAGE` and report the failure. Do not continue as if annotations exist.

If stdout lacks `SESSION_STATUS`, if `SESSION_STATUS=completed` lacks `REVIEW_MD` or `ANNOTATIONS_JSON`, or if either exported file is missing, treat the handoff as malformed and report a launch/artifact error. Do not guess artifact paths.

## Interpret Artifacts

Read `REVIEW_MD` first. It is the normal agent entrypoint.

Use `ANNOTATIONS_JSON` only when exact structured metadata is needed.

For each `## Capture N` in `REVIEW_MD`:

- Treat it as a separate screen state.
- Open the `Image:` path when visual confirmation matters.
- Resolve relative image paths from the folder containing `REVIEW_MD`.
- Treat `Image:` as the primary source of truth. It is the exported image after crop normalization, privacy masking, and export scaling.
- If the user cropped, `Image:` is the cropped image; the full screenshot is not exported for normal use.
- If the user selected an export scale below 100%, `Image:` is downscaled and all exported coordinates use that scaled image size.
- Privacy masks appear as black rectangles labeled `Privacy mask`. Treat them as intentional redactions, not image defects.
- Prefer `Annotated image:` when you need a fast visual map of all numbered annotation boxes for the capture. It is the exported primary image with privacy masks already applied and annotation outlines/numbers drawn on top.
- Treat every annotation line as relative to the `Image:` path shown in that capture.
- If an annotation includes an indented `Image:` line, use that cropped annotation snippet for focused inspection of that one region. Privacy masks are applied before these snippets are generated, so snippets can contain masked areas.
- Apply the indented comment immediately below an annotation line to that numbered rectangle.
- Do not merge annotation numbers across captures.

Annotation lines use this form:

```text
Annotated image: <path>

1. x=<left>, y=<top>, width=<width>, height=<height>
   Image: <path>
   <comment>
```

Coordinate rules:

- For an uncropped capture, coordinates are relative to the full-screen `Image:`.
- For a cropped capture, coordinates are relative to the cropped `Image:`.
- For a scaled export, coordinates are relative to the scaled `Image:`.
- `Crop:` is metadata in original screenshot coordinates. Use it only when you must map a cropped annotation back to original screen coordinates.
- If `Crop:` describes a non-full crop but `Image:` points at an uncropped full screenshot, treat the handoff as inconsistent and report an artifact error instead of guessing coordinate basis.
- Export removes annotations fully outside the crop.
- Export clips annotations partly crossing the crop edge.
- Export renumbers remaining annotations sequentially inside each capture after filtering. These numbers may differ from temporary numbers the user saw while editing.
- Privacy masks are clipped to the exported crop and scaled with the exported image.

JSON rules:

- `captures[].screenshotPath` is the primary exported image path to inspect.
- `captures[].croppedPath` is present when the primary image came from a crop.
- `captures[].annotatedImagePath` is the exported overview image with privacy masks, annotation boxes, and numbers drawn on the primary image.
- `captures[].cropRect` records the crop in original screenshot coordinates.
- `captures[].annotations[].boxRect` follows the same coordinate basis as `REVIEW_MD`: it is relative to the primary exported image.
- `captures[].annotations[].imagePath` is the cropped image for that single annotation box.
- `captures[].privacyMasks[].boxRect` follows the same coordinate basis as `REVIEW_MD`: it is relative to the primary exported image.
- `captures[].exportScalePercent` records the scale applied to the exported image and coordinates.
- `captures[].screenshotPixelSize` describes the original capture size, not necessarily the dimensions of a cropped primary image.
- Resolve relative JSON image paths from the folder containing `ANNOTATIONS_JSON`.

## Use the Result

Continue the task using the annotation comments and numbered regions.

Reference annotations by capture and number, for example `Capture 2, annotation 1`.

If a comment and its box appear inconsistent, trust the visual `Image:` plus the box first, then state the ambiguity instead of inventing intent.

Do not read or rely on `session.json`, `status.json`, private screenshots, private thumbnails, original unscaled captures, or other private local state unless debugging launch/completion failure. During the annotation process, agents must not inspect the private working folder. Before completion, the only valid state source is CLI stdout. After completion, the normal handoff inputs are `REVIEW_MD` and `ANNOTATIONS_JSON`.

Do not inspect an original unscaled image when a masked or scaled export is available. Use an original unscaled image only when the exported image is unreadable for the task, the original is available through an explicit debugging path, and privacy constraints allow inspecting it.

Do not create screenshots or annotation files in the workspace unless the user explicitly asks.

If launch fails from a plugin, check whether `<plugin-root>\app\AA.Annotate.App.exe` exists. If launch fails from a user-local install, check whether `%LOCALAPPDATA%\AA.Annotate\app\AA.Annotate.App.exe` exists. For repo-local packaged output, check whether `artifacts\publish\app-win-x64\AA.Annotate.App.exe` exists. Use `AA_ANNOTATE_APP` only for custom app paths.
