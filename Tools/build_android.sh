#!/usr/bin/env bash
# Android APK (release by default; "dev" for a development build), then verifies it. Log: Logs/android_build.log
set -u
cd "$(dirname "$0")/.."
METHOD=AndroidReleaseBatch; APK=Builds/Android/LittleMusicWorld.apk
if [ "${1:-}" = "dev" ]; then METHOD=AndroidDevelopmentBatch; APK=Builds/Android/LittleMusicWorld-dev.apk; fi
Tools/unity_batch.sh LittleMusicWorld.EditorTools.BuildScript.$METHOD android_build -buildTarget Android
echo "ANDROID BUILD exit=$?"; grep -E "\[LMW Build\]|error CS" Logs/android_build.log | head -40
Tools/verify_apk.sh "$APK"
