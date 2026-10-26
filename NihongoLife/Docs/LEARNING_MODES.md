# Learning modes (Study hub)

## Features
- **Study hub** (`StudyHubPopup`): entry point for extra study modes. Open with the **U** key or the HUD **"Học thêm"** button.
- **Japanese sentence patterns N5–N3** (`JapanesePatternsUI`, data in `Scripts/StudyModes/JapanesePatternCatalog.cs`): pattern explanation, examples (ja / kana / romaji / vi / en) and drills (fill-in, choose, reorder). Progress is saved by `LearningProgressStore` (`learning_progress.json` in persistentDataPath). On WebGL, sentences are read aloud through `Plugins/WebGL/NihongoPatternSpeech.jslib` (browser speech synthesis).
- **In-game video player** (`InGameVideoPlayerUI`, `VideoCatalog`, `VideoPlaybackStateMachine`): plays lesson/gameplay videos inside the game; if a video has no playable source it shows a YouTube card (title, summary, link) instead.
- UI text supports Vietnamese, English and Japanese.

## Controls
- `U` / "Học thêm": open or close the study hub.
- Choose a mode in the hub; `Esc` or the close button returns to the game.

## Adding a sentence pattern
1. Edit `JapanesePatternCatalog.Build()` and add a `SentencePattern` with a level from `Levels` (`N5`, `N4`, `N3`).
2. Add examples (`PatternExample(ja, kana, romaji, vi, en)`) and drills with `PatternDrill.Fill / Choose / Order`. For `Fill`, the prompt must contain `__`.
3. Run the EditMode tests (`StudyModesTests`) – they check the catalog data.

## Adding a video
1. Add an entry to `Assets/NihongoLife/Resources/Video/nihongolife_videos.json` (`id`, `module`, `youtubeUrl`, titles/summaries in vi/en/ja, `license`, `durationSeconds`).
2. Set `playableUrl` to either a direct hosted mp4/HLS URL (CDN, R2, ...) or a path relative to StreamingAssets, e.g. `Videos/my-video.mp4`, and put the file in `Assets/StreamingAssets/Videos/`.
3. Leave `playableUrl` empty to show only the YouTube card.

**Note:** only `StreamingAssets/Videos/fishing-midori.mp4` is bundled in the repo. All other videos need a hosted mp4 URL in `playableUrl` (YouTube links cannot be played by Unity's VideoPlayer directly).

## Tests and screenshots
- EditMode tests: `StudyModesTests.cs`, `StudyModesUiSmokeTests.cs`.
- Screenshots: menu `NihongoLife/Debug/Capture study mode screenshots`, or batch `-executeMethod NihongoLife.EditorTools.StudyModesScreenshots.Capture` (output: `Screenshots/learning-modes/`).