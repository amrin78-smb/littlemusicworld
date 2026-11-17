# Environment Report — Little Music World

Inspected 2026-11-17 on the development machine (Milestone 0). No secrets or credentials are recorded here.

## Machine
| Item | Value |
|---|---|
| OS | Ubuntu 24.04 (Linux 7.0 kernel), x86_64 |
| CPU | Intel Core i5-8365U, 4 cores / 8 threads, 1.6 GHz base |
| GPU | Intel UHD Graphics 620 (no CUDA/OptiX → Blender Cycles bakes on CPU) |
| RAM | 15 GiB + 8 GiB swap |
| Disk | 468 GB, ~132 GB free at start |
| KVM | available (`/dev/kvm`) → hardware-accelerated Android emulator |

## Unity
| Item | Value |
|---|---|
| Editor | **6000.3.27f2** at `~/Unity/Hub/Editor/6000.3.27f2` (Unity Hub + `unity` CLI 1.0.0-beta.15) |
| Licence | **Unity Personal** (from the licensing client log). Consequence: the "Made with Unity" splash cannot be disabled; we configure colours/logos and follow it with our own branded Boot scene. |
| Android module | installed: bundled SDK (platforms 34–37, build-tools 36.0.0), **NDK r27c (27.2.12479018)**, OpenJDK 17.0.18 |
| IL2CPP Android libs | `arm64-v8a`, `armeabi-v7a`, `armv9`, `x86_64` → **ARMv7 + ARM64 IL2CPP builds supported** |
| Render pipeline | URP 17.3 (Mobile + PC assets from the template) |
| Batch mode | works headless under `xvfb-run` (`Tools/unity_batch.sh`) |
| Existing project | none — the repository was empty (git initialised, no commits). Project created from the 6000.3 URP template and trimmed to an offline package set. |

## Blender
| Item | Value |
|---|---|
| Version | **Blender 5.2.3 LTS** (`~/.local/bin/blender`, portable install) |
| Headless | `blender -b --factory-startup --python …` works; Python 3.13 with numpy 2.3 bundled |
| Add-ons available | `io_scene_fbx`, `io_scene_gltf2`, `rigify` (bundled) |
| Render/bake | Cycles CPU (Intel iGPU not used) — a 1024² 4-pass bake takes ~3–6 min |

## Android
| Item | Value |
|---|---|
| SDK | `~/Android/Sdk` (platforms 35/36, build-tools 34–36, cmdline-tools, emulator 37.2.13) |
| System images | `android-36/google_apis/x86_64` — ABI list `x86_64, arm64-v8a` (ARM64 runs through translation; ARMv7 does not) |
| Emulator | AVD **`lmw_api36`** created for this project (Pixel 6 profile, 4 GB, SwiftShader GPU) |
| adb | `~/.local/bin/adb` (platform-tools) |
| Test phone | **Samsung Galaxy S23 Ultra** (SM-S918B, Android 17, Adreno 740, ABIs arm64-v8a + armeabi-v7a) over Wi-Fi adb at `192.168.31.96` — `Tools/device.sh` connects (fixed port 5555 after `adb tcpip`, falls back to the mDNS wireless-debugging port) |
| Budget target | **Redmi A3** (32-bit Android, PowerVR GE8320) — owner's device, not connected to this machine; results must come from the owner |

## Java
- System: OpenJDK 21.0.13 (`/usr/bin/java`).
- Unity's Gradle build uses the bundled OpenJDK 17.

## Audio tooling
- `libfluidsynth3` 2.3.4 and `libsndfile` present system-wide (no CLI fluidsynth, no ffmpeg/sox binaries).
- Python venv `~/.cache/lmw-audio/venv`: numpy, scipy, soundfile, pyfluidsynth (created for the instrument pipeline).
- Local soundfont `TimGM6mb.sf2` is GPL — **not used**. FluidR3_GM (MIT) is fetched instead.

## Voice tooling (from the owner's sibling projects)
- `~/.cache/lbw-tts`: Kokoro-82M ONNX (Apache-2.0, English) and VachanaTTS (MIT, Thai) — offline TTS for the helper's spoken lines.

## Network
- Internet available during development (GitHub, Ubuntu archive, PyPI). The **game itself is offline**: release APKs have no INTERNET permission (verified by `Tools/verify_apk.sh`).

## Constraints and recommended configuration
1. Only one Unity process may open the project at a time; builds (~20 min IL2CPP for two ABIs) block other batch jobs.
2. Cycles bakes are CPU-bound; keep atlases at 1024² for heroes, 512² for props.
3. Emulator cannot run ARMv7 code → ARMv7 verified on the S23 (`adb install --abi armeabi-v7a`) and by the owner on the Redmi A3.
4. Every Android build: IL2CPP, ARMv7 + ARM64, Vulkan with a PowerVR deny filter (GLES3 fallback), landscape, no INTERNET.
5. Audio output forced to 48 kHz (Unity's "system default" resolved to 24 kHz on Android), DSP buffer 256 (512 on low tier).
