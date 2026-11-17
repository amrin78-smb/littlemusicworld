# Development Log — Little Music World

Newest first. Each milestone lists features, assets, systems, tools, tests (actual results), risks and next steps.

---

## Milestones 2–10 — World, characters, engine, instruments, orchestra, studio, carnival, UI, saves (2026-11-18)

### Built
- **M2 Musical world:** hub with seven themed gates (piano keys, drum stack, guitar neck, wind pipes, orchestra shell,
  headphones, carnival stripes), plaza, lamps, benches, flower beds, tree ring, rolling-hill ground; tap-to-walk with
  routing around the bandstand; ten optional golden-note collectibles. Areas: Piano Paradise, Drum Valley, String Town,
  Wind Village (shared `AreaController`: overview → tap instrument → camera glides to it → play panel),
  Orchestra Park, Music Studio, Rhythm Carnival. Gentle background music in menu/hub only.
- **M3 Characters:** Melo (12 bones, 10 clips) + six original animal musicians from a parametric generator
  (15 bones, 18 clips each: Idle, Walk, Dance, Celebrate, Rest, React, Hold/Play × 6 playing styles); playing clips are
  re-triggered on each sequenced note (`MusicianController.Hit`).
- **M4 Engine:** see Milestone 1; plus `Arranger` (melody/harmony/chords/bass/beat/groove from any song for any
  instrument), `Sequencer.Looped`, audio-route recovery.
- **M5 Instruments (25 playable):** 3D models with animated parts (keys, pads, strings, wind keys, slide, bow) and
  runtime play panels: keyboard (3 pianos), drum pads (kit laid out like a real kit, snare, bass drum, cymbals,
  bongos, congas, tambourine, maracas), mallet bars (xylophone), strings with chord buttons (acoustic/electric guitar,
  ukulele, banjo), harp strings, hold-to-play bowed notes (violin, cello) and wind holes (flute, trumpet, saxophone,
  clarinet, trombone, recorder). Follow the Melody works on every melodic instrument (songs filtered to fit).
- **M6 Orchestra Park:** six musicians (Bear drums, Rabbit piano, Fox violin, Elephant trumpet, Cat guitar, Penguin
  maracas); invite/remove, reassign instruments, song cards, tempo (turtle↔rabbit 0.7–1.3×), coordinated playback,
  musicians animate on their own notes, confetti + celebration; musicians unlock by discovering their instrument.
- **M7 Music Studio:** 4 tracks × 4-bar loop, count-in, overdub recording of note events (eighth-note quantise),
  timeline lanes with tap-to-delete notes, mute/volume/clear/remove, tempo, save/load/delete (hold) of compositions.
- **M8 Learning:** Follow the Melody (4 tiers: 3–4, 5–6, 7–10 notes, whole song) + Rhythm Carnival: Rhythm Match,
  Sound Detective, Follow the Beat, Musical Memory, High or Low, Instrument Orchestra, Melody Builder — each with
  gentle levels, demonstrations instead of failure.
- **M9 UI/UX:** consistent pebble buttons, SDF icons, rendered portraits of every instrument/character, speech
  bubble + voice (EN/TH) for every instruction, sticker book (discoveries, musicians, areas, stars), parent area
  (hold gear: volumes, language, quality, credits, on-screen device check), Android back navigation.
- **M10 Saves/offline:** versioned JSON, atomic writes + backup recovery, compositions per file; no INTERNET.

### Tests (editor)
- EditMode 23/23; PlayMode 15/16 before the drum models were rebuilt (all scenes load; every area instrument builds
  a panel and sounds; Follow the Melody completes; orchestra musicians animate on notes; studio records/saves/loads;
  all seven carnival games start and clean up).

## Milestone 1 — Vertical slice (complete, 2026-11-18)

### Built so far
- **Audio engine** (`MusicalAudioEngine`): sampler with lock-free command ring, sample-accurate scheduling anchored
  to the DSP clock, velocity layers, sustain loops, envelopes, voice stealing, choke groups, 16 channels, headroom +
  limiter, route-change recovery, audio-thread instrumentation. 48 kHz output forced, 256-frame buffer (512 low tier).
- **25 instruments** built from licensed recordings (VSCO 2 CE CC0, Salamander CC BY, FluidR3 MIT) with objective
  QA (25/25 pass).
- **Multi-touch piano**: `TouchRouter` + `KeyboardSurface` (per-finger tracking, glissando, velocity by touch
  position, pitch-colour dots, press/glow/guide states); three pianos selectable; octave shift.
- **Follow the Melody** (`MelodyGuide`): demonstration with lit keys → child presses the shining key; no timer or
  failure; hints after 3 wrong keys or 6 s; celebration + full song with accompaniment as reward; progress saved.
- **Song library**: 12 public-domain melodies (own transcriptions) with chord accompaniments; 4 difficulty tiers
  derived to the GDD's note counts.
