---
name: aa-annotate
description: Launch the AA Annotate desktop overlay to collect visual screenshot annotations from the user and then continue from the generated review. Use when a task needs the user to point at UI elements, mark regions on screen, compare windows/tabs, crop screenshots, or provide visual context that text alone cannot describe.
---

# AA Annotate

Use AA Annotate when the next step depends on the user's visual selection or comments on what is currently on their screen.

## Workflow

1. Start an annotation session and wait for completion:

   ```powershell
   aa-annotate session --wait
   ```

   If the command is not installed but the source repo is available, run:

   ```powershell
   dotnet run --project src\AA.Annotate.Cli\AA.Annotate.Cli.csproj -- session --wait
   ```

   If the Windows package was installed without PATH changes, run:

   ```powershell
   & "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait
   ```

2. Tell the user that the annotation window is open and wait for them to capture, crop, annotate, and send the session back.

3. When the command exits, read stdout:

   ```text
   SESSION_STATUS=completed
   REVIEW_MD=...
   ANNOTATIONS_JSON=...
   ```

4. Read `REVIEW_MD` first. Treat it as the primary agent-facing handoff. It contains captures, crop information, annotation numbers, comments, and image paths.

5. Read `ANNOTATIONS_JSON` only when exact structured coordinates or full session metadata are needed.

6. Continue the task using the user's annotation text and numbered boxes. When reporting back, reference annotation numbers rather than asking the user to restate them.

## Rules

- Do not create screenshot or annotation files in the workspace unless the user explicitly asks. Let the tool use its default OS temp session folder.
- Do not continue as if annotations exist when `SESSION_STATUS` is `cancelled` or `error`; tell the user the session did not complete.
- Do not default to `session.json` or other private local state. Use `review.md` first, then `annotations.json` if structured data is required.
- Keep the annotation command running with a long timeout; the user may need time to switch apps, capture multiple screens, crop, and write comments.
- If the app fails to launch from a packaged install, check whether `%LOCALAPPDATA%\AA.Annotate\app\AA.Annotate.App.exe` exists. `AA_ANNOTATE_APP` is optional and should only be needed for custom installs.
