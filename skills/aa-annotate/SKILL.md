---
name: aa-annotate
description: Launch the AA Annotate desktop overlay to collect visual screenshot annotations from the user and then continue from the generated review. Use when a task needs the user to point at UI elements, mark regions on screen, compare windows/tabs, crop screenshots, or provide visual context that text alone cannot describe.
---

# AA Annotate

Use AA Annotate when the next step depends on the user's visual selection or comments on what is currently on their screen.

## Workflow

1. Choose the first available CLI path in this order.

   If this skill is loaded from a release-bundled Codex plugin, resolve the directory containing this `SKILL.md`; the plugin root is two directories up. Use this path only when the executable exists:

   ```powershell
   & "<plugin-root>\cli\aa-annotate.exe" session --wait --timeout-seconds 60
   ```

   If the app was installed from the release installer, use:

   ```powershell
   & "$env:LOCALAPPDATA\AA.Annotate\cli\aa-annotate.exe" session --wait --timeout-seconds 60
   ```

   If the user asks to store annotation sessions in a specific folder, add:

   ```powershell
   --session-root "<folder>"
   ```

   If the repo-local Windows publish output is available, run:

   ```powershell
   & ".\artifacts\publish\cli-win-x64\aa-annotate.exe" session --wait --timeout-seconds 60
   ```

   If `aa-annotate` was registered on `PATH`, this is also valid:

   ```powershell
   aa-annotate session --wait --timeout-seconds 60
   ```

   If the CLI is not installed but the source repo is available for development, run:

   ```powershell
   dotnet run --project src\AA.Annotate.Cli\AA.Annotate.Cli.csproj -- session --wait --timeout-seconds 60
   ```

   If none of these CLI paths exists, tell the user to install the AA Annotate GitHub Release bundle. Do not treat a skill-only install as complete; the skill requires the executable.

2. Before starting the blocking command, tell the user that the annotation window is opening and that you will wait for them to capture, crop, annotate, and send the session back.

3. Start the annotation session and wait for completion. If your shell/tool call requires its own timeout, make it longer than the expected human annotation session. Do not set the outer command timeout to 60 seconds; `--timeout-seconds 60` is the app inactivity period and resets while the user interacts with the app.

4. When the command exits, read stdout:

   ```text
   SESSION_STATUS=completed
   REVIEW_MD=...
   ANNOTATIONS_JSON=...
   ```

   If stdout contains `SESSION_STATUS=error`, read `ERROR_MESSAGE` and report that the annotation session failed.

5. Read `REVIEW_MD` first. Treat it as the primary agent-facing handoff. It contains captures, crop information, annotation numbers, comments, and image paths.

6. Read `ANNOTATIONS_JSON` only when exact structured coordinates or full session metadata are needed.

7. Continue the task using the user's annotation text and numbered boxes. When reporting back, reference annotation numbers rather than asking the user to restate them.

## Rules

- Do not create screenshot or annotation files in the workspace unless the user explicitly asks. Let the tool use its default OS temp session folder.
- Do not continue as if annotations exist when `SESSION_STATUS` is `cancelled` or `error`; tell the user the session did not complete.
- Do not default to `session.json` or other private local state. Use `review.md` first, then `annotations.json` if structured data is required.
- Pass `--timeout-seconds 60` unless there is a specific reason to use a different inactivity period. This one-minute inactivity period is the recommended agent workflow.
- Pass `--session-root <folder>` when the user wants session files stored somewhere other than the default OS temp location.
- If `--timeout-seconds` is omitted, the CLI uses a ten-minute inactivity period. The app resets the timer when the user interacts with it and shows a finite warning before closing an inactive session.
- Keep any outer agent command timeout longer than the app inactivity timeout plus expected user annotation time.
- If the app fails to launch from a packaged install, check whether `%LOCALAPPDATA%\AA.Annotate\app\AA.Annotate.App.exe` exists. For repo-local publish output, check whether `artifacts\publish\app-win-x64\AA.Annotate.App.exe` exists. `AA_ANNOTATE_APP` is optional and should only be needed for custom app paths.
