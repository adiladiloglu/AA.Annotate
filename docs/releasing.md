# Releasing AA.Annotate

AA.Annotate releases are created by `.github/workflows/release.yml` when an annotated
`v*` tag is pushed. The workflow tests the full solution on Windows and Linux, builds
self-contained packages on their native operating systems, verifies Linux executable
file modes, and publishes both assets to one GitHub release.

Do not create a release manually with only a Windows asset. Linux archives must be
created on Linux so `tar` preserves executable file modes.

## Release contents

A successful `v0.5.0` workflow publishes:

- `aa-annotate-0.5.0-win-x64.zip`
- `aa-annotate-0.5.0-linux-x64.tar.gz`

The release body comes from `docs/release-notes/v0.5.0.md`.

## Prepare a release

1. Update the shared version in `Directory.Build.props`.
2. Update the default versions in `scripts/package-win.ps1` and
   `scripts/package-linux.ps1`.
3. Update `.codex-plugin/plugin.json` and `.claude-plugin/plugin.json`.
4. Add `docs/release-notes/v<version>.md`.
5. Run the complete solution tests on Windows and at least one supported Linux
   environment. Test GNOME/Wayland and KDE/X11 when the change affects desktop
   integration.
6. Validate the skill:

   ```powershell
   python C:\Users\<user>\.codex\skills\.system\skill-creator\scripts\quick_validate.py skills\aa-annotate
   ```

7. Build the platform packages on their native operating systems:

   ```powershell
   ./scripts/package-win.ps1 -Version 0.5.0
   pwsh ./scripts/package-linux.ps1 -Version 0.5.0
   ```

See [Linux support and installation](linux.md) for build and desktop prerequisites.

## Publish

Commit the complete release and make sure the worktree is clean. From `master`, run:

```powershell
./scripts/publish-github-release.ps1 -Version 0.5.0
```

The script verifies the branch, clean worktree, release notes, GitHub authentication,
and absence of an existing local or remote tag. It pushes `master`, creates an
annotated tag, and pushes the tag. The GitHub Actions workflow owns package creation
and release publication.

## Verify

Wait for all three workflow jobs to pass:

- Test and package Windows
- Test and package Linux
- Publish GitHub Release

Then confirm the release is public, its notes match the versioned notes file, and both
assets are attached. Download each archive and inspect its `manifest.json`. On Linux,
also verify `app/AA.Annotate.App`, `cli/aa-annotate`, `install.sh`, and `uninstall.sh`
are executable.
