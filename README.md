# Little Music World

A premium stylised 3D musical playground for young children (Unity 6, Android, offline). Explore a musical world
with Melo the songbird, play 25 real-sampled instruments, follow songs on glowing keys, build an animal orchestra,
record songs in the studio and play rhythm games at the carnival.

## Quick start
```bash
Tools/setup.sh                    # regenerate settings, instruments, prefabs, scenes; validate
Tools/run_tests.sh all            # EditMode + PlayMode tests
Tools/build_android.sh            # release APK (ARMv7 + ARM64 IL2CPP) + APK checks
Tools/device.sh install Builds/Android/LittleMusicWorld.apk
Tools/device_walkthrough.sh       # screenshots of every scene on the test phone
Tools/device_selftest.sh          # on-device audio/performance measurements
```
Art and audio are generated from scripts:
```bash
python3 -I Tools/audio/fetch_samples.py                                        # licensed sources → cache
~/.cache/lmw-audio/venv/bin/python -I Tools/audio/build_instruments.py         # 25 instruments
~/.cache/lmw-audio/venv/bin/python -I Tools/audio/verify_instruments.py        # objective audio QA
Tools/blender/build_assets.sh [names] [--preview]                              # Blender models → Blender/Exports
Tools/branding/build_branding.sh                                               # logo, icon, splash
Tools/voice/generate_voice.sh                                                  # Melo's voice (EN/TH, offline TTS)
```

## Documentation
| File | Contents |
|---|---|
| `Documentation/GDD.md` | game design |
| `Documentation/MASTER_DEVELOPMENT_PROMPT.md` | engineering brief |
| `Documentation/ART_BIBLE.md` | visual rules |
| `Documentation/ARCHITECTURE.md` | systems |
| `Documentation/ASSET_PIPELINE.md` | Blender / audio / voice / branding pipelines |
| `Documentation/AUDIO_LICENSES.md` | every sound and melody source |
| `Documentation/TESTING.md` | how to test and the actual results |
| `Documentation/DEVELOPMENT_LOG.md` | milestone log |
| `Documentation/ENVIRONMENT_REPORT.md`, `CREATIVE_TOOLS.md` | toolchain investigation |
