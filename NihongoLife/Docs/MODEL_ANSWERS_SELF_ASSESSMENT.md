# IELTS model answers & self-assessment

Feature for players who have no AI or teacher grading: they mark their own Writing/Speaking against a rubric and compare it with model answers at every band.

## Content (pre-written, no runtime AI)
- `Resources/Content/Ielts/ModelAnswers/*.json` holds 7 sets, with `index.json` listing them:
  - Writing Task 1: 2 sets (line graph, table).
  - Writing Task 2: 2 sets.
  - Speaking: Part 1, Part 2 (cue card) and Part 3.
- Each set has samples at **bands 5, 6, 7, 8 and 9**, 35 samples in total. Every sample has:
  - a criterion band and note for each criterion (TA/TR, CC, LR, GRA, or FC, LR, GRA, P);
  - a summary;
  - highlighted phrases (good / issue) with notes;
  - "how to reach the next band".
- All samples are **original and illustrative**, written for NihongoLife. They are not real candidate scripts and not official scores, and the UI says so. The speaking P band is illustrative because pronunciation cannot be judged from text.
- `Resources/Content/Ielts/self_assessment_rubric.json` holds 4 criteria each for Task 1, Task 2 and Speaking. Each criterion has descriptors for bands 9–4, **paraphrased** (not quoted) from the public band descriptors, plus a 3-item checklist.
- Language coverage:
  - Titles, summaries, highlight notes and next-band tips are in vi/en/ja/ru.
  - Longer per-criterion notes and rubric descriptors are in vi/en; ja and ru fall back to en.
- Generators live outside the repo (Python). The JSON was validated as follows:
  - The sample band equals the result of the official rounding on its criteria: Writing task rounds down to the half band; Speaking uses the .25/.75 rule.
  - Every highlight phrase is a substring of its text.
  - Word counts: Task 1 ≥150 and Task 2 ≥250 for band ≥6.

## Code
- `Scripts/Exam/Ielts/IeltsModelAnswers.cs` contains the data classes and the loader.
- `Scripts/Exam/Ielts/IeltsSelfAssessment.cs` contains:
  - the rubric;
  - `IeltsSelfAssessment`, with source `"self-assessed"` and official rounding;
  - `IeltsSelfAssessmentService`, which saves into the attempt. It sets `selfAssessedOverall` and **never** changes the graded `overall`.
- `IeltsProductive.cs` gains these fields:
  - `IeltsWritingTaskRecord.selfAssessment`;
  - `IeltsProductiveRecord.speakingSelfAssessment` and `selfAssessedOverall`.
- UI, all in `Scripts/UI`, edit-mode safe with no DontDestroyOnLoad:
  - `ReferenceUi.cs`: the shared host canvas "ReferenceCanvas" (order 800), highlight markup and `HighlightTooltip` (hover or tap).
  - `ModelAnswersPopup.cs`: set list, prompt and data table, band tabs 5–9, **Compare** (two bands side by side), and highlight tooltips.
  - `SelfAssessPopup.cs`: Task 1/Task 2 tabs. Each criterion has a checklist, band buttons 4–9 and a live descriptor. The result line is labelled SELF-ASSESSED, and a Save button stores it.
- Hooks:
  - IELTS test results (`IeltsTestUI.Productive.cs`) gain the buttons **Tự đánh giá / Self-assess** and **Tham khảo bài mẫu / Model answers**, and show the self-assessed band once it is saved.
  - Model answers **open automatically once after submitting** Writing or Speaking. Tests switch this off with `IeltsTestUI.AutoOpenModelAnswers`.
  - The Study hub (`) has a new tile, "IELTS model answers".
- Tests: `Tests/EditMode/ModelAnswersAndRussianPathTests.cs`.
