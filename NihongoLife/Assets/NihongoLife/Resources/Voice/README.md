# Dialogue voice drop-in

Put imported MP3 or WAV files in one of these folders:

- `ja/` for Japanese voice
- `vi/` for Vietnamese voice
- `en/` for English voice

The file name must exactly match the scenario node ID. Do not include the extension in code.

Example:

`Resources/Voice/ja/node_neighbor_greeting.mp3`

When `node_neighbor_greeting` starts in Japanese mode, the clip is loaded and played automatically. A clip placed directly in `Resources/Voice/` is used as a language-independent fallback.

Recommended Unity import settings for dialogue:

- Force To Mono: enabled
- Load Type: Compressed In Memory
- Compression Format: Vorbis
- Quality: 70-80
- Preload Audio Data: enabled for short lines
- Normalize: enabled when recordings have uneven loudness

Keep dialogue peaks around -3 dB and remove long silence at the beginning and end.
