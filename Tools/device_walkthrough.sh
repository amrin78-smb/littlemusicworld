#!/usr/bin/env bash
# Visits every scene (and optionally an instrument station) on a device via launch extras and screenshots each:
#   Tools/device_walkthrough.sh [serial] [Scene[:station] ...]
# Default list covers menu, hub, every area (first station open), orchestra, studio, carnival.
# Screenshots: Logs/Device/walk_<serial>_<Scene>[_<station>].png. Always closes the app and turns the screen off.
set -u
cd "$(dirname "$0")/.."
PKG=com.nocvault.littlemusicworld
S="${1:-$(Tools/device.sh connect | awk '{print $1}')}"; shift || true
LIST=("$@")
[ ${#LIST[@]} -eq 0 ] && LIST=(MainMenu Hub PianoParadise PianoParadise:0 DrumValley DrumValley:0 StringTown:1 WindVillage:1 OrchestraPark MusicStudio RhythmCarnival)
trap 'adb -s "$S" shell am force-stop $PKG; adb -s "$S" shell input keyevent KEYCODE_SLEEP' EXIT
adb -s "$S" shell input keyevent KEYCODE_WAKEUP; adb -s "$S" shell wm dismiss-keyguard 2>/dev/null
mkdir -p Logs/Device
TAG=${S//[:.]/_}
adb -s "$S" logcat -c
for item in "${LIST[@]}"; do
  scene=${item%%:*}; station=""; [ "$item" != "$scene" ] && station=${item#*:}
  adb -s "$S" shell am force-stop $PKG
  EXTRA=(--es lmw_scene "$scene"); [ -n "$station" ] && EXTRA+=(--es lmw_station "$station")
  adb -s "$S" shell am start -n $PKG/com.unity3d.player.UnityPlayerGameActivity "${EXTRA[@]}" >/dev/null
  sleep "${WALK_WAIT:-16}"
  out="Logs/Device/walk_${TAG}_${scene}${station:+_$station}.png"
  adb -s "$S" exec-out screencap -p > "$out"
  echo "shot $out"
done
adb -s "$S" logcat -d -s Unity:W Unity:E AndroidRuntime:E | grep -vE "^-----" | grep -E "Exception|Error|LMW" | sort | uniq -c | sort -rn | head -20
