#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<'EOF'
Usage: ./uninstall.sh [options]

Uninstall AA.Annotate for the current user.

Options:
  --install-root PATH  App and CLI root (default: $HOME/.local/opt/aa-annotate)
  --skills-root PATH   Codex skills root (default: ${CODEX_HOME:-$HOME/.codex}/skills)
  --bin-dir PATH       Directory containing the optional CLI link (default: $HOME/.local/bin)
  --remove-cli-link    Remove <bin-dir>/aa-annotate when it points into the install root
  -h, --help           Show this help
EOF
}

die() {
    printf 'AA.Annotate uninstall failed: %s\n' "$*" >&2
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
remove_cli_link=false

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
        --remove-cli-link)
            remove_cli_link=true
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

skill_target="$skills_root/aa-annotate"
cli_link="$bin_dir/aa-annotate"
installed_cli="$install_root/cli/aa-annotate"
ownership_marker="$install_root/.aa-annotate-install"
ownership_marker_value='aa-annotate-user-install-v1'

if [[ "$remove_cli_link" == true && -L "$cli_link" ]]; then
    link_target=$(readlink -- "$cli_link")
    if [[ "$link_target" == "$installed_cli" ]]; then
        rm -- "$cli_link"
    else
        die "refusing to remove a CLI link that does not point to this installation: $cli_link"
    fi
fi

if [[ -e "$install_root" ]]; then
    [[ -d "$install_root" ]] || die "install root exists and is not a directory: $install_root"
    [[ -f "$ownership_marker" ]] ||
        die "refusing to remove an install root without an AA.Annotate ownership marker: $install_root"
    marker_value=$(cat -- "$ownership_marker")
    [[ "$marker_value" == "$ownership_marker_value" ]] ||
        die "refusing to remove an install root with an invalid AA.Annotate ownership marker: $install_root"
    rm -rf -- "$install_root"
fi

rm -rf -- "$skill_target"

printf 'AA.Annotate uninstalled from:\n'
printf '  %s\n' "$install_root"
printf '  %s\n' "$skill_target"
if [[ "$remove_cli_link" == true ]]; then
    printf 'CLI link checked at:\n  %s\n' "$cli_link"
fi
