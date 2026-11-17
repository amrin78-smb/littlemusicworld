# Little Music World — Game Design Document

**Project:** Little Music World  
**Engine:** Unity 6  
**Platform:** Android  
**Art direction:** Premium stylised 3D  
**Primary audience:** Young children  
**Development tools:** Claude Code, Blender, Unity 6  
**Architecture:** Android ARMv7 (32-bit) and ARM64 (64-bit), IL2CPP  
**Connectivity:** Offline-first

---

## 1. Game vision

Little Music World is an interactive 3D musical playground where children explore musical environments, discover instruments, play melodies, learn rhythms, create compositions and build their own orchestra.

It must feel like one coherent, living musical world rather than a collection of disconnected educational mini-games.

Children should be able to:
- Explore colourful musical environments.
- Discover and interact with instruments.
- Play freely without restrictions.
- Learn simple melodies and rhythms.
- Experiment with different musical sounds.
- Record and replay their own compositions.
- Combine instruments and build an orchestra.
- Play musical mini-games.
- Unlock new instruments, characters, decorations and activities.

The experience should reward curiosity and experimentation, never punish mistakes harshly.

## 2. Design pillars

1. **Visual excellence:** Premium stylised 3D, original models, detailed materials, attractive lighting and polished animation.
2. **Musical authenticity:** Recognisable instrument sounds, responsive playback and satisfying interaction.
3. **Creative freedom:** Free play is always available and is not gated behind progression.
4. **Intuitive interaction:** A young child can understand the experience primarily through visual demonstration and touch.
5. **Technical excellence:** Smooth frame pacing, fast loading, responsive controls and stable audio on Android, including the Redmi A3.

## 3. Target audience and experience

Design for young children, with simple interactions and optional parent-facing settings. Do not assume that the player can read confidently.

- Large touch targets and clear visual affordances.
- Minimal text and simple spoken or animated instructions where appropriate.
- No timers or failure states in free play.
- Gentle, positive feedback.
- Easy recovery from mistakes.
- No advertisements, external purchases or unnecessary online dependencies.

## 4. Art direction

### 4.1 Visual target

Use premium stylised 3D inspired by polished animated films and high-quality family adventure games. The world should feel warm, tactile, colourful and carefully art-directed—not like a generic asset-store scene.

Priorities:
- Strong, readable silhouettes.
- Attractive proportions and appealing character design.
- Clean geometry and considered surface detail.
- Rich but controlled colour palettes.
- PBR materials where they add value.
- Soft, deliberate lighting and shadows.
- Environmental movement and subtle atmospheric effects.
- Consistent visual language across every area.

Avoid primitive Unity shapes as final assets, flat placeholder materials, visual clutter, noisy textures and excessive post-processing.

### 4.2 Blender-first asset pipeline

Blender is a first-class production tool. Use it for original modelling, procedural asset generation, UV mapping, material authoring, rigging, animation, scene assembly and preview rendering.

Important assets should have:
- Clean, appropriate topology.
- Correct scale, orientation and pivot.
- Proper UV mapping.
- Sensible material separation.
- Optimised polygon counts.
- LODs where useful.
- Consistent naming and export settings.

Use Blender Python scripting to automate repeatable work. Export to Unity using a reliable, documented FBX or other suitable pipeline. Preserve source `.blend` files.

### 4.3 Materials and textures

Use PBR maps selectively and appropriately:
- Base Color / Albedo.
- Normal.
- Metallic where relevant.
- Roughness or Unity Smoothness.
- Ambient occlusion where beneficial.

Use texture atlases, shared materials and sensible texture sizes to manage memory and draw calls. Hero assets may receive more texture detail; distant props should use lighter settings. Do not increase texture resolution without a visual or technical reason.

## 5. World structure

The initial world is a compact, polished hub connecting distinct musical areas. Prioritise density, discoverability and quality over a huge empty map.

### 5.1 Central Musical Hub

The central hub introduces the world and provides intuitive routes to each area.

