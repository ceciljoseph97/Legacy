#!/usr/bin/env bash
# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/scripts/compliance.sh
# NeuroSim compliance check (Linux/CI)
# docCompliant: docs populated, README, CHANGELOG
# testCompliant: unit + system tests pass
# dirCompliant: expected directory structure
# State: Perfect(11), Good(10), Okay(01), Bad(00)

ROOT="${1:-$(cd "$(dirname "$0")/.." && pwd)}"
cd "$ROOT"

out01() { [[ "$1" == "1" ]] && echo "01" || echo "00"; }

CFG_FILE="${COMPLIANCE_CONFIG:-$ROOT/compliance.config.json}"

# Minimal JSON array extractor (assumes arrays of strings, one level deep).
# This avoids requiring jq/python/node on CI images.
json_array() {
  local key="$1"
  # CRLF: strip \r before matching; close array only on a line that is just ], (not any stray ]).
  awk -v k="\"$key\"" '
    { sub(/\r$/, "") }
    $0 ~ k"[[:space:]]*:[[:space:]]*\\[" { inarr=1; next }
    inarr && $0 ~ /^[[:space:]]*\][[:space:]]*,?[[:space:]]*$/ { inarr=0; exit }
    inarr { print }
  ' "$CFG_FILE" | sed -n 's/.*"\([^"]\+\)".*/\1/p' | tr -d '\r' | sed '/^$/d'
}

json_number() {
  local key="$1"
  tr -d '\r' < "$CFG_FILE" | sed -n "s/.*\"$key\"[[:space:]]*:[[:space:]]*\\([0-9][0-9]*\\).*/\\1/p" | head -n 1
}

json_string() {
  local key="$1"
  tr -d '\r' < "$CFG_FILE" | sed -n "s/.*\"$key\"[[:space:]]*:[[:space:]]*\"\\([^\"]*\\)\".*/\\1/p" | head -n 1
}

# Fail fast if config missing
[[ -f "$CFG_FILE" ]] || { echo "Missing compliance config: $CFG_FILE"; exit 1; }

# Helpers
docs_populated() {
  # requiredDocs must exist
  while IFS= read -r rel; do
    [[ -e "$rel" ]] || return 1
  done < <(json_array requiredDocs)

  [[ -d docs ]] || return 1
  local min="$(json_number minimumDocsMarkdownFilesInDocsDir)"
  [[ -z "$min" ]] && min=2
  local n=0
  shopt -s nullglob
  for _ in docs/*.md; do [[ -f "$_" ]] && n=$((n + 1)); done
  shopt -u nullglob
  [[ "$n" -ge "$min" ]]
}

readme_done() {
  [[ -f README.md ]] || return 1
  local len=$(wc -c < README.md)
  [[ $len -gt 200 ]] && grep -qE "NeuroSim|Run it|Tests" README.md
}

changelog_done() {
  [[ -f CHANGELOG.md ]] || return 1
  local len=$(wc -c < CHANGELOG.md)
  [[ $len -gt 100 ]] && grep -qE "Changelog|Added|Changed" CHANGELOG.md
}

dir_structure() {
  while IFS= read -r d; do
    [[ -e "$d" ]] || return 1
  done < <(json_array requiredDirectories)
}

# Build first
dotnet build --verbosity quiet --nologo 2>/dev/null || true

# Docs + dir checks
docs_ok=0
docs_populated && readme_done && changelog_done && docs_ok=1

dir_ok=0
dir_structure && dir_ok=1

doc_compliant=0
[[ $docs_ok -eq 1 && $dir_ok -eq 1 ]] && doc_compliant=1

# Test checks (don't exit on fail)
unit_ok=0
unit_ok=1
while IFS= read -r p; do
  [[ -f "$p" ]] || { unit_ok=0; break; }
  dotnet test "$p" --verbosity quiet --nologo 2>/dev/null || { unit_ok=0; break; }
done < <(json_array unitTestProjects)

sys_ok=0
sys_ok=1
while IFS= read -r p; do
  [[ -f "$p" ]] || { sys_ok=0; break; }
  dotnet test "$p" --verbosity quiet --nologo 2>/dev/null || { sys_ok=0; break; }
done < <(json_array systemTestProjects)

test_compliant=0
[[ $unit_ok -eq 1 && $sys_ok -eq 1 ]] && test_compliant=1

# State: bit0=docCompliant, bit1=testCompliant
state_bits=$(( (test_compliant << 1) | doc_compliant ))
case $state_bits in
  0) state_name="Bad" ;;
  1) state_name="Okay" ;;
  2) state_name="Good" ;;
  3) state_name="Perfect" ;;
  *) state_name="Unknown" ;;
esac

# Binary display 00, 01, 10, 11
case $state_bits in
  0) bits_bin="00" ;;
  1) bits_bin="01" ;;
  2) bits_bin="10" ;;
  3) bits_bin="11" ;;
  *) bits_bin="??" ;;
esac

# Output
echo ""
echo "NeuroSim Compliance Check"
echo "-------------------------"
echo "docs populated:    $(out01 $docs_ok)"
readme_done && r_ok=1 || r_ok=0
echo "README done:       $(out01 $r_ok)"
changelog_done && c_ok=1 || c_ok=0
echo "CHANGELOG done:    $(out01 $c_ok)"
echo "dir structure:     $(out01 $dir_ok)"
echo "dirCompliant:      $(out01 $dir_ok)"
echo "docCompliant:      $(out01 $doc_compliant)"
echo ""
echo "unit tests:        $([[ $unit_ok -eq 1 ]] && echo pass || echo fail)"
echo "system tests:      $([[ $sys_ok -eq 1 ]] && echo pass || echo fail)"
echo "testCompliant:     $(out01 $test_compliant)"
echo ""
echo "State: $state_name ($bits_bin)"
echo ""

# CI: exit 0 on pass, 1 on fail
threshold="$(json_string passThreshold)"
[[ -z "$threshold" ]] && threshold="Good"
case "$threshold" in
  Perfect) need=3 ;;
  Good)    need=2 ;;
  Okay)    need=1 ;;
  *)       need=2 ;;
esac
[[ $state_bits -ge $need ]] && exit 0 || exit 1
