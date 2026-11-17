# Architecture — Little Music World

Unity 6000.3.27f2 · URP 17.3 · Input System · uGUI + TextMeshPro · Android (IL2CPP ARMv7 + ARM64), landscape, offline.

## Principles
- **Everything is generated from sources.** Art (Blender scripts), audio (Tools/audio), voice (Tools/voice),
  branding (Tools/branding), prefabs, materials and scenes (editor generators). `Tools/setup.sh` rebuilds the
  project state; never hand-edit generated assets.
- **One musical engine.** Every note in the game goes through `MusicalAudioEngine` — free play, guided melodies,
  Studio playback, Orchestra, mini-games and UI sounds — so timing, voice limits and loudness are consistent.
- **Event-driven visuals.** Keys, musicians and Melo react to note events (`NoteStarted`, `Sequencer.NoteVisual`)
  at the moment the sound starts, never on a blind loop.
- **Small, decoupled services.** `Services` (static) holds the engine, the background-music sequencer, the save
  store and scene flow. Instruments and activities do not depend on a global manager beyond these.

## Runtime layout (`Assets/Scripts`, asmdef `LittleMusicWorld.Runtime`)
| Folder | Key types | Role |
|---|---|---|
| Core | `Services`, `SceneFlow`, `BootLoader`, `QualityTier`, `Tween`, `DeviceSelfTest` | boot before first scene, scene transitions behind a branded curtain, device tiering, tweening, on-device measurements |
| Audio | `MusicalAudioEngine`, `Sequencer`, `InstrumentDefinition`, `InstrumentLibrary`, `SongLibrary`, `NoteUtil` | sampler, beat-time sequencing, data |
| Instruments | `TouchRouter`, `ITouchSurface`, `KeyboardSurface`, `PianoModel3D` | multi-touch input and instrument interfaces |
| Gameplay | `PianoActivity`, `MelodyGuide`, `PianoParadiseController` | activities |
| Characters | `MascotController`, `Guide`, `SpeechBubble` | Melo: animation, voice + bubble |
| World | `HubController`, `AreaPortal` | hub navigation |
| UI | `PressFeedback`, `UISound`, `CanvasAspect`, `MainMenuController`, `ParentGate`, `SettingsPanel`, `SceneButton` | UI behaviour |
| SaveSystem | `SaveStore`, `SaveSerializer`, `SaveData`, `CompositionData` | versioned local persistence |

Editor (`Scripts/Editor`, asmdef `LittleMusicWorld.Editor`): `ProjectSetup` (orchestrator), `ProjectConfigurator`
(player/Android/audio/icons/splash), `InstrumentImporter`, `ArtLibrary` (FBX → materials/prefabs/animator),
`ModelImportRules`, `UISprites` (SDF UI art), `UIKit`, `SceneBuilder`/`WorldBuilder` (scenes), `BuildScript`,
`AndroidManifestPostProcess`, `ProjectValidator`, `SceneShots`.

## Musical audio engine
- **Sampler, not AudioSources-per-note.** One `AudioSource` with `OnAudioFilterRead` mixes all voices. Samples are
  decoded once per instrument into 16-bit arrays (`Load`), so playing never allocates or decodes.
- **Threading:** the main thread writes fixed-size commands into a lock-free single-producer/single-consumer ring;
  the audio thread drains it every buffer. No locks on the audio thread.
- **Timing:** each buffer is anchored to `AudioSettings.dspTime`; commands carry an absolute sample time, so
  sequenced notes start on the exact sample (self-test: 0 late notes on device).
- **Voices:** linear-interpolated resampling from the nearest zone (pitch + fine tune), attack/release envelopes,
  sustain loops (seamless loops prepared offline), velocity layers, per-instrument caps, re-strike damping, choke
  groups (hi-hat), quietest/oldest stealing, 16 mix channels with volume/mute, −5 dB headroom + soft limiter.
- **Output:** 48 kHz forced (Unity's "system default" became 24 kHz on Android); DSP buffer 256 frames
  (512 on the low tier). Route changes (headphones/Bluetooth) re-sync the engine.
- **Voices by tier:** 28 (low) / 40 / 56.

## Input
`TouchRouter` polls Enhanced Touch every frame and tracks each finger from down to up, dispatching to
`ITouchSurface`s by priority. Surfaces hit-test their own targets (black keys before white). Sliding between
targets re-triggers (glissando/strum). UI buttons use the normal EventSystem; instrument areas never overlap
navigation buttons, and scene-changing buttons sit in the top corners.

## Scenes and flow
`Boot` (branded launch, after Unity's Personal splash) → `MainMenu` → `Hub` → areas (`PianoParadise`; others in
Milestone 2). Single-scene loading behind `SceneFlow`'s curtain keeps memory low; services persist.

## Data
- `InstrumentDefinition` ScriptableObjects (generated from `instrument.json`), collected in
  `Resources/InstrumentLibrary`.
- Songs: `SongLibrary` (code data, PD melodies, compact melody strings, chord accompaniments, graded lessons).
- Voice lines: `Tools/voice/voice_lines.json` → `Resources/Voice` (text + clips).
- Save: `persistentDataPath/lmw/save.json` (+ `.bak`), compositions in `compositions/<id>.json`; atomic writes,
  backup fallback, versioned migration, sanitising.

## Quality tiers
`QualityTier.Detect()`: PowerVR / old Mali / Adreno 3xx / < 3.5 GB RAM / 32-bit process / ≤ 4 cores → Low
(30 FPS, render scale 0.8, no MSAA, 1 hard cascade 22 m, LOD bias 0.7). ≥ 7 GB and ≥ 8 cores → High (60 FPS,
4× MSAA, 2 cascades 45 m). Otherwise Medium. The parent area can override.
