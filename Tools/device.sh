#!/usr/bin/env bash
# Test-device helper (Galaxy S23 Ultra over Wi-Fi adb by default).
#   Tools/device.sh connect                 find + connect (5555 first, then the wireless-debugging mDNS port)
#   Tools/device.sh install <apk> [abi]     install (abi=armeabi-v7a forces the 32-bit libraries)
#   Tools/device.sh run [seconds]           launch, wait, screenshot + logcat into Logs/Device, then close
#   Tools/device.sh close                   force-stop the app and turn the screen off (ALWAYS after a test)
# Owner rule: after every install/test the app is closed and the screen switched off.
set -u
cd "$(dirname "$0")/.."
HOST="${LMW_DEVICE_HOST:-192.168.31.96}"
PKG="${LMW_PACKAGE:-com.nocvault.littlemusicworld}"
mkdir -p Logs/Device
# The phone may appear as host:port or under its auto-connected mDNS TLS name; accept either (never the emulator).
serial() {
  adb devices | awk -v h="$HOST" '$2=="device" && ($1 ~ "^"h":" || $1 ~ "_adb-tls-connect") {print $1; exit}'
}
connect() {
  S=$(serial); [ -n "$S" ] && { echo "$S"; return 0; }
  adb connect "$HOST:5555" >/dev/null 2>&1; S=$(serial); [ -n "$S" ] && { echo "$S"; return 0; }
  P=$(adb mdns services 2>/dev/null | awk -v h="$HOST" '$2=="_adb-tls-connect._tcp" && index($3,h":")==1{split($3,a,":");print a[2];exit}')
  [ -n "$P" ] && adb connect "$HOST:$P" >/dev/null 2>&1 && S=$(serial)
  # Keep a fixed port open for next time (lasts until the phone reboots).
  [ -n "${S:-}" ] && { adb -s "$S" tcpip 5555 >/dev/null 2>&1; sleep 3; adb connect "$HOST:5555" >/dev/null 2>&1; S=$(serial); }
  [ -n "${S:-}" ] && { echo "$S"; return 0; }
  echo "No device at $HOST (turn on Wireless debugging)" >&2; return 1
}
close_app() { adb -s "$1" shell am force-stop "$PKG"; adb -s "$1" shell input keyevent KEYCODE_SLEEP; echo "closed $PKG, screen off"; }
cmd="${1:-connect}"; shift || true
S=$(connect) || exit 1
case "$cmd" in
  connect) echo "$S $(adb -s "$S" shell getprop ro.product.model)";;
  install) ABI=(); [ -n "${2:-}" ] && ABI=(--abi "$2"); adb -s "$S" install -r "${ABI[@]}" "$1";;
  run)
    trap 'close_app "$S"' EXIT
    adb -s "$S" shell input keyevent KEYCODE_WAKEUP; adb -s "$S" shell wm dismiss-keyguard 2>/dev/null
    adb -s "$S" logcat -c
    adb -s "$S" shell monkey -p "$PKG" -c android.intent.category.LAUNCHER 1 >/dev/null 2>&1
    sleep "${1:-12}"
    adb -s "$S" exec-out screencap -p > Logs/Device/screen.png
    adb -s "$S" logcat -d -s Unity:V AndroidRuntime:E DEBUG:V libc:F > Logs/Device/logcat.txt
    adb -s "$S" shell pidof "$PKG" >/dev/null && echo "RUNNING" || echo "NOT RUNNING"
    grep -E "LMW|Exception|FATAL|signal" Logs/Device/logcat.txt | head -30;;
  close) close_app "$S";;
  *) echo "unknown: $cmd"; exit 2;;
esac
