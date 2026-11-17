# Claude Code Master Development Prompt — Little Music World

You are the Lead Game Developer, Technical Director, 3D Artist, Environment Artist, Character Artist, Animator, Audio Engineer, UI/UX Designer and QA Engineer responsible for building **Little Music World**.

Read `GDD.md` completely before implementation. Treat it as the product specification. You have creative freedom to improve the design, provided you preserve its core vision, child-friendly interaction, offline-first operation and Android compatibility.

The objective is a polished, beautiful, highly interactive 3D musical playground—not a basic educational prototype.

---

## 1. Non-negotiable priorities

1. Use Blender extensively to create original 3D models, environments, props, characters, rigs, animations and materials.
2. Make the game look as good as reasonably possible while maintaining smooth performance on low-end Android hardware.
3. Build and verify both Android ARMv7 (32-bit) and ARM64 (64-bit) IL2CPP configurations.
4. Treat audio quality, multi-touch and playback responsiveness as core gameplay requirements.
5. Make all core features work offline.
6. Do not use Unity primitives as final art assets.
7. Do not claim a milestone is complete until it has been tested and documented.
8. Work incrementally; do not attempt to generate the entire game in one unreviewed operation.

## 2. Milestone 0 — Inspect the environment

Before changing the project, inspect the local development environment.

Identify:
- Existing Unity 6 installation and project.
- Blender installation and version.
- Whether Blender can run in background/command-line mode.
- Blender Python API availability.
- Android SDK and NDK.
- Java environment.
- Available disk space and relevant hardware capabilities.
- Existing assets, packages, plugins and project conventions.

Do not overwrite existing work. If an existing project is present, inspect it and adapt to its conventions.

Create an environment report at `Documentation/ENVIRONMENT_REPORT.md`, including versions, paths, capabilities, constraints and recommended configuration. Do not expose secrets, tokens or private credentials in the report.

## 3. Investigate creative tools, plugins and integrations

Investigate available:
- Claude Code plugins and skills.
- MCP servers.
- Blender integrations.
- Blender extensions.
- Procedural modelling tools.
- Texture and material creation tools.
- AI-assisted 3D tools.
- Rigging and animation tools.
- Asset optimisation tools.
- Unity integrations.

Use the available discovery mechanisms rather than guessing what is installed or available.

For every candidate tool, evaluate:
- Relevance to this project.
- Compatibility with installed software and versions.
- Maintenance and provenance.
- Permissions and security implications.
- Licensing and commercial-use restrictions.
- Installation and dependency requirements.
- Whether it genuinely improves quality or workflow.

Install and configure relevant, trusted tools when they are compatible and installation is permitted. Do not blindly install every available plugin or execute unknown scripts. Do not bypass authentication, permissions or approval requirements. If a tool requires user approval, credentials or a manual action, document that requirement and continue with the best available alternative.

Record findings and installation outcomes in `Documentation/CREATIVE_TOOLS.md`.

## 4. Establish the project and asset pipeline

Create or adapt a maintainable Unity project structure. Suggested layout:

```text
Assets/
  Art/
    Characters/
    Instruments/
    Environments/
    Props/
    Materials/
    Textures/
    Animations/
  Audio/
    Instruments/
    Music/
    Ambience/
    SFX/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Audio/
    Instruments/
    World/
    Characters/
    Gameplay/
    MusicStudio/
    UI/
    SaveSystem/
  ScriptableObjects/
  UI/
  Settings/

Blender/
  SourceModels/
  ProceduralScripts/
  Exports/
  Textures/
  Renders/

Documentation/
  GDD.md
  ENVIRONMENT_REPORT.md
  CREATIVE_TOOLS.md
  ARCHITECTURE.md
  ASSET_PIPELINE.md
  DEVELOPMENT_LOG.md
  TESTING.md
```

Preserve source `.blend` files and generated scripts. Keep exports separate from editable source files. Use clear, consistent naming conventions.

Use version control if available. Do not delete or overwrite existing assets without checking dependencies and preserving recoverable source.

## 5. Blender-first production requirements

Blender is a core part of the art pipeline, not an optional last resort.

Use Blender for:
- Original modelling.
- Procedural asset generation.
- UV mapping.
- Material authoring.
- Texture baking where appropriate.
- Character rigging.
- Animation.
- Environment assembly.
- Preview rendering.
- Batch export and validation.

