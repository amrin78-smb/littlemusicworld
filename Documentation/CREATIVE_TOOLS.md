# Creative Tools, Plugins and Integrations — Investigation

Discovery used: the Claude Code session's skill/plugin and MCP listings, `blender` add-on enumeration, the
`unity` CLI command manifest, the local package lists, and the owner's sibling projects. Nothing was installed
blindly; every candidate is evaluated below.

## Summary of what is used
| Tool | Purpose | Licence / provenance | Installed? |
|---|---|---|---|
| Blender 5.2.3 (headless Python) | all 3D modelling, UVs, materials, Cycles AO/colour bakes, rigs, animation, FBX export, preview renders | GPL (tool only; our outputs are ours) | already installed |
| `Tools/blender/lmw_blender.py` | shared procedural modelling/bake/export library (forked from the owner's Little Builders World pipeline and extended with FBX, pivots, LODs, armatures) | project code | in repo |
| Unity `unity` CLI + batch mode | project creation, headless setup/builds, tests | Unity | already installed |
| FluidSynth (libfluidsynth via pyfluidsynth) | renders FluidR3_GM instruments note by note, effects off | LGPL library, used offline as a tool | pip venv (`~/.cache/lmw-audio`) |
| numpy / scipy / soundfile | sample trimming, resampling, pitch measurement, loop finding, QA | BSD | pip venv |
| Kokoro-82M ONNX + VachanaTTS | offline spoken helper lines (EN + TH) | Apache-2.0 / MIT | present from sibling project |
| Android emulator (AVD `lmw_api36`) | install/launch/UI tests | Google | present |

## Candidates evaluated and not adopted
| Candidate | Evaluation | Decision |
|---|---|---|
| **Claude Code plugins/skills** in this session | Available: artifact/docs/pptx/xlsx/pdf skills, Neon (database), Gmail/Calendar/Drive, Playwright, easel charts. None provides 3D, audio or Unity capability. | Not used for production. |
| **MCP servers** (Neon, Google, Playwright, Claude Docs) | Unrelated to an offline Unity game; Neon/Google would add network dependencies. | Not used. |
| **Unity MCP server** (`unity mcp`) | Lets an agent drive a *running* Editor. Our pipeline is fully reproducible in batch mode (generators rebuild every asset/scene), which is more reliable headless. | Not needed; documented as an option for interactive sessions. |
| **Blender MCP add-ons** (third-party socket servers) | Require installing an unreviewed add-on that opens a local socket and executes received code; adds nothing over direct headless `bpy` scripts. | Rejected (security, no benefit). |
| **Rigify** (bundled) | Full human metarig; our characters are stylised animals/birds with ≤ 20 bones and need lean rigs for budget GPUs (2 bones/vertex). | Not used; custom minimal rigs scripted instead. |
| **AI 3D generators** (Meshy, Tripo, Rodin, etc.) | Cloud services needing accounts/credits; unclear commercial licensing of outputs; inconsistent topology/style; not offline. | Rejected. Everything is modelled procedurally in Blender. |
| **AI texture/image generators** | Same licensing/account concerns; our style relies on baked procedural materials. | Rejected. |
| **Asset-store characters/instruments** | Prohibited by the brief (original art only). | Rejected. |
| **gltfpack / meshoptimizer CLI** | Not installed; Unity's mesh optimisation + Blender Decimate cover our needs. | Not needed. |
| **ImageMagick / Inkscape** | Not installed (no sudo). Icons/branding are painted with Python (numpy) and Blender renders; UI shapes with Unity editor SDF painters. | Not needed. |
| **TimGM6mb.sf2** (local) | GPL-2 soundfont — licence unsuitable for bundling samples in a commercial app. | Rejected; FluidR3_GM (MIT) used. |
| **Salamander Grand Piano V3** | CC BY 3.0, excellent 16-layer grand; attribution required (credits screen + AUDIO_LICENSES.md). | **Adopted** (2 velocity layers, minor-third zones). |
| **VSCO 2 Community Edition** | CC0, high-quality orchestral/percussion multisamples. | **Adopted** for upright piano, strings, woodwinds, brass, percussion. |
| **FluidR3_GM** | MIT soundfont (Debian `fluid-soundfont-gm`). Covers guitars, banjo, sax, recorder, kit pieces, bongos, maracas, EP. | **Adopted** (rendered per note, no reverb/chorus). |
| **Freesound** | Per-sound licences vary; downloads need an account/API key. | Not used. |

## Permissions, security and approvals
- No tool required credentials. Package installs were limited to a user-level Python venv (PyPI) — no system
  changes, no sudo.
- Raw source recordings are cached outside the repository (`~/.cache/lmw-samples`); only processed samples ship.
- Downloads are fetched by `Tools/audio/fetch_samples.py` from the original upstream repositories; licences are
  stored alongside the cache and summarised in `Documentation/AUDIO_LICENSES.md`.

## Owner actions that may be needed later
- Redmi A3 testing (device not attached to this machine).
- A decision on "Wheels on the Bus" (see AUDIO_LICENSES.md).
- If a Unity Pro/Plus licence becomes available, the Unity splash could be disabled (`ProjectConfigurator.ConfigureSplash`).
