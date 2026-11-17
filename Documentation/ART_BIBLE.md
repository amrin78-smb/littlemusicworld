# Little Music World — Art Bible

This is the visual reference for every asset. When it disagrees with a model, change the model (or update this
document on purpose).

## 1. Style in one sentence

**A warm, tactile toy-box world:** chunky rounded shapes, soft bevels, rich but calm colour, gentle light, and
instruments that look like the best handmade wooden and lacquered toys. It should feel like an animated film set,
not an asset-store scene.

### Visual principles
1. **Silhouette first.** Every instrument and character has to be recognisable as a black silhouette at 64 px.
   Proportions are exaggerated where it helps (big piano lid, fat drum shells, oversized trumpet bell).
2. **Nothing sharp.** Every edge is bevelled (2–6 % of the part size, 2–3 segments). No knife edges, no
   needle-thin details; thin parts (strings, piano keys, valves) get extra thickness.
3. **Readable at a glance.** One dominant colour per object, one secondary and one accent. Detail goes into
   form and soft material variation, not into noisy texture.
4. **Soft contact.** Baked ambient occlusion is in every albedo atlas, so objects sit in the world and do not float.
5. **Calm movement.** Everything breathes a little (idle bobs, swaying flowers, drifting notes), but motion is
   slow and gentle unless a child caused it.

## 2. Colour palette

| Role | Hex | Notes |
|---|---|---|
| Cream (base, paths, UI panels) | `#FFF4E0` | main light neutral |
| Warm white (highlights) | `#FFFBF3` | |
| Ink (outlines, text, dark details) | `#2E2A4A` | never pure black |
| Sky | `#8FD3FF` → `#DDF3FF` | gradient, top to horizon |
| Grass | `#7CCB5A` / shadow `#4FA04A` | |
| Wood light / dark | `#E4B57E` / `#A8683E` | instruments, props |
| Brass | `#F2C14E` with `#C98F2A` | metal: high smoothness, not metallic=1 (reads better on phones) |
| Celebration accent | `#FF5A8A` | sparkles, rewards |

### Area colour keys
| Area | Primary | Secondary | Accent | Landmark |
|---|---|---|---|---|
| Central Hub | `#FFF4E0` cream | `#7CCB5A` grass | `#F2C14E` gold | music-box bandstand with a treble-clef fountain |
| Piano Paradise | `#5B5FC7` indigo | `#C9B8FF` lilac | ivory `#FFFBF3` | giant grand piano, key-step paths |
| Drum Valley | `#E2553F` terracotta | `#F4A259` apricot | `#3D405B` slate | drum-shaped huts, canyon walls |
| String Town | `#C68B59` honey wood | `#F2C14E` amber | `#2A9D8F` teal | guitar-body houses, harp bridge |
| Wind Village | `#5CC8B5` mint | `#8ECAE6` sky | `#FFB000` brass | windmills, flute-pipe chimneys |
| Orchestra Park | `#C0392B` stage red | `#F4C430` gold | `#2E2A4A` ink | shell bandstand, lanterns |
| Music Studio | `#7B5CD6` violet | `#FF7BAC` pink | `#2EC4B6` teal | dome with sound-wave windows |
| Rhythm Carnival | `#FF5A8A` candy | `#FFD23F` lemon | `#3EC1D3` aqua | striped big-top tent |

### Pitch colours (shared by keys, bells, Studio notes, guided prompts)
C `#FF5A5F` · D `#FFA62B` · E `#FFE156` · F `#8BD346` · G `#2EC4B6` · A `#5B7CFA` · B `#D46CE0`
(sharps take the in-between hues). A child can follow a melody by colour alone.

## 3. Characters

### Mascot — Melo the songbird (original)
- Plump pear-shaped body, height ≈ 0.9 m in world units (child eye-level with the instruments).
- Coral `#FF7A59` body, cream `#FFF4E0` belly and face disc, sunflower `#FFC93C` beak and feet.
- A curled crest that reads as a treble-clef swirl; short rounded wings that act as hands; big glossy eyes
  (ink with two white highlights), soft blush cheeks.
- Personality: curious, encouraging, bouncy. Never scolds; demonstrates by doing.

