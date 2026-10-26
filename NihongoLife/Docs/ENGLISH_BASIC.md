# English Basic (Tiếng Anh cơ bản / 基礎英語)

Beginner (A1) English course inside the Study Hub (**U** / **Học thêm**). Based on Codex's English course draft
(`Scripts/English`, "English Foundations" in the Exam Center), but kept separate: English Basic is short, topic-based
practice for absolute beginners; English Foundations (CEFR A1–C1) and IELTS stay in the Exam Center (**K**).

## What the player gets

| # | Topic | id |
|---|---|---|
| 1 | Greetings & introductions | `eb_greetings` |
| 2 | Numbers & age | `eb_numbers` |
| 3 | Family | `eb_family` |
| 4 | Daily life | `eb_daily` |
| 5 | Food & ordering | `eb_food` |
| 6 | Shopping | `eb_shopping` |
| 7 | Directions | `eb_directions` |
| 8 | Time & days | `eb_time` |

Each lesson: 6 vocabulary cards (English + Vietnamese/Japanese meaning), 3 example sentences with translations, and a
4-question quiz (choose the meaning, fill in the blank, put the words in order) with instant feedback, a correct-answer
read-back, 0–3 stars and a saved best score (`learning_progress.json`, ids prefixed `eb_`).

UI text follows the game language (`Loc.T(vi, en, ja)`); meanings show Vietnamese in the Vietnamese/English UI and
Japanese in the Japanese UI. On WebGL, the speaker buttons read English aloud with the browser's speech synthesis
(`PatternSpeech.Speak(text, "en-US")`); they are hidden on other platforms.

## Code

- `Scripts/StudyModes/EnglishBasicCatalog.cs` — content + `EnglishQuizzes` (answer checks, seeded shuffle).
- `Scripts/UI/EnglishBasicUI.cs` — popup (lesson list, lesson page, quiz, summary).
- `Scripts/UI/StudyHubPopup.cs` — registers `StudyModeRegistry.EnglishBasic` when the hub opens.
- `Tests/EditMode/EnglishBasicTests.cs` — content integrity + UI run-through in vi/en/ja.

## Adding a lesson

Add an `L("eb_<topic>", "<icon>", …)` block in `EnglishBasicCatalog.Build()` with ≥5 vocab, ≥3 examples and ≥4 quiz
questions including one `Order(...)`. Icons must exist in `Resources/UI/Icons/ic_<name>.png`. For fill-in and word-order
questions the English hint must equal the full correct sentence (the tests enforce it). All content is pre-written and
original; no AI is used at runtime and no images are needed.
