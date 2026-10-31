# Russian path (alphabet → TORFL)

## What already existed (audit)
- School: `alphabets/ru_cyrillic.json` (Cyrillic lessons ru_L_*), `units/units_ru.json` (35 units, A1–B2) and `grammar_ru.json`.
- Certificates: `cert_torfl_a1 … b2` practice sets.
- Russian UI language: `ru_table.json` and `ru_table_extra.json`, with a coverage audit test.

## What was added
- `Resources/Content/Russian/ru_path.json` has 6 stages and 53 steps:
  - Alphabet (refs into the School alphabet).
  - **Pronunciation**: 4 new lessons covering stress and vowel reduction, hard/soft consonants, devoicing and assimilation, and intonation (ИК-1…4). Each has examples and a short quiz.
  - A1, A2, B1, B2: refs into the existing School units, then the certificate practice set, then a new mock test.
- `Resources/Content/Russian/tests/torfl_{a1,a2,b1,b2}_mock_1.json` are **original** mock tests in the TORFL format:
  - Each has 20 multiple-choice items (lexis/grammar, reading with a passage, listening with a script), with an answer key and explanations.
  - Each also has a writing task with a model text and checklist, and 2 speaking prompts with sample answers.
  - They carry a disclaimer that they are not official ТРКИ material. The 66 % pass line is a practice threshold only, not an official cut score.
- Code:
  - `Scripts/StudyModes/Russian/RussianPath.cs`: data and loader (`RussianCourse`).
  - `Scripts/UI/RussianPathPopup.cs`: stage tabs, step list, lessons with quiz, and the mock (answer, submit, key, model reveal). Unit and certificate steps open the School.
  - The Study hub has a new tile, "Russian path".

## Videos (@NihongoLifeGame)
- Every step has a video slot `{ "id": "ru-path-<step>", "youtubeUrl": "" }`. **All slots are empty**: no videos have been uploaded.
- When a video is published, paste its YouTube URL into the slot. `RussianCourse.RegisterVideos()` then merges the filled slots into the in-game video catalog under module `ru-path`, and the "Watch the lesson video" button turns on.
- `VideoCatalog.Category(v)` / `ByCategory("en"|"ja"|"ru")` sort videos into English / Japanese / Russian based on their module. These are the same three categories to use as playlists on the channel.
