#!/usr/bin/env bash
# dos2unix.sh — Convert all *.sh and .env/.env.* files from CRLF to LF with logs,
# then chmod 755 on converted *.sh files (only).

set -uo pipefail
set -o noglob   # prevent *.sh from expanding before find runs

# ----- settings -----
MAKE_SH_EXECUTABLE=true   # when true, chmod 755 *.sh after successful conversion
# ---------------------

# Colors when TTY
if [[ -t 1 ]]; then
  C_OK=$'\033[32m'; C_SKIP=$'\033[33m'; C_FAIL=$'\033[31m'; C_INFO=$'\033[36m'; C_RESET=$'\033[0m'
else
  C_OK=""; C_SKIP=""; C_FAIL=""; C_INFO=""; C_RESET=""
fi
info(){ echo "${C_INFO}[INFO]${C_RESET} $*"; }
ok(){   echo "${C_OK}[OK]  ${C_RESET} $*"; }
skip(){ echo "${C_SKIP}[SKIP]${C_RESET} $*"; }
fail(){ echo "${C_FAIL}[FAIL]${C_RESET} $*"; }

# Go to repo root if this is a git repo
if command -v git >/dev/null 2>&1 && git rev-parse --show-toplevel >/dev/null 2>&1; then
  cd "$(git rev-parse --show-toplevel)"
fi

# Gather files (.sh, .env, .env.*) — globs are quoted so find gets them literally
mapfile -d '' FILES < <(
  find . -type f \( -name '*.sh' -o -name '.env' -o -name '.env.*' \) -print0 2>/dev/null
)

if [[ ${#FILES[@]} -eq 0 ]]; then
  info "No matching files found (*.sh, .env, .env.*)."
  exit 0
fi

# Choose tool
USE_D2U=false
if command -v dos2unix >/dev/null 2>&1; then
  USE_D2U=true
  info "Using dos2unix."
else
  info "dos2unix not found; using Perl fallback to strip CR."
fi

# Helper: does file still contain CRs?
has_cr(){ LC_ALL=C grep -q $'\r' -- "$1"; }

converted=0; unchanged=0; skipped=0; failed=0; chmod_ok=0; chmod_skip=0; chmod_fail=0

for f in "${FILES[@]}"; do
  # readable/writable checks
  [[ -r "$f" ]] || { skip "$f (not readable)"; ((skipped++)); continue; }
  [[ -w "$f" ]] || { skip "$f (not writable)"; ((skipped++)); continue; }

  if ! has_cr "$f"; then
    ok "$f already LF (no change)."
    ((unchanged++))
    # Note: we only chmod after conversion, by request.
    continue
  fi

  # Convert
  if $USE_D2U; then
    if dos2unix -q -k -- "$f" && ! has_cr "$f"; then
      ok "$f converted (dos2unix)."
      ((converted++))
    else
      fail "$f conversion failed (dos2unix)."
      ((failed++))
      continue
    fi
  else
    if perl -i -pe 's/\r$//' -- "$f" && ! has_cr "$f"; then
      ok "$f converted (fallback)."
      ((converted++))
    else
      fail "$f conversion failed (fallback)."
      ((failed++))
      continue
    fi
  fi

  # chmod 755 only for *.sh and only after successful conversion
  if $MAKE_SH_EXECUTABLE && [[ "$f" == *.sh ]]; then
    if chmod 755 -- "$f"; then
      ok "$f chmod 755."
      ((chmod_ok++))
    else
      fail "$f chmod 755 failed."
      ((chmod_fail++))
    fi
  else
    ((chmod_skip++))
  fi
done

echo
info "Summary:"
echo "  Converted : $converted"
echo "  No change : $unchanged"
echo "  Skipped   : $skipped"
echo "  Failed    : $failed"
echo "  chmod OK  : $chmod_ok"
echo "  chmod Skip: $chmod_skip"
echo "  chmod Fail: $chmod_fail"

(( failed > 0 )) && exit 1 || exit 0