Include:
- Clear visual landmarks.
- Distinct entrances to each area.
- A welcoming mascot.
- Optional collectibles and discovery objects.
- Attractive environmental composition.
- Short, smooth transitions.

### 5.2 Piano Paradise

**Focus:** Piano and keyboard.

Instruments and activities:
- Grand piano.
- Upright piano.
- Digital keyboard.
- Free-play keyboard.
- High/low note discovery.
- Guided melodies.
- Illuminated-key follow mode.

Piano keys must respond immediately, support multi-touch and overlapping notes, and provide clear visual feedback.

### 5.3 Drum Valley

**Focus:** Drums and percussion.

Include a drum kit, snare, bass drum, cymbals, bongos, congas, tambourine, maracas and xylophone.

Activities:
- Tap individual drums.
- Make free-form rhythms.
- Repeat simple patterns.
- Explore fast and slow beats.
- Combine percussion instruments.

### 5.4 String Town

**Focus:** String instruments.

Include acoustic guitar, electric guitar, ukulele, violin, cello, harp and banjo.

Interactions should suit each instrument: strumming, plucking, bowing or selecting notes. Do not present decorative instruments as playable if they have no functional interaction.

### 5.5 Wind Village

**Focus:** Wind instruments.

Include flute, trumpet, saxophone, clarinet, trombone and recorder. Provide note exploration, sound recognition and simple pitch-matching activities. Use touch-friendly interfaces; do not require microphone input or breath detection.

### 5.6 Orchestra Park

A collaborative performance area where children can:
- Select animal musicians.
- Assign instruments.
- Arrange musicians on a stage.
- Start and stop performances.
- Change tempo within suitable bounds.
- Experiment with combinations.
- Watch musicians perform together.

Initial character ideas:
- Bear drummer.
- Rabbit pianist.
- Fox violinist.
- Elephant trumpet player.
- Cat guitarist.
- Penguin percussionist.

Use original designs. Give each musician a recognisable personality through movement and performance animation. Synchronise visible playing actions with musical events as closely as practical.

### 5.7 Music Studio

A child-friendly composition space.

Features:
- Instrument selection.
- Multiple instrument tracks.
- Note-event recording.
- Playback and replay.
- Tempo adjustment.
- Track volume.
- Track muting and removal.
- Save, load and edit compositions.
- Visual timeline with clear, colourful track identities.

Record musical events and timing data, not microphone input. Keep the initial editing workflow simple and forgiving.

### 5.8 Rhythm Carnival

A collection of short musical activities:
- **Rhythm Match:** Listen to a rhythm and repeat it.
- **Sound Detective:** Match an instrument sound to its picture.
- **Follow the Beat:** Tap along with a beat.
- **Musical Memory:** Repeat a sequence of notes.
- **High or Low:** Identify pitch direction.
- **Instrument Orchestra:** Identify instruments in a short combination.
- **Melody Builder:** Arrange notes into a simple melody.

Use gradual difficulty progression, generous timing and positive feedback.

## 6. Gameplay modes

### 6.1 Free Play

The core experience. Every available instrument can be played without objectives, time limits or penalties. Avoid forced progression and unnecessary menus.

### 6.2 Learn Music

Introduce concepts progressively:
1. Recognise instruments by appearance and sound.
2. Explore different timbres.
3. Understand high and low notes.
4. Explore fast and slow rhythms.
5. Repeat musical patterns.
6. Follow simple melodies.
7. Combine instruments.

Use visual demonstrations and sound rather than lengthy text.

### 6.3 Follow the Melody

Show illuminated keys or note prompts for a short melody. Start with three or four notes and gradually increase complexity.

Suggested tiers:
- Beginner: 3–4 notes.
- Easy: 5–6 notes.
- Intermediate: 7–10 notes.
- Advanced: longer melodies and rhythm variations.

Use original compositions and public-domain melodies. Any other music must be properly licensed. Do not reproduce copyrighted recordings or arrangements without permission.

