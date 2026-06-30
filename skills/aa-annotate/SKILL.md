---
name: aa-annotate
description: Launch the AA Annotate desktop overlay to collect visual screenshot annotations from the user and continue from the exported handoff. Use when a task needs the user to point at UI elements, mark screen regions, compare windows or tabs, crop screenshots, or provide visual context that text alone cannot describe.
---

# AA Annotate

Use AA Annotate when the task depends on the user's visual selection or comments on their screen.

## Start a Session

Tell the user the annotation window is opening and that you will wait while they capture, crop, annotate, and send the session back.

Use the first available launch method:

1. Release-bundled Codex plugin: resolve the directory containing this `SKILL.md`; the plugin root is two directories up.

   ```powershell
   & "<plugin-root>\cli\aa-annotate.exe" session --wait
   ```

2. User-local install:

   ```powershell
   & "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait
   ```

3. Repo-local packaged output, from the repository root:

   ```powershell
   & ".\artifacts\publish\cli-win-x64\aa-annotate.exe" session --wait
   ```

4. Registered on `PATH`:

   ```powershell
   aa-annotate session --wait
   ```

5. Development source fallback, from the repository root. Build first so the CLI can resolve the desktop app from the app project's output.

   ```powershell
   dotnet build AA.Annotate.slnx -v minimal
   ```

   ```powershell
   dotnet run --project src\AA.Annotate.Cli\AA.Annotate.Cli.csproj -- session --wait
   ```

Add `--session-root "<folder>"` only when the user asks to store session files somewhere specific. Otherwise let AA Annotate use the OS temp directory.

Use a shell/tool timeout longer than the expected human annotation time. If `--timeout-seconds` is omitted, the CLI uses 600 seconds. `--timeout-seconds` controls app inactivity and the CLI waiter's inactivity bound, and resets while the user interacts with the app. The outer shell/tool timeout is separate and must be longer than the AA Annotate inactivity window plus expected user time. Use a shorter value such as 60 seconds only for deliberate short tests.

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
- Treat `Image:` as the primary source of truth. If the user cropped, this image is the cropped image; the full screenshot is not exported for normal use.
- Treat every annotation line as relative to the `Image:` path shown in that capture.
- Apply the indented comment immediately below an annotation line to that numbered rectangle.
- Do not merge annotation numbers across captures.

Annotation lines use this form:

```text
1. x=<left>, y=<top>, width=<width>, height=<height>
   <comment>
```

Coordinate rules:

- For an uncropped capture, coordinates are relative to the full-screen `Image:`.
- For a cropped capture, coordinates are relative to the cropped `Image:`.
- `Crop:` is metadata in original screenshot coordinates. Use it only when you must map a cropped annotation back to original screen coordinates.
- If `Crop:` describes a non-full crop but `Image:` points at an uncropped full screenshot, treat the handoff as inconsistent and report an artifact error instead of guessing coordinate basis.
- Export removes annotations fully outside the crop.
- Export clips annotations partly crossing the crop edge.
- Export renumbers remaining annotations sequentially inside each capture after filtering. These numbers may differ from temporary numbers the user saw while editing.

JSON rules:

- `captures[].screenshotPath` is the primary exported image path to inspect.
- `captures[].croppedPath` is present when the primary image came from a crop.
- `captures[].cropRect` records the crop in original screenshot coordinates.
- `captures[].annotations[].boxRect` follows the same coordinate basis as `REVIEW_MD`: it is relative to the primary exported image.
- `captures[].screenshotPixelSize` describes the original capture size, not necessarily the dimensions of a cropped primary image.
- Resolve relative JSON image paths from the folder containing `ANNOTATIONS_JSON`.

## Use the Result

Continue the task using the annotation comments and numbered regions.

Reference annotations by capture and number, for example `Capture 2, annotation 1`.

If a comment and its box appear inconsistent, trust the visual `Image:` plus the box first, then state the ambiguity instead of inventing intent.

Do not read or rely on `session.json`, `status.json`, or other private local state unless debugging launch/completion failure.

Do not create screenshots or annotation files in the workspace unless the user explicitly asks.

If launch fails from a plugin, check whether `<plugin-root>\app\AA.Annotate.App.exe` exists. If launch fails from a user-local install, check whether `%LOCALAPPDATA%\AA.Annotate\app\AA.Annotate.App.exe` exists. For repo-local packaged output, check whether `artifacts\publish\app-win-x64\AA.Annotate.App.exe` exists. Use `AA_ANNOTATE_APP` only for custom app paths.