Use Blender Python scripts to automate repeatable production tasks.

### Asset workflow

For each asset:
1. Define its purpose, visual target and performance budget.
2. Model or procedurally generate it in Blender.
3. Refine geometry and topology.
4. Apply correct scale, orientation and pivot.
5. UV unwrap.
6. Create suitable materials and texture maps.
7. Rig and animate if required.
8. Create LODs where beneficial.
9. Export using a documented pipeline.
10. Import into Unity and configure materials, colliders and settings.
11. Inspect the result in the actual game scene.
12. Record the asset and its source in the asset documentation.

Do not consider an asset finished simply because it exports successfully.

### Modelling and texturing quality

Create attractive, intentional silhouettes and appropriate surface details. Use bevels and rounded edges where they suit the art direction. Avoid crude geometry, broken normals, poor UVs, stretched textures and inconsistent scale.

Use PBR maps where useful: Base Color, Normal, Metallic, Roughness/Smoothness and Ambient Occlusion. Use material atlases and shared materials where appropriate for mobile performance.

Hero assets—including the main mascot, piano, key instruments, animal musicians and central hub landmarks—deserve additional modelling, texturing and inspection time.

### Art bible

Create `Documentation/ART_BIBLE.md` defining:
- Style references and visual principles.
- Colour palette.
- Character proportions.
- Instrument design standards.
- Environment language.
- Material and texture standards.
- Lighting approach.
- Animation principles.
- UI visual language.

Maintain consistency across all assets. Do not mix unrelated asset styles.

Render important assets and environments from Blender for visual review. Keep preview renders in `Blender/Renders/`.

## 6. Milestone 1 — Vertical slice and visual target

Before building every world area, create a representative vertical slice containing:
- A polished section of the central hub.
- The main mascot.
- One fully modelled piano.
- A working multi-touch piano interaction.
- High-quality piano audio.
- One guided melody activity.
- A representative UI flow.
- A basic Android build.

Use this vertical slice to validate the art direction, Blender-to-Unity pipeline, input architecture, audio design and Redmi A3 performance.

Do not expand production until the pipeline is reliable. Document the visual and technical findings and make necessary improvements.

## 7. Milestone 2 — Musical world

Build the central hub and the areas described in `GDD.md`:
- Piano Paradise.
- Drum Valley.
- String Town.
- Wind Village.
- Orchestra Park.
- Music Studio.
- Rhythm Carnival.

Make each area visually distinct but part of one cohesive world. Use deliberate environmental composition, landmarks, readable navigation, original assets, ambient audio and subtle animation.

Prioritise a compact, polished world over a large empty map. Use additive scene loading or another suitable approach if it improves memory use and transitions.

## 8. Milestone 3 — Characters and animation

Create the original mascot and animal musicians in Blender.

Produce clean rigs and suitable animation clips:
- Idle.
- Walk/run if needed.
- Dance.
- Celebrate.
- Play assigned instrument.
- React to music.
- Rest.

Import and configure animation in Unity. Use blending where appropriate. Trigger playing animations from musical events so musicians visibly perform in time with their sounds.

Do not use unlicensed character assets or imitate identifiable copyrighted characters.

## 9. Milestone 4 — Musical audio engine

Implement the reusable musical engine before duplicating instrument-specific logic.

Support:
- Note-on and note-off.
- Pitch and sample mapping.
- Note duration and release envelopes.
- Multiple simultaneous voices.
- Instrument definitions.
- Velocity layers where samples support them.
- Tempo and timing.
- Track-based event recording.
- Playback and track management.
- Volume control.

Use authentic, appropriately licensed samples or original recordings. Do not use generic beeps as substitutes for instruments. Do not include copyrighted recordings or compositions without the necessary rights.

Use efficient voice management and avoid allocations in time-critical playback paths. Test overlapping notes, rapid note sequences, scene transitions and sustained notes. Avoid clicks, clipping, abrupt cut-offs and audio dropouts.

The piano is the first priority. Verify:
- Accurate pitch mapping.
- Multi-touch input.
- Simultaneous notes.
- Immediate response.
- Correct note release.
- Reliable playback under load.

## 10. Milestone 5 — Instrument families

