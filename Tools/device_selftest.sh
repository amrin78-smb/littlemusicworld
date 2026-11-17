#!/usr/bin/env bash
# Runs the on-device audio/performance self-test and saves the report to Logs/Device/selftest_<serial>.txt.
#   Tools/device_selftest.sh [serial]   (default: the Wi-Fi test phone; use emulator-5554 for the emulator)
# Always closes the app and switches the screen off afterwards (owner rule).
set -u
cd "$(dirname "$0")/.."
PKG=com.nocvault.littlemusicworld
S="${1:-$(Tools/device.sh connect | awk '{print $1}')}"
trap 'adb -s "$S" shell am force-stop $PKG; adb -s "$S" shell input keyevent KEYCODE_SLEEP' EXIT
adb -s "$S" shell input keyevent KEYCODE_WAKEUP; adb -s "$S" shell wm dismiss-keyguard 2>/dev/null
adb -s "$S" shell am force-stop $PKG; adb -s "$S" logcat -c
adb -s "$S" shell am start -n $PKG/com.unity3d.player.UnityPlayerGameActivity --es lmw_selftest 1 >/dev/null
OUT="Logs/Device/selftest_${S//[:.]/_}.txt"; mkdir -p Logs/Device
for i in $(seq 1 ${SELFTEST_TIMEOUT:-120}); do
  sleep 2
  if adb -s "$S" logcat -d -s Unity:I | grep -q "\[LMW SelfTest\] DONE"; then break; fi
done
adb -s "$S" logcat -d -s Unity:I | grep -E "\[LMW( SelfTest|)\]|Exception" | sed 's/.*Unity   : //' | tee "$OUT"
