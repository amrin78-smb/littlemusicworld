# Little Music World — Claude Code Project Rules

- Product authority: `Documentation/GDD.md`; engineering brief: `Documentation/MASTER_DEVELOPMENT_PROMPT.md`. Read both before feature work. Status lives in `Documentation/DEVELOPMENT_LOG.md` and `Documentation/TESTING.md` — keep them honest (never report unperformed tests as passed).
- Unity **6000.3.27f2** (Personal), URP 17.3, Input System, uGUI + TextMeshPro. Android landscape, touch-first, offline. Project at repo root; runtime code `Assets/Scripts` (asmdef `LittleMusicWorld.Runtime`), generators `Assets/Scripts/Editor`.
- **Generated, never hand-edited:** art (`Tools/blender`), audio (`Tools/audio`), voice (`Tools/voice`), branding (`Tools/branding`), materials/prefabs/scenes (`Tools/setup.sh`). Change the generator, then regenerate.
- Every note goes through `MusicalAudioEngine` (sampler). No per-note AudioSources, no allocations in play paths. Audio QA: `Tools/audio/verify_instruments.py` must stay 25/25.
- Budget-phone rules (Redmi A3: 32-bit Android, PowerVR GE8320): IL2CPP **ARMv7 + ARM64**, Vulkan deny filter `.*PowerVR.*` (GLES3 fallback), verify each APK with `Tools/verify_apk.sh`. Close-and-relaunch must be part of device tests.
- Test phone: Galaxy S23 Ultra over Wi-Fi adb (`Tools/device.sh`). **After every install/test: force-stop the app and turn the screen off** (the scripts do this). `adb install --abi armeabi-v7a` exercises the 32-bit build on it. Emulator AVD `lmw_api36` (x86_64 + arm64 translation; no ARMv7).
- Milestone loop: build → test on emulator → install/play on the S23 → document → next milestone.
- Child-facing UI: no reading required (voice + bubble for every instruction, EN + TH), no timers, no fail states, targets ≥ 150 px at 1080p, navigation in the top corners away from instruments. Grown-up screens behind the 2-second hold gear.
- No ads, analytics, accounts, network, IAP. Keep analytics/cloud packages out of `Packages/manifest.json`.
- Only original or clearly licensed assets; record sources in `Documentation/AUDIO_LICENSES.md` / `ASSET_PIPELINE.md`. Songs: public-domain melodies transcribed by us only.
- Commands: `Tools/setup.sh`, `Tools/run_tests.sh [EditMode|PlayMode|all]`, `Tools/shots.sh`, `Tools/build_android.sh [dev]`, `Tools/device.sh install|run|close`, `Tools/device_selftest.sh [serial]`, `Tools/blender/build_assets.sh [names] [--preview]`, `Tools/branding/build_branding.sh`, `Tools/voice/generate_voice.sh`. Only one Unity process may open the project at a time.
