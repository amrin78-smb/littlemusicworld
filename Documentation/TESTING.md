# Testing — Little Music World

Only tests that were actually run are marked with a result. "Not yet run" means exactly that.

## How to test
| What | Command | Notes |
|---|---|---|
| Audio QA (offline) | `~/.cache/lmw-audio/venv/bin/python -I Tools/audio/verify_instruments.py` | pitch ±25 c (±40 c pianos/harp/xylophone), loop clicks, loop pumping < 4 dB, clipping, for every playable note; writes `Logs/Audio/<Id>_demo.wav` |
| Unit tests (EditMode) | `Tools/run_tests.sh EditMode` | notes/melody parsing, song tiers, save round-trip/corruption/backup recovery/compositions, offline mixer, arranger |
| Scene tests (PlayMode) | `Tools/run_tests.sh PlayMode` | every scene loads; piano reaches the engine; Follow the Melody completes; every area instrument builds a playable panel |
| Scene renders | `Tools/shots.sh` | `Logs/Shots/*.png` (1920×1080 and 2316×1080) |
| APK checks | `Tools/verify_apk.sh <apk>` (run by `Tools/build_android.sh`) | ARM64 + ARMv7 native code, PowerVR Vulkan deny filter, landscape, no INTERNET in release |
| On-device self-test | `Tools/device_selftest.sh [serial]` | decode times, mixer CPU load, 40-voice burst, scheduling accuracy, held loops, frame times, memory |
| Device play | `Tools/device.sh install|run|close` | always force-stops the app and turns the screen off afterwards |

## Results log

### 2026-11-18 — Milestone 1 (vertical slice), interim
| Test | Device / build | Result |
|---|---|---|
| Audio QA | offline, 25 instruments | **25/25 pass** |
| EditMode | Unity 6000.3.27f2 batch | **23/23 pass** |
| APK verification | release 0.1.0 | arm64-v8a + armeabi-v7a ✔, PowerVR filter ✔, landscape ✔, no INTERNET ✔ |
| Install + launch | emulator `lmw_api36` (Android 16, x86_64 + arm64 translation) | ✔ installs and runs; keyboard touch lights key and starts a voice; 4–5 fps (software GPU + translation: not a performance measure) |
| Install + launch | Galaxy S23 Ultra, Android 17, ARM64 | ✔ 60 fps, ~330 MB PSS |
| Self-test | S23 Ultra, ARM64 | 48 kHz, 256×4 buffer; 40-voice burst: mixer 3.1 % (peak 0.24 ms of 5.33 ms); 16ths @ 180 bpm ×3 tracks: 240 scheduled, 0 late; held loops 0.7 %; 60 fps p95 16.7 ms |
| Self-test | S23 Ultra, **ARMv7** (`--abi armeabi-v7a`, primaryCpuAbi verified) | Low tier, 512×4 buffer, 28 voices; mixer 5.2 %; 0 late of 240; 30 fps, max frame 41.7 ms |
| Redmi A3 | — | **not yet run** (owner's device) |

### Branding verification (gate)
| Check | Status |
|---|---|
| Logo/wordmark inspected at full size | done (`Assets/Art/Branding/Logo_Master.png`) |
| Icon legibility 48/72/96/144 px + adaptive circle | done (`Blender/Renders/Icon_SmallSizes.png`) |
| Adaptive (bg + fg), round, legacy icons configured | done by `ProjectConfigurator.ConfigureIcons` |
| Installed launcher icon inspected on a device | pending next device build |
| Branded launch sequence on device | pending next device build |
| Unity licence limitation | Unity **Personal**: "Made with Unity" splash cannot be removed; configured sky-blue background, our logo shown alongside, then our branded Boot scene |

## Manual checklist (each milestone build)
- [ ] Launch → Unity splash → branded Boot → Main Menu (Melo waves, says hello, bubble shows)
- [ ] Play → Hub; tap ground (Melo walks), tap a gate (Melo walks, announces, area loads)
- [ ] Each area: tap every instrument, play it, Back, Home
- [ ] Piano: two-hand chords, glissando, octave buttons; Songs → lesson → demonstration → follow → reward song
- [ ] Orchestra: add/remove musicians, change instrument, choose song, tempo, play/stop, celebration
- [ ] Studio: record on two tracks, mute, volume, delete note, clear/remove track, save, load from My Songs
- [ ] Carnival: all seven games, success and retry paths
- [ ] Sticker book shows discoveries
- [ ] Parent gear: hold 2 s; volumes, language (Thai bubble + voice), quality, credits
- [ ] Android back button: area → hub → menu; nothing on menu
- [ ] Fully close the app and relaunch (PowerVR crash regression check); save persists
- [ ] Airplane mode: everything works
