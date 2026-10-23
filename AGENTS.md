# NihongoLife repository rules

Read this before changing anything. These rules come from bugs that already happened; each one has a test that
guards it. Keep code that works — fix what is wrong, do not rewrite a working system to your own taste.

## Scenes, assets, builders

- Do not add Unity editor commands with `[MenuItem]`. Build, setup, asset integration and scene repair must not require manual menu actions. Builders run headless: `Unity -batchmode -executeMethod <Class.Method>`.
- Do not create another Unity scene unless the user explicitly approves that exact scene. Prefer improving the existing gameplay scene and reusable prefabs.
- Persist finished content directly in scenes, prefabs and assets. Opening the project and pressing Play must use the finished state without a setup step.
- Never stack a replacement environment on top of an existing one. Remove or disable the superseded hierarchy, then validate renderers, colliders, doors, spawn points and navigation together.
- Do not claim a scene is complete without Play Mode visual and collision testing.
- Generated content is rebuilt by its builder, never hand-edited in the scene: streets `CityStreetBuilder.Build` (90_TestSandbox; `NL_STREET_SCENE=01_MainMenu` for the menu's copy of the town), town entrances `CityTownBuilder`, Game Center `GameCenterBuilder`, UFO catcher art `Tools/art/ufo_catcher_art.py`, story quests `Tools/story/*.py`.

## Product rules (decided by the owner — do not change without asking)

- **Guest / offline = the whole town open.** A guest (no account signed in, `PlayerSessionService.Mode == Guest`) has every place unlocked and is not held in the opening story. This is derived from the session (`PlayerSessionService.OpenWorld`), never from which menu button was pressed — starting the town directly must behave the same. Do not set `LocationUnlocks.Bypass` / `StoryDirector.Bypass` in game code; those flags are for tests only.
- **Account = campaign + online.** A signed-in registered account plays the roadmap (places open by 「trí tuệ」 knowledge + story quests, `LocationUnlocks`) and may go online. Online features (online world, friends, chat, co-op, voice, Speaking room, island visits, ranking submissions) require `OnlineAccess.IsRegistered` (signed in with an email). Guests are invited to register (`AuthUI.ShowForOnline`).
- Guest progress is saved on the device (`guest_progress.json`) and moves into a **new** account on its first sign-in; an existing account keeps its own save.
- **Each account has its own local save** (`progress_<userId>.json`, `PlayerSessionService.AccountId`). On a shared machine one account must never start from — or merge into the cloud — another account's save. Repositories reload when the session's account changes.
- The server enforces "registered only" too: Edge Functions reject anonymous users (`user.is_anonymous`), private voice calls are issued only to their two players (`agora-voice` checks `peer`), and the database uses `public.is_registered()` for online writes. Never rely on the client alone.
- The Vietnamese UI word for knowledge is **trí tuệ** (not "kiến thức").
- The game has exactly three founders: Quách Thành Long, Nguyễn Lê Gia Huy, Lương Nguyễn Tấn Dũng (`Resources/Menu/menu_about.asset`). Do not add other names to the founder credits.
- Priorities: online communication and the IELTS / JLPT exams, on the website (WebGL) as well as desktop.

## Invariants (each one broke once)

- **Never move a component's own transform for a side effect.** An `AudioSource` / FX that must follow something lives on its own child GameObject. (StreetTraffic once put its horn on the Traffic root and moved it to the honking car — every car jumped onto pavements and roofs.)
- **Unity fake null**: never write `GetComponent<T>() ?? …` (or `?.` on a Unity object you just looked up) — in the Editor a missing component is a fake-null object, so `??` never runs and the next access throws `MissingComponentException`. Write `GetComponent<T>().OrNull() ?? …` (`Scripts/Core/UnityNull.cs`) or an explicit `== null` check.
- **Modal entries** (`UiModalStack.Register`): the `isOpen` callback must mean "visible on screen now" (`activeInHierarchy`), not "the object exists". A hidden-but-alive card that still reports open blocks Esc, the map-unlock card and every "no overlay" check.
- **Input**: per-frame code must not clear shared input it does not own. The touch overlay releases its joystick/buttons once when it hides, never every frame while hidden (that wiped the mobile move and buttons for everything else).
- **Town edge** (`World/TownEdge.cs`): only the city scene has an edge; zones (island, station, shops) must never be treated as the town, and F near the edge only travels while the city is the active scene — otherwise F at counters / fishing spots / NPCs travels to the station.
- **Expressions** (`Player/KidExpression.cs`): mood gestures (bow / point) only play while the character is free — not sleeping, fainted, sitting, working or in dialogue.
- **Traffic** (`World/StreetTraffic.cs`): the Traffic root never moves; cars move only along local X in their lane (eastbound z −8.4, westbound z −11.6); kinematic cars must never push the player's CharacterController — `SeparateFromPlayer` sets the player beside the car on the kerb side; the bumper (not the pivot) stops behind the stop line; cars brake with `Braking()`. Tests: `TrafficLanePlayModeTests`, `StreetTrafficPlayModeTests`.
- **Streets** (`Editor/CityStreetBuilder*.cs`): zebra bars run along the road; stop lines 2 m before the zebra; walls are clipped against every building and reserved area (`c.solids`) and a lot that still overlaps a reserved area (konbini, plaza, station) is dropped. Test: `StreetTrafficPlayModeTests` (no yard wall inside the konbini).
- **Lighting**: `PC_RPAsset` shadow depth bias 1 / normal bias 1 (0.1 caused shadow acne — the ground flashed dark); thin ground layers and `GameplayGround` do not cast shadows. Test: `LightingFlickerPlayModeTests`.
- **Camera** (`ThirdPersonCameraController`): orbit angles apply at once, only the pivot is smoothed, distance pulls in at once and eases out; overhead canopies / sign boards have colliders so the camera never passes through them.
- **Per-frame cost**: no `FindObjectsByType` / `GetComponent(s)InChildren` / allocations every frame in `Update` / `LateUpdate` — scan on a timer (0.2–1 s) or on change; use `SceneRefs.Find` for cached singletons. The web and mobile builds feel every one of them.
- **WebGL**: with the build target set to WebGL the Editor compiles the web path (`#if UNITY_WEBGL`), so keep `Agora.Rtc` shim code fully qualified (`NihongoLife.Learning.…`) and compile once with the WebGL target before pushing web changes. Excluded from WebGL: `AgoraRtcSDK.asmdef` (excludePlatforms); the web path is `AgoraWebShim` / `VideoSurface` / `NihongoAgora.jslib`; `UnityEngine.Microphone` does not exist on WebGL (`Exam/WebMicrophone.cs`); no threads or `ClientWebSocket` on WebGL (`NihongoRealtime.jslib`); saves flush IndexedDB (`NihongoLifeWeb.jslib`). Check the web compile with the build target set to WebGL (`Unity -batchmode -quit -buildTarget WebGL -projectPath …`, zero `error CS`).
- Never write secrets (Supabase service keys, DB passwords, API keys) into files or commits. Cambridge / JLPT source materials stay local-only.

## Testing

- Unity can open the project once: batch runs fail while the Editor is open. Close it, then: `Unity -batchmode -projectPath <project> -runTests -testPlatform PlayMode|EditMode -testFilter "A|B" -testResults <xml> -logFile <log>`. Put logs on drive D (C: is full).
- `PlayModeSaveGuard` protects the real save and sets test defaults: `LocationUnlocks.Bypass = true`, `StoryDirector.Bypass = true`, `PlayerSessionService.PersistGuest = false`, `PlayerSessionService.GuestFullAccess = false`. A test that checks gates turns the bypass off and restores it in `finally` / `TearDown`.
- Known external / flaky: `IeltsProductivePlayModeTests` real-AI cases (network), `WeatherCameraPlayerPlayModeTests` rain recovery, occasionally `SpeakingLive` / `MidoriIsland` (re-run alone).
- Prove visual work with captures under `Bao_Cao/<topic>/` and look at them before reporting.

## Teamwork

- Before changing a system, read `NihongoLife/Docs/TEAM_TASKS.md` and the system's tests. After a change, add a short dated entry to `TEAM_TASKS.md` (what, why, files, tests).
- Keep commits focused, with the test that proves them; run the related tests and the full suite before pushing to `main`.
