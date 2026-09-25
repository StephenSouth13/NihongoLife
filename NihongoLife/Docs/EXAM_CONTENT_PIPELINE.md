# Exam Content Pipeline

## Folder convention

Each exam owns one folder. Keep the ScriptableObject small and keep large media outside Git:

```text
Assets/NihongoLife/Resources/Exams/
  ielts_academic_practice_1.asset
  ielts_academic_practice_1/
    manifest.json
    audio/
      listening_hotel_01.mp3
      listening_hotel_02.mp3
    images/
    source/
```

`manifest.json` stores the remote media URL, duration and checksum. The Unity exam asset stores only
the passage/question metadata and a stable `mediaId`. Do not commit WAV files or large MP3 files to the
game repository.

## Recommended hosting

For the website/WebGL build, use object storage plus a CDN with public read URLs and long-lived cache
headers. Keep media in a separate bucket/provider from the Supabase database. Supabase should store only
the exam metadata, media id, URL and checksum; it should not proxy every MP3/MP4 byte. Cloudflare R2,
Backblaze B2 or S3 behind a CDN are suitable for a large library. This avoids turning Supabase into the
video server and avoids database/storage quota pressure.

Google Drive is acceptable as an editor/source archive only. If it must be used temporarily, publish the
file for link access and convert the share link through a small server-side proxy. Never put a Google
service credential in Unity or in a WebGL build.

## Size targets

- Listening speech: mono AAC/MP3, 64-96 kbps, 44.1 kHz.
- Video listening: H.264 MP4, 480p/720p, AAC audio; never upload the original camera master.
- Keep each clip below 8 MB and split long listening sections into passages.
- Use remote loading for website builds and cache downloaded clips outside `Assets`.
- Store a SHA-256 checksum in the manifest so corrupted downloads can be discarded and retried.

`mediaStartSeconds`, `mediaDurationSeconds` and `mediaAspectRatio` are metadata for the player. The
player should seek to the start, stop at `start + duration`, and letterbox/crop to the requested ratio.
The original media remains untouched, so one file can serve many questions.

## YouTube links

Use `youtubeUrl` only for a public educational video that you are allowed to embed. The website should
use the official YouTube embed/player API, with the start/end parameters and CSS `object-fit` for crop.
The Unity client should not scrape, download or transcode YouTube URLs. For exam audio that must work
offline and have a fixed playback count, upload an optimized MP3/MP4 to your own CDN instead.

## Large-library rule

Do not put a whole question bank or media library in the Unity build. Keep one lightweight seed exam in
`Resources/Exams`, then fetch additional manifests/pages from the content API. Download only the current
passage, cache it in `Application.persistentDataPath`, evict old files by size/last-used date, and keep
the checksum in the cache index. This keeps the WebGL build small and prevents a large media library
from consuming Supabase database quota.

## Publishing workflow

1. Create a folder named after the exam id.
2. Add the question/passage metadata to the `.asset` file.
3. Upload audio to the media bucket/CDN and record its URL and checksum in `manifest.json`.
4. Assign a stable media id to the Listening passage.
5. Test play count, pause/replay, offline failure, and WebGL CORS before publishing.