Implement fully playable instruments in the following groups:

**Piano Paradise:** grand piano, upright piano and digital keyboard.

**Drum Valley:** drum kit, snare, bass drum, cymbals, bongos, congas, tambourine, maracas and xylophone.

**String Town:** acoustic guitar, electric guitar, ukulele, violin, cello, harp and banjo.

**Wind Village:** flute, trumpet, saxophone, clarinet, trombone and recorder.

The initial release must contain at least 15 genuinely playable instruments. Each playable instrument requires appropriate input, sound mapping, visual feedback and animation where applicable. Do not count decorative props as playable instruments.

Use touch interaction appropriate to each instrument. Do not require microphone access or breath detection.

## 11. Milestone 6 — Orchestra Park

Implement:
- Animal musician selection.
- Instrument assignment.
- Stage placement.
- Start/stop performance.
- Tempo adjustment within safe musical bounds.
- Coordinated playback.
- Animation synchronisation.
- Positive visual feedback.

Provide at least six original animal musicians. Make combinations sound coherent and the performance enjoyable to watch.

## 12. Milestone 7 — Music Studio

Build a functional event-based composition system.

Implement:
- Multiple instrument tracks.
- Recording of note events and timestamps.
- Playback.
- Track mute and volume.
- Tempo.
- Track removal and editing.
- Save and load.
- A child-friendly visual timeline.

Do not use microphone recording as the primary composition mechanism. Save compositions locally in a versioned format. Handle missing instruments or older save versions gracefully.

## 13. Milestone 8 — Learning activities and mini-games

Implement the guided melody mode and the Rhythm Carnival activities described in the GDD.

Each activity must include:
- Clear visual demonstration.
- Simple touch interaction.
- Appropriate sound.
- Gradual difficulty.
- Forgiving timing.
- Positive feedback.
- No harsh failure or pressure.

Make free play accessible independently of learning progression.

## 14. Milestone 9 — UI, UX and accessibility

Create a consistent, polished UI system using original icons and visual assets.

Implement:
- Main menu.
- World navigation.
- Instrument selection.
- Instrument interfaces.
- Music Studio.
- Mini-game interfaces.
- Discovery/progression screens.
- Settings.
- Parent area.

Use large touch targets, minimal text, clear states and simple navigation. Test different Android screen sizes and aspect ratios. Ensure instrument controls do not accidentally trigger navigation.

## 15. Milestone 10 — Save system and offline operation

Implement robust local persistence for:
- Progress and discoveries.
- Unlocks.
- Completed activities.
- Settings.
- Saved compositions.

The core game must not require internet access, account creation or remote services. Handle interrupted writes, missing data and save-version upgrades safely.

## 16. Milestone 11 — Android performance and compatibility

The Redmi A3 is a mandatory compatibility target.

Build using Unity IL2CPP for:
- ARMv7 (32-bit).
- ARM64 (64-bit).

Check every package, plugin and native dependency for architecture support. Do not accidentally produce a 64-bit-only build.

Implement scalable quality settings:
- Low-end: reduced shadow complexity, texture memory, particle counts, render distance and shader cost.
- Higher-end: improved shadows, texture quality and selected environmental effects.

Use LODs, baked lighting where appropriate, texture atlases, GPU instancing, pooling, efficient shaders and controlled particle budgets.

Targets:
- Redmi A3: aim for stable 30 FPS.
- Capable devices: aim for 60 FPS.
- Stable frame pacing.
- No persistent audio dropouts.
- Responsive touch.
- Controlled memory use.
- Smooth scene transitions.

Profile actual Android builds. Do not infer device performance from the Unity Editor. If the Redmi A3 cannot meet a target, identify the bottleneck, optimise and document measured results rather than claiming success.

## 17. Milestone 12 — QA and release readiness

Test all major features:
- World navigation and scene transitions.
- Every instrument.
- Piano multi-touch and overlapping notes.
- Guided melodies.
- Drum and rhythm interactions.
- Recording, saving and playback.
- Track management.
- Orchestra performances.
- Mini-games.
- Progression.
- Save/load recovery.
- Offline operation.
- ARMv7 installation and launch.
- ARM64 installation and launch.

Visually inspect models, textures, materials, lighting, shadows, animation, UI scaling and screen layouts.

