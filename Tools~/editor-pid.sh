# Sourced by the tools: the process id of the Unity Editor that has this project open
# (UNITY_PROJECT_PATH or the current folder), as the unity CLI reports it.
project=${UNITY_PROJECT_PATH:-$PWD}
editor_pid=$(unity status --project-path "$project" --format tsv --no-header 2>/dev/null | awk -F'\t' 'NF >= 5 { print $5; exit }')
# An Editor still starting — waiting on the Safe Mode dialog after a compile error — isn't registered
# yet, so the CLI doesn't list it: found by the project it was started with instead (macOS and Linux).
if [ -z "$editor_pid" ] && [ "$(uname -s)" != MINGW* ]; then
  real=$(cd "$project" 2>/dev/null && pwd -P)
  editor_pid=$(ps -axo pid=,args= 2>/dev/null | awk -v a="$project" -v b="$real" '
    /[Uu]nity/ && tolower($0) ~ /-projectpath/ {
      for (i = 2; i < NF; i++) if (tolower($i) == "-projectpath") { p = $(i + 1); sub(/\/$/, "", p);
        if (p == a || p == b || "/private" p == b) { print $1; exit } } }')
fi
