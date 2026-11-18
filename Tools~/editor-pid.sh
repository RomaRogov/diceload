# Sourced by the tools: the process id of the Unity Editor that has this project open
# (UNITY_PROJECT_PATH or the current folder), as the unity CLI reports it.
project=${UNITY_PROJECT_PATH:-$PWD}
editor_pid=$(unity status --project-path "$project" --format tsv --no-header 2>/dev/null | awk -F'\t' 'NF >= 5 { print $5; exit }')