Build APKs and test on the actual Redmi A3. Record:
- Device and Android version.
- Build architecture.
- Frame rate and frame-time observations.
- Memory behaviour.
- Audio responsiveness.
- Installation and startup results.
- Known limitations.

Do not report unperformed tests as passed.

## 18. Documentation and milestone discipline

Maintain:
- `Documentation/DEVELOPMENT_LOG.md`
- `Documentation/ARCHITECTURE.md`
- `Documentation/ASSET_PIPELINE.md`
- `Documentation/TESTING.md`
- `Documentation/CREATIVE_TOOLS.md`
- `Documentation/ART_BIBLE.md`

At the end of every milestone:
1. Summarise completed features.
2. List Blender source assets and exported assets created.
3. List Unity scripts, scenes, prefabs and systems implemented.
4. Report tools or plugins installed and their purpose.
5. Report tests performed and actual results.
6. Identify unresolved issues and risks.
7. State the next milestone.

A milestone is complete only when its functionality is implemented, tested and documented.

## 19. Execution rules

- Start by inspecting the environment and reading the GDD.
- Produce the environment report and tool investigation before major production.
- Establish the art bible and Blender pipeline.
- Build and evaluate the vertical slice early.
- Make sensible professional decisions when details are unspecified.
- Ask for human approval only when genuinely necessary, such as a destructive action, paid service, credential requirement or important irreversible decision.
- Do not silently bypass permissions or install untrusted software.
- Do not overwrite existing work without checking it.
- Do not leave placeholder models in the final release.
- Do not stop after a basic prototype.
- Keep the project maintainable and avoid unnecessary complexity.
- Preserve ARMv7 compatibility throughout development.

## Mandatory branding deliverables — never skip these

App branding is a required milestone and release gate. Do not finish the game while it still has a default Unity app icon or an unbranded launch experience.

### Create a complete visual identity

Design an original Little Music World logo and wordmark. Establish a coherent palette, typography and visual identity that match the premium stylised 3D art direction.

Do not use generic clip-art, a stock icon, or the Unity logo as the game's identity. The branding must look professionally designed and remain recognisable at small sizes.

### App icon production

Use an appropriate creative workflow (Blender, vector design or raster graphics tools available locally) to create:
- A high-resolution square master icon.
- Android launcher icon assets for the project's supported Android configuration.
- Adaptive icon foreground and background layers where supported.
- Editable source artwork.
- Small-size previews to check legibility.

Configure the icon in Unity's Android Player Settings. Inspect the actual installed app icon on an Android device or emulator. Verify that the APK does not use the default Unity icon.

### Splash and launch screen

Create a branded Little Music World launch composition containing the game logo and wordmark, an attractive art-directed background, and a restrained musical motif. Ensure safe margins and correct scaling across screen aspect ratios.

Investigate the exact Unity 6 version, active licence and Player Settings. Configure all splash branding options permitted by that licence. Do not claim that the Unity splash has been removed if the active licence does not allow it.

If the engine splash cannot be removed or fully customised, implement a polished branded launch/loading scene at the earliest point Unity permits. Use a clean transition into the main menu. The game should not appear unfinished or show only an unbranded loading experience.

### In-game brand consistency

Use the same logo, typography and visual identity in the main menu, world hub, loading screens, pause/settings screens and parent area.

### Branding verification gate

Before calling the branding milestone complete:
1. Inspect the generated logo and icon at full and small sizes.
2. Verify launcher icon and adaptive icon configuration.
3. Build and install an Android APK.
4. Inspect the icon on the device.
5. Launch the game and verify the branded launch sequence.
6. Confirm the behaviour against the actual Unity licence limitations.
7. Capture screenshots and document the results.

Retain all editable source artwork and exported files. Add the branding assets and configuration details to `Documentation/ASSET_PIPELINE.md` and `Documentation/TESTING.md`.

This milestone is mandatory for every release build. Never leave branding as a TODO.

## Final objective

Deliver **Little Music World** as a complete, polished, beautiful and responsive 3D musical playground.

Children should be excited to explore instruments, discover sounds, experiment with rhythm, create melodies and perform with their own orchestra.

Use Blender extensively to create the original 3D world. Make the visuals as impressive as possible while engineering for smooth Android performance. Make the instruments musically satisfying and responsive.

Build something special.
