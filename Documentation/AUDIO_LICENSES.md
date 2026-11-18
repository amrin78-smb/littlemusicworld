# Audio Sources and Licences

All instrument sounds are real recordings (no synthesised beeps). Raw sources are fetched by
`Tools/audio/fetch_samples.py` into `~/.cache/lmw-samples` (not committed); `Tools/audio/build_instruments.py`
produces the trimmed/looped samples under `Assets/Audio/Instruments/` that ship in the game.

## Sample libraries
| Library | Author | Licence | Used for | Attribution |
|---|---|---|---|---|
| VSCO 2 Community Edition | Versilian Studios (upright piano by Simon Dalzell / Ivy Audio) | **CC0 1.0** (public domain dedication) | Upright Piano, Violin, Cello, Harp, Flute, Clarinet, Trumpet, Trombone, Snare, Bass Drum, Cymbals, Congas, Tambourine, Xylophone | not required; credited anyway |
| Salamander Grand Piano V3 | Alexander Holm | **CC BY 3.0** | Grand Piano (velocity layers v6/v12, minor-third zones) | **required** — shown in Parent area ▸ Credits |
| FluidR3_GM SoundFont 3.1 | Frank Wen (Debian `fluid-soundfont-gm`) | **MIT** | Digital Keyboard (EP), Drum Kit, Bongos, Maracas, Acoustic/Electric Guitar, Ukulele*, Banjo, Saxophone, Recorder | licence text kept in credits |

\* General MIDI has no ukulele; the nylon-string guitar recorded in the ukulele's high register (short decay) is
used and named honestly in the asset register.

Rejected: TimGM6mb.sf2 (GPL-2, installed system-wide) — licence unsuitable for redistribution in the app.

## Processing (non-creative, documented for transparency)
mono mixdown → resample (32 kHz tonal, 44.1 kHz bright percussion) → trim leading silence (1.5 ms pre-roll) →
length cap + fade → pitch measured (tuning stored, mislabelled samples re-rooted) → sustain loops for bowed/blown
instruments (whole periods, loudness-flattened, best of 10 crossfades) → per-instrument normalisation.
`Tools/audio/verify_instruments.py` checks pitch (±25 c, ±40 c for stretch-tuned pianos), loop clicks, loop
pumping (< 4 dB) and clipping for every playable note. Status: **25/25 pass** (2026-11-17).

## Melodies
All built-in songs are traditional or public-domain melodies, transcribed note by note for this game into our own
simple C-major arrangements; no recordings, published arrangements or lyrics sheets are copied. Accompaniment
chords are our own.

| Song | Origin | Status |
|---|---|---|
| Twinkle Twinkle Little Star | French melody "Ah! vous dirai-je, maman" (1761); words Jane Taylor (1806) | public domain |
| The Alphabet Song | same 1761 melody; alphabet words (C. Bradlee, 1835) | public domain |
| Mary Had a Little Lamb | Sarah Josepha Hale / Lowell Mason (1830) | public domain |
| Hot Cross Buns | English street cry (18th c.) | public domain |
| Frère Jacques | French traditional round (18th c.) | public domain |
| Row, Row, Row Your Boat | American traditional (published 1852) | public domain |
| London Bridge | English traditional | public domain |
| Old MacDonald Had a Farm | folk song, published 1917 (earlier variants) | public domain |
| Itsy Bitsy Spider | American traditional | public domain |
| Ode to Joy | Beethoven, Symphony No. 9 (1824) | public domain |
| Jingle Bells | James Lord Pierpont (1857) | public domain |
| This Old Man | English traditional | public domain |
| **Wheels on the Bus** | Verna Hills (1939) — an authored song, not anonymous folk | **included at the owner's request (2026-11-18)**; melody transcribed by us, own chords. Probably public domain in the US (no renewal); status elsewhere uncertain — the owner accepted this. |

## Voice
Helper speech is generated offline by text-to-speech (Kokoro-82M, Apache-2.0; VachanaTTS, MIT). No voice
recordings of people are used.
