# Progression and Exam Integrity

## Three different scores

- **Knowledge** is the learning currency. It is earned from completed learning scenarios and validated
  practice. Knowledge unlocks lessons, zones and features through `requiredKnowledge`.
- **XP** is the level-progress number. It is used for the learner level and the progress bar; it should
  not be presented as an official IELTS/JLPT result.
- **Exam score** is an attempt result. IELTS reports an estimated practice band; JLPT reports the mock
  score and pass rule. An exam result never directly grants enough Knowledge to skip the curriculum.

## Exam rules

The exam runner already enforces section timers and locks a section when its timer expires. Production
rules should additionally be server-authoritative:

1. Create an attempt on the server with `started_at`, exam version, section order and expiry time.
2. Save answers/events incrementally; never trust a client-submitted final score.
3. Reject answers after the section expiry and reject duplicate submissions with an idempotency key.
4. Rate-limit attempts by account and exam version. Practice mode may allow unlimited attempts; ranked
   attempts should have a daily/weekly limit and a cooldown.
5. Keep a reviewable audit trail: attempt id, answer changes, tab/focus events, reconnects and timeout.
6. Mark suspicious attempts for review rather than deleting progress automatically.

Offline play can provide a local practice result, but it must be labelled **unverified practice** and must
not enter the competitive leaderboard or unlock gated content until a server validates it.

## Anti-cheat boundary

The client can disable copy/paste, pause, inspect-key shortcuts and repeated tab switching during a
ranked attempt, but it cannot prove that a user did not use another device or external tool. Real ranked
integrity requires server validation and, for high-stakes assessments, a separate proctoring service with
explicit consent and privacy documentation.

## Duolingo-inspired progression, adapted for NihongoLife

- Daily learning streak and review reminders.
- Skill mastery by vocabulary, grammar, listening, reading, writing and speaking.
- Linear N5/N4 lesson paths with optional placement tests.
- Knowledge gates for story zones and new interaction features.
- Private/friends leaderboard by weekly XP, with an all-time learning profile.
- Do not rank raw AI-estimated IELTS speaking/writing scores as official results.

## Privacy and retention

Audio recordings, transcripts and integrity events require explicit consent, a retention period and a
delete/export path. Store only what the learning feature needs. Do not put API keys, service-role keys or
third-party credentials in Unity or a WebGL build.
