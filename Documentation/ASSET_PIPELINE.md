# Asset Pipeline

All game art, audio, voice and branding are produced by scripts in `Tools/` from original or licensed sources,
then imported by the Unity generators (`Tools/setup.sh`). Source files are kept; outputs are reproducible.

## 3D (Blender 5.2.3, headless)
```
Tools/blender/build_assets.sh [GrandPiano Melo HubEnv …] [--preview]
```
| Step | Implementation |
|---|---|
| Modelling | `Tools/blender/models/<asset>.py` using `lmw_blender.py` (rounded boxes, lathes, sweeps, metaball blobs, outline extrusion), all authored in **Unity coordinates/metres** via `U()` |
| Topology/normals | bevel + weighted-normal modifiers baked into meshes; subdivision where curvature matters (Melo) |
| UVs | smart-project + pack per model or per prop group |
| Materials | procedural Cycles materials (mottle, wood grain, bark, foliage, stripes, rings, bricks, soft patches) |
| Bake | Cycles CPU: diffuse colour, AO, roughness, emission → atlas `T_<name>.png` (RGB = colour × soft AO, A = smoothness) + `T_<name>_E.png` |
| Pivots | `set_origin` / node empties: keys pivot at their hinge, the lid at its hinge, props at base centre |
| Rigging | minimal custom armatures (Melo: 12 bones), rigid + smooth weights, ≤ 2 bones/vertex |
| Animation | scripted clips sampled at 30 fps from pose functions (Idle, Walk, Wave, Dance, Celebrate, Sing, Talk, Sleep, Point, React) |
| LOD | decimated `_LOD1` copies for large props (trees, bandstand) → Unity LODGroup |
| Export | FBX, `axis_forward=Z, axis_up=Y` (verified with the `AxisTest` model), all actions baked |
| Source | `Blender/SourceModels/<name>.blend` |
| Review | `Blender/Renders/<name>_*.png` (Cycles previews) and `Tools/shots.sh` (Unity scene renders) |

Unity import (`ModelImportRules`): scale 1, axis conversion baked, no imported materials, normals imported, no
tangents, characters Generic rig with 2-bone skinning. `ArtLibrary` builds URP Lit/Simple Lit materials from the
atlas, LOD groups, colliders and prefabs; character Animator controllers get one state per clip.

### Assets created
| Asset | Script | Tris (approx.) | Atlas | Notes |
|---|---|---|---|---|
| Grand Piano | `grand_piano.py` | ~18k | 1024² | 49 pivoted keys (C2–C6), propped lid, lyre, casters |
| Melo (mascot) | `melo.py` | ~9.5k | 1024² + emission | 12 bones, 10 clips |
| Hub props | `hub_env.py` | varies | shared 2048² | Bandstand (LOD), GatePiano, LampNote, TreeRound/Blossom (LOD), Bush, FlowerBed, Bench, Plaza, PathSegment |
| Hub ground | `hub_env.py` | ~37k | 1024² | rising hills hide the horizon |

## Audio
```
python3 -I Tools/audio/fetch_samples.py                     # licensed sources → ~/.cache/lmw-samples
~/.cache/lmw-audio/venv/bin/python -I Tools/audio/build_instruments.py [Id …]
~/.cache/lmw-audio/venv/bin/python -I Tools/audio/verify_instruments.py   # objective QA (25/25 pass)
```
`instruments.py` is the single manifest (source, sample range, velocity layers, envelope, play style, playable
notes, string tunings). Output: `Assets/Audio/Instruments/<Id>/*.wav` + `instrument.json`. Unity imports them as
mono Vorbis (q 0.62), Decompress On Load, not preloaded, sample rate preserved (loop points stay exact).
Details and licences: `AUDIO_LICENSES.md`. Notable fixes found by the QA script: upright piano file numbering is
A0 + 2×index (measured), a mislabelled clarinet sample re-rooted, loop seams need guard samples, long crossfades
cancel on vibrato (best-of-10 crossfade search).

## Voice
`Tools/voice/voice_lines.json` (EN + TH) → `Tools/voice/generate_voice.sh` (Kokoro / VachanaTTS, offline) →
`Assets/Resources/Voice/<lang>/<key>.wav`; setup copies the JSON to Resources for the bubble text.

## Branding
`Tools/branding/build_branding.sh` (Blender + numpy) renders, from the real Melo model and the Fredoka font:
`Assets/Art/Branding/Logo_Master.png`, `Splash_Logo.png`, `Icon_Master.png`, `Icon_Background.png`,
`Icon_Foreground.png` (adaptive, 66 % safe zone), `Assets/Resources/Brand/Logo.png`, `LoadingBackground.png`,
`Note.png`, and `Blender/Renders/Icon_SmallSizes.png` (48/72/96/144 px legibility sheet).
`ProjectConfigurator.ConfigureIcons` applies adaptive + round + legacy Android icons;
`ConfigureSplash` sets the splash colours and logos permitted by Unity Personal.

## UI art
`UISprites` (editor) paints all UI shapes and icons with signed-distance functions (pebble buttons, keys, glow,
note, play/home/back/gear/star/lock/listen/check, song pictures). Fonts: Fredoka SemiBold + Mitr (Thai fallback),
both SIL OFL (licence files in `Assets/Art/UI/Fonts`).
