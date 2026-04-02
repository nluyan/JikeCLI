#!/usr/bin/env bash

set -euo pipefail

CONFIGURATION="${1:-Release}"
RUNTIME="${2:-linux-x64}"

SCRIPT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_ROOT/src/JikeCLI/JikeCLI.csproj"
PUBLISH_PATH="$SCRIPT_ROOT/src/JikeCLI/bin/$CONFIGURATION/net10.0/$RUNTIME/publish"

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "Native AOT for Linux must be published on a Linux environment."
  echo "Current OS: $(uname -s)"
  echo "Try running this script in Linux, WSL, Docker, or CI."
  exit 1
fi

echo "Publishing JikeCLI with Native AOT..."
echo "Project: $PROJECT_PATH"
echo "Configuration: $CONFIGURATION"
echo "Runtime: $RUNTIME"

dotnet publish "$PROJECT_PATH" -c "$CONFIGURATION" -r "$RUNTIME"

if [[ ! -d "$PUBLISH_PATH" ]]; then
  echo "Publish output not found: $PUBLISH_PATH"
  exit 1
fi

echo
echo "Publish completed."
echo "Output: $PUBLISH_PATH"