### 6.4 Create Music

Allow free-form performance, recording, track layering, playback and saving. The player should be able to experiment without needing to understand formal music theory.

### 6.5 Orchestra Creator

Select musicians, assign instruments and create a performance. Make the result visually entertaining and musically coherent.

## 7. Progression and rewards

Use discovery-based progression:
- New instruments.
- New animal musicians.
- Decorations and stage items.
- New musical areas.
- New melodies and activities.

Keep core free-play instruments accessible. Collectibles are optional and must not create pressure or require excessive repetition. Avoid manipulative reward loops.

## 8. Characters and animation

Create an original central mascot to welcome the player, introduce activities, demonstrate interactions and celebrate discoveries.

Create original animal musician models with suitable rigs and animation clips.

Required animation categories:
- Idle.
- Walk and run, if locomotion is used.
- Dance.
- Celebrate.
- Play assigned instrument.
- React to music.
- Rest or sleep.

Use animation blending in Unity. Instrument-playing animations should be event-driven where possible.

## 9. Audio and musical engine

Audio responsiveness and quality are core features.

Use authentic sampled or professionally produced instrument sounds. Do not represent instruments with generic beeps.

The reusable musical engine should support:
- Note-on and note-off events.
- Pitch mapping and sample selection.
- Note duration and release.
- Multiple simultaneous voices.
- Instrument configuration.
- Velocity layers where available.
- Tempo and timing.
- Recording and playback.
- Track management.
- Volume control.

Use suitable sample rates, compression and import settings. Avoid abrupt cut-offs, clipping and unnecessary allocations during gameplay. Use pooled or otherwise efficiently managed audio voices.

The piano is the first audio priority. It must support multi-touch, overlapping notes, accurate pitch mapping and responsive playback.

## 10. Controls, camera and UI

### Controls
- Touch-first.
- Large interactive targets.
- Reliable multi-touch for instruments.
- No accidental navigation while playing keys or drums.
- Clear pressed, active and selected states.

### Camera
Use a third-person/isometric hybrid camera. Provide smooth movement and transitions, sensible bounds and clear sightlines. The player should not easily lose the playable area.

### UI
Create a consistent visual design system with:
- Main menu.
- World hub navigation.
- Instrument selection.
- Instrument interaction screens.
- Music Studio.
- Mini-game interfaces.
- Progression and discovery screens.
- Settings.
- Parent area.

Use original icons and graphical elements. Keep text minimal and layouts understandable without reading.

## 11. Technical architecture

Use a modular Unity 6 architecture. Suggested systems:
- GameManager.
- WorldManager.
- PlayerController.
- CameraController.
- InstrumentManager.
- MusicalAudioEngine.
- PianoSystem.
- DrumSystem.
- StringInstrumentSystem.
- WindInstrumentSystem.
- MelodyLearningSystem.
- RhythmGameManager.
- OrchestraManager.
- MusicRecordingSystem.
- CompositionManager.
- ProgressionManager.
- SaveSystem.
- UIManager.
- SettingsManager.

Use ScriptableObjects for instrument definitions, sample mappings, lessons, melodies and unlockable content. Use event-driven communication where useful. Avoid tightly coupling individual instruments to a global manager.

## 12. Save system and offline support

The game must work fully offline.

Persist:
- Progress.
- Unlocked instruments and characters.
- Discovered areas.
- Completed activities.
- Optional collectibles.
- User compositions.
- Settings.

Use robust local storage with versioning and safe handling of corrupted or missing save data. Do not require account creation.

## 13. Android performance and compatibility

The Redmi A3 is a mandatory low-end compatibility target.

Build architectures:
- ARMv7 (32-bit).
- ARM64 (64-bit).
- IL2CPP.

Verify that all packages, plugins and native dependencies support both architectures where required. Do not make the game 64-bit-only.

