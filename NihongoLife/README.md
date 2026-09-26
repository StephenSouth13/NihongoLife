# NihongoLife

NihongoLife is an open-source Japanese-learning life simulation prototype. It combines story scenarios,
real-world practice, structured JLPT/IELTS-style exercises and a social town.

## Repository status

This is an active educational prototype, not an official JLPT or IELTS exam platform. Scores are practice
estimates and are not affiliated with the Japan Foundation, British Council, IDP or Cambridge.

The project currently includes:

- data-driven scenarios, quests, knowledge rewards and unlock requirements;
- local/cloud progress persistence and a Supabase-backed leaderboard integration;
- timed JLPT/IELTS-style practice flows with review screens;
- remote exam-media metadata so large audio/video files do not need to ship in the Unity build.

See `Docs/PROGRESSION_AND_EXAM_INTEGRITY.md` and `Docs/EXAM_CONTENT_PIPELINE.md` before adding content.

## Building and testing

Open the project in the Unity version recorded in `ProjectSettings/ProjectVersion.txt`. Wait for import,
resolve the local Unity license, then test scenes in Play Mode. Do not rebuild authored gameplay scenes
with legacy editor builders unless the task explicitly calls for it.

## Contributions

Keep content data-driven, preserve existing serialized field names, add focused tests for shared systems,
and document any external service or asset requirement. Never commit credentials, private media links or
service-role keys.

## Licensing

Original source code in this repository is offered under the MIT License in `LICENSE`. Third-party assets,
fonts, sounds and packages remain under their own licenses; see the relevant asset folder and license
files. The MIT grant does not relicense third-party content.

## Recent Architectural Updates
- **Direct Gameplay Boot:** The build pipeline and runtime bootstrap have been streamlined to bypass `01_MainMenu` and load directly into the sandbox/gameplay loop (`90_TestSandbox`), significantly speeding up iteration.
- **Native 3D Video Call (Agora):** Full support for in-game video calling has been implemented in `EduMeetingManager.cs`. 
  - To enable, install the *Agora Video SDK for Unity* and add the `AGORA_SDK_INSTALLED` scripting define symbol.
  - Video feeds are mapped to 3D TV screens in the metaverse environment. API keys are Base64 obfuscated to prevent accidental leakage in the client codebase.
  - Integration specs for the backend token generation are documented in `Docs/WEB_INTEGRATION_API.md`.
