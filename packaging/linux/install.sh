#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<'EOF'
Usage: ./install.sh [options]

Install AA.Annotate for the current user.

Options:
  --install-root PATH  App and CLI root (default: $HOME/.local/opt/aa-annotate)
  --skills-root PATH   Codex skills root (default: ${CODEX_HOME:-$HOME/.codex}/skills)
  --bin-dir PATH       Directory for the optional CLI link (default: $HOME/.local/bin)
  --add-cli-link       Create <bin-dir>/aa-annotate
  -h, --help           Show this help
EOF
}

die() {
    printf 'AA.Annotate install failed: %s\n' "$*" >&2
    exit 1
}

require_value() {
    [[ $# -ge 2 && -n "${2:-}" ]] || die "missing value for $1"
}

home_dir=${HOME:?HOME must be set}
install_root="$home_dir/.local/opt/aa-annotate"
codex_root=${CODEX_HOME:-"$home_dir/.codex"}
skills_root="$codex_root/skills"
bin_dir="$home_dir/.local/bin"
add_cli_link=false

while (($# > 0)); do
    case "$1" in
        --install-root)
            require_value "$@"
            install_root=$2
            shift 2
            ;;
        --skills-root)
            require_value "$@"
            skills_root=$2
            shift 2
            ;;
        --bin-dir)
            require_value "$@"
            bin_dir=$2
            shift 2
            ;;
        --add-cli-link)
            add_cli_link=true
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            die "unknown option: $1"
            ;;
    esac
done

case "$install_root" in
    ""|"/"|"$home_dir")
        die "refusing unsafe install root: $install_root"
        ;;
esac
case "$skills_root" in
    ""|"/"|"$home_dir")
        die "refusing unsafe skills root: $skills_root"
        ;;
esac

package_root=$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
app_source="$package_root/app"
cli_source="$package_root/cli"
skill_source="$package_root/skills/aa-annotate"
app_executable="$app_source/AA.Annotate.App"
cli_executable="$cli_source/aa-annotate"
ownership_marker="$install_root/.aa-annotate-install"
ownership_marker_value='aa-annotate-user-install-v1'

for required in "$app_executable" "$cli_executable" "$skill_source/SKILL.md" "$package_root/manifest.json"; do
    [[ -f "$required" ]] || die "package is incomplete; missing $required"
done

umask 022
if [[ -e "$install_root" ]]; then
    [[ -d "$install_root" ]] || die "install root exists and is not a directory: $install_root"
    if [[ -f "$ownership_marker" ]]; then
        marker_value=$(cat -- "$ownership_marker")
        [[ "$marker_value" == "$ownership_marker_value" ]] ||
            die "install root has an invalid AA.Annotate ownership marker: $install_root"
    elif [[ -n "$(find "$install_root" -mindepth 1 -maxdepth 1 -print -quit)" ]]; then
        die "refusing to replace a non-empty install root without an AA.Annotate ownership marker: $install_root"
    fi
else
    mkdir -p -- "$install_root"
fi

printf '%s\n' "$ownership_marker_value" > "$ownership_marker"
chmod 0644 -- "$ownership_marker"
mkdir -p -- "$skills_root"

app_target="$install_root/app"
cli_target="$install_root/cli"
skill_target="$skills_root/aa-annotate"

rm -rf -- "$app_target" "$cli_target" "$skill_target"
cp -a -- "$app_source" "$app_target"
cp -a -- "$cli_source" "$cli_target"
cp -a -- "$skill_source" "$skill_target"

for item in manifest.json README.txt LICENSE uninstall.sh; do
    if [[ -f "$package_root/$item" ]]; then
        cp -a -- "$package_root/$item" "$install_root/$item"
    fi
done

chmod 0755 -- "$app_target/AA.Annotate.App" "$cli_target/aa-annotate"
[[ ! -f "$install_root/uninstall.sh" ]] || chmod 0755 -- "$install_root/uninstall.sh"

installed_cli="$cli_target/aa-annotate"
installed_app="$app_target/AA.Annotate.App"

"$installed_cli" --help >/dev/null
"$installed_app" --help >/dev/null

cli_link=
if [[ "$add_cli_link" == true ]]; then
    mkdir -p -- "$bin_dir"
    cli_link="$bin_dir/aa-annotate"
    if [[ -L "$cli_link" ]]; then
        existing_target=$(readlink -- "$cli_link")
        if [[ "$existing_target" != "$installed_cli" ]]; then
            die "refusing to replace unrelated symbolic link: $cli_link"
        fi
    elif [[ -e "$cli_link" ]]; then
        die "refusing to replace unrelated file: $cli_link"
    fi
    ln -sfn -- "$installed_cli" "$cli_link"
fi

printf 'AA.Annotate installed.\n'
printf 'App:   %s\n' "$installed_app"
printf 'CLI:   %s\n' "$installed_cli"
printf 'Skill: %s\n' "$skill_target"
if [[ -n "$cli_link" ]]; then
    printf 'Link:  %s\n' "$cli_link"
fi
printf '\nRun without PATH changes:\n  %q session --wait\n' "$installed_cli"
