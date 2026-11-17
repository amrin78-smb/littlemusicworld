#!/usr/bin/env bash
# Copies staged Blender/branding exports (Blender/Exports/{Art,Resources}) into the Unity project, only files that
# changed. Run only when no Unity process is building (setup.sh calls it first).
set -eu
cd "$(dirname "$0")/.."
for d in Art Resources; do
  [ -d "Blender/Exports/$d" ] && rsync -a --checksum --itemize-changes "Blender/Exports/$d/" "Assets/$d/" | grep -c '^>' | sed "s/^/synced $d: /" || true
done