Performance objectives:
- Target stable 30 FPS on Redmi A3.
- Target 60 FPS on capable devices.
- Stable frame pacing.
- No persistent audio dropouts.
- No noticeable touch latency.
- Controlled memory use.
- Smooth scene transitions.

Use LODs, occlusion culling where useful, GPU instancing, texture atlases, object pooling, baked lighting where appropriate, optimised shaders and sensible particle budgets.

Provide scalable quality settings. Low-end settings should reduce shadow complexity, texture memory, particle counts, render distance and shader cost while retaining the game's visual identity. Higher-end devices may use improved shadows, textures and effects.

Profile on the actual device. Unity Editor performance is not evidence of Android performance.

## 14. Initial release scope

The first release should deliver:
1. Complete central musical hub.
2. Piano Paradise.
3. Drum Valley.
4. String Town.
5. Wind Village.
6. Orchestra Park.
7. Music Studio.
8. Rhythm Carnival.
9. At least 15 genuinely playable instruments.
10. At least 6 animal musicians.
11. Guided melody mode.
12. Musical event recording and playback.
13. Discovery and progression.
14. Local save system.
15. Parent settings.
16. Offline operation.
17. ARMv7 and ARM64 Android builds.

Prioritise complete, polished experiences over empty environments or a large number of unfinished features.

## 15. Quality acceptance criteria

A feature is not complete merely because it compiles.

A milestone must be:
- Implemented.
- Functionally tested.
- Visually inspected.
- Audio-tested where relevant.
- Documented.
- Checked for performance impact.

The final game should feel cohesive, polished, responsive and genuinely enjoyable.

## 15. App branding, icon and launch experience

Branding is a required release deliverable, not optional polish.

### 15.1 App identity

Create an original, professional visual identity for Little Music World:
- A distinctive game logo and wordmark.
- A memorable app icon that remains legible at small sizes.
- A consistent colour palette and typography system.
- Matching visual treatment across the app icon, splash/launch screen, main menu, loading screens and store artwork.

The logo should be designed specifically for this game. Do not use a generic music-note clip-art image or a Unity logo as the game's identity.

### 15.2 App icon

Create an original high-quality icon featuring a clear, recognisable focal design that communicates music and the game's visual world. It should be attractive at small sizes and avoid tiny details or excessive text.

Deliver:
- Editable source artwork where practical.
- A high-resolution square master.
- Android launcher icon assets in the formats and density/adaptive-icon configuration required by the selected Unity/Android pipeline.
- Foreground and background layers for adaptive icons where supported.
- A preview showing how the icon appears at small launcher sizes.

Configure the icon in Unity's Android Player Settings and verify it in an installed APK on a device. Do not leave the default Unity icon.

### 15.3 Splash and launch branding

Create an original Little Music World launch composition with:
- The game logo and readable wordmark.
- A polished background consistent with the game's art direction.
- A restrained musical visual motif.
- Appropriate safe margins for different screen aspect ratios.

Configure the actual Unity/Android launch experience. Investigate the installed Unity version and licence limitations. If the Unity Personal licence or another configuration prevents replacing or removing the engine splash, do not claim it has been removed. Instead, configure all permitted branding options and implement a branded first-party launch/loading scene at the earliest point technically available. Avoid a blank screen or an unbranded transition where possible.

The player must see Little Music World branding rather than a launch experience that only communicates “Made with Unity”, to the extent permitted by the active Unity licence and platform.

### 15.4 In-game brand consistency

Use the logo, typography, palette and visual language consistently in:
- Main menu.
- World hub.
- Loading screens.
- Pause/settings screens.
- Parent area.
- Any future store screenshots and promotional artwork.

### 15.5 Branding acceptance criteria

Before release, verify:
- The installed Android app displays the custom launcher icon.
- Adaptive icon appearance is correct.
- The game logo and wordmark are readable.
- The branded launch experience appears at the earliest permitted point.
- There is no unintended default Unity launcher icon.
- Branding scales correctly across supported resolutions.
- Source artwork is retained and documented.
