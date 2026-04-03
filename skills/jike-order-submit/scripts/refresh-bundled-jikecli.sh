#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$SKILL_DIR/../.." && pwd)"

SOURCE_DIR="${1:-$REPO_ROOT/src/JikeCLI/bin/Release/net10.0/linux-x64/publish}"
TARGET_DIR="$SKILL_DIR/bin/linux-x64"

if [[ ! -d "$SOURCE_DIR" ]]; then
  echo "Source publish directory not found: $SOURCE_DIR" >&2
  exit 1
fi

mkdir -p "$TARGET_DIR"

cp "$SOURCE_DIR/JikeCLI" "$TARGET_DIR/JikeCLI"

if [[ -f "$SOURCE_DIR/JikeCLI.dbg" ]]; then
  cp "$SOURCE_DIR/JikeCLI.dbg" "$TARGET_DIR/JikeCLI.dbg"
fi

chmod +x "$TARGET_DIR/JikeCLI"

echo "Bundled JikeCLI refreshed."
echo "Source: $SOURCE_DIR"
echo "Target: $TARGET_DIR"
