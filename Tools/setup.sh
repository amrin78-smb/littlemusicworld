#!/usr/bin/env bash
# Regenerates settings, UI sprites, instruments, models, prefabs and scenes, then validates. Log: Logs/setup.log
set -u
cd "$(dirname "$0")/.."
# TextMeshPro essentials are extracted directly (AssetDatabase.ImportPackage is asynchronous in batch mode).
if [ ! -f "Assets/TextMesh Pro/Resources/TMP Settings.asset" ]; then
  PKG=$(ls Library/PackageCache/com.unity.ugui@*/"Package Resources/TMP Essential Resources.unitypackage" 2>/dev/null | head -1)
  [ -n "$PKG" ] && python3 -I Tools/extract_unitypackage.py "$PKG" .
fi
mkdir -p Assets/Resources/Voice && cp Tools/voice/voice_lines.json Assets/Resources/Voice/voice_lines.json
Tools/unity_batch.sh LittleMusicWorld.EditorTools.ProjectSetup.RunBatch setup
code=$?
grep -E "\[LMW|error CS|Exception" Logs/setup.log | grep -v "^UnityEngine" | head -80
echo "SETUP exit=$code"
exit $code
