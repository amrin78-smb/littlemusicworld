#!/usr/bin/env bash
# Renders the logo, icon layers, splash logo and loading background (needs Blender/SourceModels/Melo.blend:
# run Tools/blender/build_assets.sh Melo first). Then run Tools/setup.sh to apply them to Player Settings.
set -eu
cd "$(dirname "$0")/../.."
mkdir -p Logs/Blender
blender -b --factory-startup --python-exit-code 1 --python Tools/branding/build_branding.py 2>&1 | tee Logs/Blender/branding.log | grep -E "\[LMW\]|Error|Traceback"
