# Sourced by the tools: the process id of the Unity Editor that has this project open (UNITY_PROJECT_PATH or the current folder).
project=${UNITY_PROJECT_PATH:-$PWD}
project=${project%/}
editor_pid=$(ps -axo pid=,command= | awk -v p="$project" 'index($0, "MacOS/Unity -") && !index($0, "-adb2") {
  for (i = 1; i <= NF; i++) if ((tolower($i) == "-projectpath") && ($(i+1) == p || $(i+1) == p "/")) { print $1; exit } }')