### Animal musicians
Shared proportions so animation is shared: head ≈ 40 % of height, short body, stubby limbs, mitten hands
(no fingers), eyes on the front of the head, a signature accessory that matches the instrument.

| Musician | Colours | Accessory | Personality in motion |
|---|---|---|---|
| Bear (drums) | `#A8683E` / muzzle `#E4B57E` | red bandana | big, steady, head-bobs on the beat |
| Rabbit (piano) | `#F5F0EA` / ears `#FFB4C2` | bow tie | delicate, bouncy, ears flop with notes |
| Fox (violin) | `#F28C38` / `#FFF4E0` | green scarf | elegant sway, tail swish |
| Elephant (trumpet) | `#9BA8C9` / ears `#C8A6D8` | small hat | proud, trunk lifts on loud notes |
| Cat (guitar) | `#4A4E69` / `#F2E9E4` | star badge | cool, tail flick, foot taps |
| Penguin (percussion) | `#2E2A4A` / `#FFFBF3` | yellow beanie | jittery, waddle-dance |

## 4. Instruments
- Real instrument layout and proportions, simplified: correct number of strings on guitars (6), ukulele (4),
  banjo (5), violin/cello (4); piano keys in the correct black/white pattern; valves on the trumpet, slide on the
  trombone, keys on the sax/clarinet suggested by a few chunky pads.
- Playable parts are oversized and bevelled so a child's finger hits them: piano keys ≥ 110 px wide on screen,
  drum pads ≥ 160 px, strings with ≥ 70 px spacing.
- Materials: lacquered wood (smoothness 0.6–0.75), satin metal (brass smoothness 0.7, chrome 0.8), drum skins
  matte cream (0.2), plastic keys glossy (0.65).
- Every playable surface has a pressed state: moves (key travel, skin dip, string vibration) plus a coloured glow.

## 5. Environments
- Compact dioramas: each area is one readable “stage” around a landmark, ringed by soft hills and trees that block
  the horizon (no empty distance).
- Ground: grass with soft painted gradient, cream cobble paths 2 m wide, rounded kerbs.
- Props: musical motifs everywhere — note-shaped lamp posts, metronome trees, drum stools, flower trumpets.
- Density: a child should never see more than ~25 % empty ground in a frame.

## 6. Materials and textures
- **One baked atlas per model group** (Blender Cycles bake): albedo × soft AO in RGB, smoothness in A.
  Emission map only where things glow.
- Sizes: hero assets (Grand Piano, Melo, landmarks) 1024²; instruments/props 512²; small props share a 1024² group
  atlas. Android import max size 1024, ASTC 6×6 (ETC2 fallback).
- Unity material: URP Lit (hero/near) or Simple Lit (environment), base map + smoothness from albedo alpha,
  no metallic maps (stylised metals use colour and smoothness).

## 7. Lighting
- One warm directional sun (`#FFF1D6`, 1.1–1.3 intensity, 40–50° elevation), gradient ambient sky `#BFE6FF` /
  equator `#FFE9C7` / ground `#7CCB5A`.
- Shadows: soft on capable devices (2 cascades), hard single cascade at ≤ 30 m on low tier.
- Light fog in the area colour to soften the horizon. No bloom on low tier; a subtle bloom on high tier only.

## 8. Animation principles
- Squash and stretch on everything that is played (keys, drum skins, the mascot when it lands).
- Anticipation and overshoot on reactions (0.08 s wind-up, 0.15 s overshoot), never longer than 0.4 s.
- Playing animations are triggered by note events, not looped blindly, so movement matches sound.
- Idle loops are 2–4 s, slightly different per character so a group never moves in lockstep.

## 9. UI visual language
- Big rounded “pebble” buttons (corner radius ≈ 30 % of height) with a 6 px darker bottom lip that presses down.
- Icons, not words: instrument portraits rendered from the actual 3D models; universal glyphs (play ▶, stop ■,
  record ●, home house, back arrow).
- Typeface: rounded geometric sans (Fredoka / Baloo-style) for the logo and the few labels; minimum 36 px at 1080p.
- Child-facing screens: no reading needed, no timers, no fail states. Grown-up screens behind a press-and-hold gate.
- Feedback: every touch makes a sound and a visible response within one frame.
