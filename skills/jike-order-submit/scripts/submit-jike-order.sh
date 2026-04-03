#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
DEFAULT_EXE_PATH="$SKILL_DIR/bin/linux-x64/JikeCLI"
FIXED_TENANT_ID="08dcf6f3-6f09-419b-8fdd-5747b5e5def8"
FIXED_USER_NAME="V1Jk7qVa2L5YCP9vtgfeSQ=="
FIXED_PASSWORD="arwj71YS+ZrtCIZTjdKBMQ=="

CONTENT=""
PHONE=""
URGENCY="0"
REQUIRED_TIME=""
EXE_PATH="$DEFAULT_EXE_PATH"
CONFIG_HOME=""
ATTACHMENT_PATHS=()
FILE_IDS=()
ORDER_ID=""
FINAL_REQUIRED_TIME=""

usage() {
  cat <<'EOF'
Usage:
  submit-jike-order.sh --content <text> --phone <phone> [options]

Options:
  --content <text>              Required. Order description.
  --phone <phone>               Required. Requester phone number.
  --urgency <0|1>               Optional. Default: 0
  --required-time <datetime>    Optional. Format: yyyy-MM-dd HH:mm:ss
  --attachment <path>           Optional. Repeat for multiple files.
  --exe-path <path>             Optional. Override JikeCLI Linux executable path.
  --config-home <path>          Optional. Override JIKE_CONFIG_HOME.
  -h, --help                    Show help.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --content)
      CONTENT="${2:-}"
      shift 2
      ;;
    --phone)
      PHONE="${2:-}"
      shift 2
      ;;
    --urgency)
      URGENCY="${2:-}"
      shift 2
      ;;
    --required-time)
      REQUIRED_TIME="${2:-}"
      shift 2
      ;;
    --attachment)
      ATTACHMENT_PATHS+=("${2:-}")
      shift 2
      ;;
    --exe-path)
      EXE_PATH="${2:-}"
      shift 2
      ;;
    --config-home)
      CONFIG_HOME="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ -z "$CONTENT" ]]; then
  echo "Missing required argument: --content" >&2
  exit 1
fi

if [[ -z "$PHONE" ]]; then
  echo "Missing required argument: --phone" >&2
  exit 1
fi

if [[ "$URGENCY" != "0" && "$URGENCY" != "1" ]]; then
  echo "--urgency must be 0 or 1" >&2
  exit 1
fi

if [[ ! -x "$EXE_PATH" ]]; then
  echo "JikeCLI executable not found or not executable: $EXE_PATH" >&2
  exit 1
fi

for attachment_path in "${ATTACHMENT_PATHS[@]}"; do
  if [[ ! -f "$attachment_path" ]]; then
    echo "Attachment file not found: $attachment_path" >&2
    exit 1
  fi
done

if [[ -z "$CONFIG_HOME" ]]; then
  CONFIG_HOME="$(mktemp -d "${TMPDIR:-/tmp}/jike-order-submit.XXXXXX")"
fi

export JIKE_CONFIG_HOME="$CONFIG_HOME"
export LC_ALL="${LC_ALL:-C.UTF-8}"
export LANG="${LANG:-C.UTF-8}"

invoke_cli() {
  echo "> $EXE_PATH $*"
  "$EXE_PATH" "$@"
}

extract_last_id() {
  awk -F'ID: ' '/ID: / { print $2 }' | tail -n 1
}

extract_last_time() {
  grep -Eo '[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2}' | tail -n 1
}

invoke_cli login \
  --username "$FIXED_USER_NAME" \
  --tenant "$FIXED_TENANT_ID" \
  --password "$FIXED_PASSWORD"

for attachment_path in "${ATTACHMENT_PATHS[@]}"; do
  upload_output="$(invoke_cli file upload --path "$attachment_path")"
  printf '%s\n' "$upload_output"

  file_id="$(printf '%s\n' "$upload_output" | extract_last_id)"
  if [[ -n "$file_id" ]]; then
    FILE_IDS+=("$file_id")
  fi
done

order_args=(
  order
  add
  --phone "$PHONE"
  --urgency "$URGENCY"
  --content "$CONTENT"
)

if [[ -n "$REQUIRED_TIME" ]]; then
  order_args+=(--requiredTime "$REQUIRED_TIME")
fi

if [[ ${#FILE_IDS[@]} -gt 0 ]]; then
  joined_file_ids="$(IFS=,; echo "${FILE_IDS[*]}")"
  order_args+=(--files "$joined_file_ids")
fi

order_output="$(invoke_cli "${order_args[@]}")"
printf '%s\n' "$order_output"

ORDER_ID="$(printf '%s\n' "$order_output" | extract_last_id)"
FINAL_REQUIRED_TIME="$(printf '%s\n' "$order_output" | extract_last_time)"

echo "ORDER_ID=$ORDER_ID"
echo "FILE_IDS=$(IFS=,; echo "${FILE_IDS[*]-}")"
echo "REQUIRED_TIME=$FINAL_REQUIRED_TIME"
echo "CONFIG_HOME=$CONFIG_HOME"
