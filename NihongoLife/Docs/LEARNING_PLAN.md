# Personal learning plan (Lộ trình học cá nhân)

Open it from **Study Hub → My learning plan**. Pick a language (Japanese / English / Russian / IELTS), your current level, a goal (★ = mastery: C2, N1, IELTS 9.0) and minutes per day (15/30/60/120). The game turns the content it already has (School, sentence patterns, Russian path, English Basic, 12 tenses, IELTS model answers, Exam Center, Dictionary, videos) into phases with weekly milestones and an estimated finish date. You log study time with the +15/+30/+60 buttons. After 7 days the ETA switches from the planned pace to your real pace, and the plan shows whether you are ahead of it or behind.

## Progress bars, checkpoints, certificates, badges

- **Animated progress**: the overall bar and every stage bar ease to their value (`AnimatedBar`) and show `% · hours done / planned`.
- **Milestone checkpoint test** (one per stage): 16 mixed questions (vocabulary, grammar, reading, listening) chosen round-robin by skill. They come from content the game already has: the School practice set for that level (`SchoolCatalog.Certificate`, e.g. `cert_jlpt_n5`, `cert_torfl_b1`, or the IELTS-style set for band ≥ 5.0) plus that level's unit exercises. The test runs in the existing `CourseQuizView`, which already has the timer (45 s per question), instant feedback, a summary and mistake review. The plan then adds a result screen with a per-skill breakdown. A score of **≥ 70 %** passes. Stage 1 is always open, and each later stage unlocks when the previous checkpoint is passed (`MilestoneCheckpoint`).
- **Certificate**: each pass creates a `MilestoneRecord` (player name, level, date, score, skills, serial `NL-XX-LEVEL-YYYYMMDD`). `CertificateView` draws it with the official logo from `Bao_Cao/Logo` (copied to `Resources/Branding/`, never redrawn). It says plainly: *"NihongoLife in-game certificate — not an official JLPT / TORFL / IELTS certificate."* **Save as PNG** renders it off-screen to `persistentDataPath/certificates/<serial>.png` (1600×1130).
- **Badge gallery**: shows every earned badge across tracks, plus the locked milestones still ahead in the current plan.

This reuses existing systems and does not duplicate them: School certificate practice sets (`CourseCertificatesView`) and `CourseQuizView`.

Code: `Scripts/StudyModes/Plan/LearningPlanner.cs` (math), `Scripts/UI/LearningPlanPopup.cs` (UI), `Tests/EditMode/LearningPlanTests.cs`. State is saved in `persistentDataPath/learning_plan.json`.

## All figures are ESTIMATES

The figures are published averages for guided learning. Real time depends on the learner, their first language, how intensive the study is and how much self-study they do. They are not official guidance from Cambridge, IELTS, JLPT or TORFL.

| Track | Cumulative hours used (midpoint) | Source |
|---|---|---|
| English CEFR | A1 95 · A2 190 · B1 375 · B2 550 · C1 750 · C2 1100 | Cambridge English, *Guided learning hours*: A2 ~180–200, B1 ~350–400, B2 ~500–600, C1 ~700–800, C2 ~1000–1200 — https://support.cambridgeenglish.org/hc/en-gb/articles/202838506-Guided-learning-hours ; Cambridge blog *How long does it take to learn a foreign language?* (2018) — https://www.cambridge.org/elt/blog/2018/10/11/how-long-learn-language/ |
| Japanese JLPT | N5 462.5 · N4 787.5 · N3 1325 · N2 2200 · N1 3900 | Japanese Language Education Center survey of study hours (2010–2015), learners *without* kanji background: N5 325–600, N4 575–1000, N3 950–1700, N2 1600–2800, N1 3000–4800 (summarised in the Wikibooks *JLPT Guide*). Vietnamese learners are counted as non-kanji learners. |
| Russian TORFL | CEFR × 1.3 | **Our assumption.** TORFL levels map to CEFR (ТЭУ A1 … ТРКИ-4 C2). Cyrillic script, cases and aspect make Russian slower than English for Vietnamese speakers. FSI's category figures are for English speakers and are not used directly. |
| IELTS | ≤4.0: 375·band/4; above: 375 + 200 per band | Rule of thumb of ~200 guided hours per band (Cambridge / British Council guidance). IELTS 4.0 is taken as ≈ B1 entry. |

Weeks = ceil(hours × 60 / minutesPerDay / 7). ETA after ≥7 days = remaining hours ÷ observed hours per day. The observed pace has a floor of 10% of the planned pace, so a stalled plan still shows a finite date.

IELTS, JLPT and TORFL/ТРКИ are trademarks of their owners. NihongoLife is not affiliated with them.