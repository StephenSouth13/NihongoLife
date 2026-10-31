# Audio credits

All music and ambience below is **CC0 (public domain)**, downloaded from OpenGameArt by `_work/audio_fetch.py`
(the licence is checked on each page before download) and processed by `_work/audio_process.py`.
Story voices are generated with Microsoft Edge neural text-to-speech (`_work/voice_gen.py`).

| Used as | Title | Author | Source |
|---|---|---|---|
| Music/menu_bgm | Jaoan | Tozan | https://opengameart.org/content/jaoan |
| Music/city_day, Music/konbini | Happy Lo-Fi (Lofi Collection) | Holizna | https://opengameart.org/content/happy-lo-fi-lofi-collection |
| Music/city_night, Music/station, Music/bedroom | Chill Beats (Collection) | Holizna | https://opengameart.org/content/chill-beats-collection |
| Music/sushi | Oriental Somber | Tozan | https://opengameart.org/content/oriental-somber |
| Music/school | Hyoshi Tiny Sparrow Short 1 | Tozan | https://opengameart.org/node/155584 |
| Music/game_center | 4 Chiptunes (Adventure) | SubspaceAudio | https://opengameart.org/content/4-chiptunes-adventure |
| Music/island | vintage hawaii | Tarush Singhal | https://opengameart.org/content/vintage-hawaii |
| Ambience/city_day, Ambience/river | Park ambiences | Thimras | https://opengameart.org/content/park-ambiences |
| Ambience/city_night | Crickets Ambient Noise - loopable | Wolfgang_ | https://opengameart.org/content/crickets-ambient-noise-loopable |
| Ambience/island | Birds and Wind - Ambient, Birds, Wind and Synth | Spring Spring | https://opengameart.org/content/birds-and-wind-ambient-birds-wind-and-synth |
| Jingles/quest_complete, Jingles/purchase, Jingles/unlock, Jingles/level_up | 85 Short music jingles | Kenney | https://opengameart.org/node/22000 |

## Original music (NihongoLife)

Composed and synthesised from code by `Tools/audio/compose_music.py` (no samples, no third-party audio; the project's own
work). Rebuild with `python Tools/audio/compose_music.py [key ...]` (needs numpy, scipy and ffmpeg). Each file is a
seamless loop.

| Used as | Title | Instruments |
|---|---|---|
| Music/clinic | クリニック · Quiet waiting room | marimba, soft pads (F major pentatonic, 76 bpm) |
| Music/rain | あめの ひ · Ame no Hi | lo-fi electric piano, brushes, vinyl dust (68 bpm) |
| Music/sick | おかゆ · Get well soon | music-box lullaby in 3/4 (60 bpm) |
| Music/spring | さくらみち · Sakura Michi | koto-style plucks and a breathy flute in the yo scale (92 bpm) |
| Music/work | いそがしい シフト · Busy shift | shamisen-style riff, taiko, bass in the in scale (112 bpm) |

Where each track plays: `Resources/Audio/music.json` (the handbook's 音楽 chapter lists them; a pinned track plays everywhere).

## Music studio melodies and instruments (world folk update)

Instrument voices (đàn tranh, đàn bầu, sáo trúc, koto, shamisen, shakuhachi, balalaika, violin/erhu) are **synthesised
in code** by `Scripts/Music/InstrumentSynth.cs` (additive models with vibrato, bends, breath and tremolo) — no recorded
samples, no third-party audio.

Melodies added to `Resources/Music/songs.json` by `Tools/music/folk/import_world_folk.py`. Every melody is public
domain (anonymous traditional song, or composer dead long enough); note data is NihongoLife's own, released CC0.
Reference transcriptions consulted (not redistributed):

| Song id | Melody | Instruments | Reference |
|---|---|---|---|
| ly-cay-da | Lý cây đa — quan họ Bắc Ninh, anonymous | đàn tranh, sáo trúc | Ngô Càn Chiếu, "Vài bản nhạc ký âm ABC" (ngocanchieu.org) |
| ly-ngua-o | Lý ngựa ô — dân ca Nam Bộ, anonymous | đàn tranh, đàn bầu | same |
| ho-hui | Hò hụi — boatmen's chant, anonymous (collected by Dương Bích Hà) | đàn bầu, sáo trúc | same |
| sakura-sakura-full | Sakura Sakura — Japanese traditional | koto, shakuhachi | trillian.mit.edu/~jc/music/abc/Japan/Sakura_Am.abc |
| kojo-no-tsuki | Kōjō no Tsuki — Taki Rentarō (d. 1903), Yamada version (1917) | shakuhachi, koto | …/Japan/KojoNoTsuki_Dm.abc |
| soran-bushi | Sōran Bushi — Hokkaido folk | shamisen, shakuhachi | …/Japan/Sooranbushi_F.abc |
| takeda-no-komoriuta | Takeda no Komoriuta — Kyoto folk lullaby | shakuhachi, koto | …/Japan/TakedaNoKomoriuta.abc |
| kalinka | Kalinka — Ivan Larionov (1860, d. 1889) | balalaika | …/Russia/Kalinka.abc |
| korobeiniki | Korobeiniki — Russian folk | balalaika | NihongoLife's own transcription |
| ochi-chyornye | Ochi Chyornye — Florian Hermann (1843) | violin, balalaika | …/Russia/OchiChorniya.abc |
| metelitsa | Metelitsa — Russian folk dance | balalaika | …/Russia/Metelitsa.abc |
| hopak | Hopak — Ukrainian folk dance | violin, balalaika | …/Russia/Hopak.abc |
| arirang | Arirang — Korean folk | gayageum | …/Korea/Arirang_G.abc |
| mo-li-hua | Mò Lì Huā — Chinese folk | erhu, sáo trúc | …/China/MoLiHua_F.abc |

Excluded on purpose: songs still in copyright (e.g. Sơn Tùng M-TP, Katyusha — Blanter d. 1990, "Hana" by Kina Shoukichi).
