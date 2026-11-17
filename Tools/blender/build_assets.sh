#!/usr/bin/env bash
# Builds Blender assets headless (offline). Each script in Tools/blender/models/<name>.py models, UV-unwraps,
# bakes, exports FBX to Assets/Art and saves the .blend source to Blender/SourceModels.
#   Tools/blender/build_assets.sh                    # everything
#   Tools/blender/build_assets.sh GrandPiano Melo    # selected (names = script names, case-insensitive)
#   add --preview to also render review images into Blender/Renders
set -u
cd "$(dirname "$0")/../.."
mkdir -p Logs/Blender
PREVIEW=""; NAMES=()
for a in "$@"; do [ "$a" = "--preview" ] && PREVIEW="--preview" || NAMES+=("$a"); done
ALL=(grand_piano melo hub_env instruments_keys instruments_drums instruments_strings instruments_winds animals)
[ ${#NAMES[@]} -eq 0 ] && NAMES=("${ALL[@]}")
rc=0
for n in "${NAMES[@]}"; do
  s=$(ls Tools/blender/models/*.py | grep -i "/$(echo "$n" | sed 's/\([a-z]\)\([A-Z]\)/\1_\2/g' | tr 'A-Z' 'a-z').py$" | head -1)
  [ -z "$s" ] && { echo "no model script for $n"; rc=1; continue; }
  name=$(basename "$s" .py)
  if blender -b --factory-startup --python-exit-code 1 --python "$s" -- $PREVIEW > "Logs/Blender/$name.log" 2>&1; then
    echo "OK   $name  $(grep -E '\[LMW\] [A-Za-z]+:' Logs/Blender/$name.log | tail -1)"
  else
    echo "FAIL $name (Logs/Blender/$name.log)"; grep -E "Error|Traceback" -A3 "Logs/Blender/$name.log" | head; rc=1
  fi
done
exit $rc
