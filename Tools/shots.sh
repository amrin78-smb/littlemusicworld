#!/usr/bin/env bash
# Renders every build scene to Logs/Shots/*.png for art review (run Tools/setup.sh first).
set -u
cd "$(dirname "$0")/.."
Tools/unity_batch.sh LittleMusicWorld.EditorTools.SceneShots.RunBatch shots
code=$?; grep -E "\[LMW|error CS|Exception" Logs/shots.log | head -20; echo "SHOTS exit=$code"; exit $code