- **Melo**: original rigged mascot (Blender) with 10 clips; `MascotController` (crossfades, walking, talking while
  speaking, dancing/reacting to notes); `Guide` voice + speech bubble (offline TTS, EN + TH, 22 lines).
- **Grand Piano**: hero model with 49 pivoted keys animated in sync (`PianoModel3D`).
- **Hub section**: bandstand landmark, piano gate, lamps, trees, flower beds, benches, plaza, paths, ground with
  hills; tap-to-walk navigation (`HubController`, `AreaPortal`).
- **UI flow**: Boot (branded) → Main Menu (logo, Melo, Play) → Hub → Piano Paradise; parent gate (2 s hold) with
  volumes, language, quality tier, credits; SDF-painted icons; Fredoka/Mitr fonts.
- **Branding** pipeline (logo, icon master, adaptive layers, splash logo, loading background, small-size sheet).
- **Save system**: versioned JSON, atomic writes with backup recovery, sanitising, compositions.
- **Tooling**: setup/build/verify/test/shots/device/self-test scripts; Blender asset and branding builders.

### Tests so far (actual results)
| Test | Result |
|---|---|
| Audio QA (`verify_instruments.py`) | 25/25 instruments pass pitch / loop click / loop pump / clipping checks |
| EditMode tests | 18/18 pass (notes, songs/tiers, save round-trip/corruption/backup recovery, offline mixer: pitch, loops, release, polyphony cap, sample-accurate scheduling, mute) |
| APK checks (`verify_apk.sh`) | arm64-v8a + armeabi-v7a, PowerVR Vulkan deny filter present, landscape, no INTERNET |
| Emulator (lmw_api36, x86_64 + arm64 translation) | installs, launches, keyboard press lights key and starts a voice (4–5 fps under software GPU + ARM translation: not a performance indicator) |
| S23 Ultra, ARM64 self-test | 48 kHz / 256×4 buffer; 40-note burst: mixer 3.1 % of buffer time (peak 0.24 ms of 5.33 ms); 3-track 16ths @ 180 bpm: 240 scheduled notes, **0 late**; 60 fps (p95 16.7 ms) |
| S23 Ultra, **ARMv7** self-test (`--abi armeabi-v7a`) | 32-bit process → Low tier, 512×4 buffer, 28 voices, mixer 5.2 %, 0 late notes, steady 30 fps (max frame 41.7 ms) |

### Issues found and fixed
- Unity "system default" sample rate became **24 kHz** on Android → forced 48 kHz (settings + runtime reset).
- Mixer too hot (limiter active 70 % of buffers) → −5 dB headroom.
- Audio QA caught: upright piano file numbering (A0 + 2×index), a mislabelled clarinet sample, loop seams
  needing guard samples, vibrato loops cancelling under long crossfades.
- Keyboard did not span 20:9 screens → stretched to screen width.
- Blender: outline inset sign, lid prop, Melo belly/face patches (per-face → procedural smooth mask).

### Device verification (S23 Ultra, release APK)
- Launch → Unity Personal splash (sky blue) → branded Boot → Main Menu renders (logo, Melo, bandstand, Play).
- Piano Paradise: grand piano framed above the 15-key multi-touch keyboard; Back/Songs/parent gear present.
- Installed launcher icon is the custom Melo icon (App info), "No permissions required", "No data used".
- Two launch crashes found and fixed during verification:
  1. a build made while Blender wrote into `Assets/` → exports are now staged in `Blender/Exports` and synced by setup;
  2. "level1 is corrupted! Position out of bounds": MonoBehaviours declared in files with other names
     (e.g. `MainMenuController` in `MenuUI.cs`) cannot be deserialised by the player → one class per file, and the
     validator now fails the setup if this ever happens again.

### Open
- Redmi A3 measurements need the owner's device.
- Upright piano and digital keyboard currently share the grand piano model in Piano Paradise (own models: M5).

---

## Milestone 0 — Environment and tools (complete, 2026-11-17)
- Environment inspected and documented (`ENVIRONMENT_REPORT.md`): Unity 6000.3.27f2 Personal with Android
  IL2CPP ARMv7/ARM64, NDK r27c, Blender 5.2.3 headless, Android SDK/emulator, KVM.
- Tools investigated (`CREATIVE_TOOLS.md`): adopted Blender headless scripting, FluidSynth renderer, numpy/scipy,
  offline TTS; rejected AI 3D/texture services, Blender MCP add-ons, GPL soundfont, asset-store content.
- Project created from the URP template, trimmed to an offline package set; repository structure, `.gitignore`,
  art bible (`ART_BIBLE.md`), audio licences (`AUDIO_LICENSES.md`).
- Test phone (Galaxy S23 Ultra) connected over Wi-Fi adb; 7-day authorisation timeout disabled; fixed port 5555.
