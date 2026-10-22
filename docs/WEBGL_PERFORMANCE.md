# WebGL performance and online voice

Unity is not available in the environment that prepared this change. Sizes below are estimates from the last owner build plus what the player now excludes. Confirm them with one local WebGL build.

## What was wrong

The last local WebGL build (`Build/Release/WebGL`) shipped a 113 MB Brotli `.data.unityweb` and a 9.6 MB wasm file. Every gameplay scene was inside that first download. `index.html` left `config.devicePixelRatio` commented out, so HiDPI screens rendered at full device pixels. Quality levels Mobile and PC both kept shadows (`shadowDistance` 40).

Online did not work in the browser:

- Agora voice used the native `Agora-RTC-Plugin` (`agora_rtc_sdk.dll` and the native video bridge). That plugin does not run in WebGL. `VoiceChatService` created the engine only when a call started, which is already lazy, but `RtcEngine.CreateAgoraRtcEngine()` had no browser implementation.
- Supabase Realtime already spoke the Phoenix WebSocket protocol. On WebGL it now uses the browser `WebSocket` in `NihongoRealtime.jslib` (`NihongoLifeWeb.jslib`). Native builds still use `System.Net.WebSockets`. `ServicePointManager` and the background receive thread are not used on WebGL.

## Build

Open `NihongoLife/` in Unity 6000.3.12f1 and run the existing batch entry `NihongoLife.Editor.ReleaseBuild.BuildWebGL` (same class as the Windows and Android release builds). There is no new menu item.

That build:

1. Writes one compressed AssetBundle per area into `NihongoLife/WebGLZoneBundles/` (gitignored staging).
2. Builds the player with only `00_Bootstrap` and `01_MainMenu`.
3. Copies the bundles to `zones/` next to `index.html` and writes `zones/index.json`.
4. Writes `_headers` and `.htaccess` into the player folder.

Area scenes streamed from `zones/`:

| Scene | Bundle |
| --- | --- |
| `90_TestSandbox` | `nlzone-city` |
| `20_StationDistrict` | `nlzone-station` |
| `30_SushiRestaurant` | `nlzone-sushi` |
| `40_ HIBARICLASS` | `nlzone-school` |
| `45_HomeBedroom` | `nlzone-home` |
| `50_GameCenter` | `nlzone-arcade` |
| `60_MidoriIsland` | `nlzone-island` |

`SceneFlowController` waits on `SceneContentLoader.EnsureAvailable` and shows download progress before `LoadSceneAsync`. Editor, Windows, and Android still load scenes from the player.

## Expected size and runtime

Initial download should be the menu, bootstrap, engine, and shared UI, not the town, school, island, and restaurant meshes. The 113 MB data file was dominated by those scenes. Each area is downloaded the first time the player enters it. A return visit uses Unity's AssetBundle cache when `zones/index.json` includes the bundle hash.

Texture metas now have an explicit WebGL override: max 1024, crunched for 3D albedo, uncompressed-or-sharp for UI, sprites, normals, and lightmaps. Audio metas use Vorbis on WebGL, streaming for music and ambience, and 22050 Hz mono for short effects. Reimport on a machine with Unity to bake the override into the library; the `.meta` files already carry the settings.

Runtime:

- The WebGL template sets `config.devicePixelRatio` to `min(devicePixelRatio, cap)`. The cap defaults to 1.5 and is stored in `localStorage` key `nihongo.webgl.dprCap`.
- Quality level `WebGL` (shadow distance 0, shorter terrain distances) is selected on boot. It uses `Assets/Settings/WebGL_RPAsset.asset`: main-light shadows off, two additional lights per object, MSAA off (`m_MSAA: 1`).
- Settings has a web graphics toggle: Smooth (DPR 1, render scale 0.75), Balanced (DPR 1.5, render scale 1), Sharp (DPR 2, render scale 1). The pixel cap applies on the next page load. Render scale applies immediately.
- The first area load warms `Resources/WebGL/WebGLShaderVariants` when that collection exists. It is not in the repo yet; add one after a WebGL build report if the first frame of a zone still hitches.

## Player settings and hosting

Release WebGL settings applied by `ReleaseBuild.BuildWebGL`:

- Brotli (`webGLCompressionFormat` 0) with `decompressionFallback` on.
- Data caching on. File names are hashes. Debug symbols off.
- Exception support stays at explicitly thrown exceptions. Voice, Agora, and Supabase use `try/catch`. `None` would skip those catches.
- Managed stripping High. Minimal stripping breaks `JsonUtility` reflection and the URP render-scale setter.
- Initial wasm memory 256 MB, max 2048 MB, geometric growth.

Do **not** send `Content-Encoding: br` while decompression fallback is enabled. The Unity loader inflates `.unityweb` itself. A `Content-Encoding` header makes the browser inflate the body first, and the loader then fails.

`_headers` (Netlify / Cloudflare Pages) and `.htaccess` (Apache) set:

- `application/wasm` for `.wasm`
- `application/javascript` for `.js`
- long cache for hashed `Build/*` and `zones/*`
- `Cache-Control: no-cache` for `index.html` and `zones/index.json`

Serve the whole player folder, including `zones/`, from the same origin as `index.html`. `SceneContentLoader` requests `zones/` beside the page URL.

## Online and Agora

Desktop and Android keep the native Agora SDK. WebGL compiles `AgoraWebShim` instead (`NihongoAgora.jslib`). The game still talks to `IRtcEngine` through `VoiceChatService`.

The browser SDK (`AgoraRTC_N-4.23.4.js`) is injected the first time a voice or classroom session starts, not at boot. Microphone permission is the browser `getUserMedia` prompt. `Application.HasUserAuthorization` is not used on WebGL. Missing App ID or a failed `agora-voice` token request surfaces the existing localized error in the voice UI. The engine is released on leave.

`EnsureEngine` sets `audioScenario` on `RtcEngineContext` and then calls the single-argument `SetAudioProfile`. The two-argument overload is obsolete in the native SDK.

Owner setup:

1. Agora console: the project App ID must be the one in local secrets (`EduMeetingManager.ConfiguredAppId`). Enable the App Certificate and deploy the `agora-voice` Supabase Edge Function so the certificate never ships in the client. Token server and client must use the same channel name and uid.
2. For two-browser tests, the Agora project must allow the page origin (App ID + token). A token built for a native uid will not join a second browser on a different uid.
3. Supabase URL, anon key, and auth stay in the existing LocalSecrets / `SupabaseClient` path. Do not commit `secrets.json`.

## Manual test checklist

Unity 6000.3.12f1, a hosted build (or `localhost` with the generated `_headers` behavior), two browsers.

1. Build WebGL. Confirm `Build/Release/WebGL/index.html`, `Build/*.wasm`, and `zones/index.json` exist. The first `.data.unityweb` should be far below 113 MB. Record the new size.
2. Open the build. The menu appears before any area bundle downloads. DevTools network shows no `nlzone-*` file yet.
3. Start the game. The loader reports progress, then one zone bundle downloads, then the town (or the story scene) appears. Walk into a wall and a doorway. Colliders and the door should match the mesh.
4. Travel to the station, school, sushi shop, bedroom, game center, and island once each. Each first entry downloads its bundle. The second entry should not re-download when caching is on.
5. Settings: cycle Smooth / Balanced / Sharp. Render scale changes immediately. Reload and confirm the canvas backing-store ratio follows the stored cap (1, 1.5, or 2).
6. Two browsers, two accounts. Both reach the co-op lobby. Presence updates when one joins and leaves. Pull the network cable or close one tab and confirm the other client drops that presence without hanging.
7. Both join the same voice channel. Each browser shows a microphone prompt. Speech is audible both ways. Leave and rejoin. A missing App ID or a 404 from `agora-voice` shows the localized error and leaves the rest of the game playable.
8. Console: the warnings fixed here should be gone (`SetAudioProfile` obsolete, CS0414 on the station and menu fields, `SetManagedStrippingLevel(BuildTargetGroup)`). Agora and Devion package warnings can remain.

## Main menu

Unity was not run for this pass. Play `01_MainMenu` locally. Once, run `NihongoLife.EditorTools.MainMenuBaker.Bake` so the Editor scene stores the same top bar Play builds. There is no new scene and no menu item.

Start opens a mode card, then character select, then the game.

- **Offline** uses the existing guest save (`guest_progress.json` via `PlayerSessionService.BeginGuest`) and the existing unlock bypass (`LocationUnlocks.Bypass` and `StoryDirector.Bypass`). Places, story quests, jobs, and the company knowledge check are open. The prologue is not forced. Online progression is unchanged: the co-op lobby and zone loads call `BeginProgressionPlay`, which turns those bypasses off and uses the account save when signed in.
- The sidebar **Play online** button still sends a guest to the account panel and a registered account to character select, then the lobby.
- If Supabase is not configured, the mode card says so and Offline still starts.
- Settings scrolls the key list inside the card. The language row (VI / EN / JP), About, and Sign in share one top bar. Reduce motion is in Settings (`nl.reduceMotion`). It snaps menu entrance, popup scale, button hover scale, title shimmer, sakura, the town camera orbit, and the scene fade.
- Effects stay on the CPU and unlit meshes: no extra post-processing. WebGL uses the WebGL quality level, a smaller character preview, two preview lights, and ten petals.

Check at 16:9, 16:10, 4:3, and ultrawide:

1. VI / EN / JP, About, and Sign in stay inside the screen. The learner line stays in the left column.
2. Settings: text stays inside the card, the X is in the header, the key list scrolls, Reset and Close stay visible. Toggle reduce motion and confirm the motion above stops.
3. Character select: drag a full turn with mouse and touch, the turn buttons step, idle spin runs when you are not dragging, and the model fills the frame. Esc closes character select, then the mode card.
4. Offline enters the town with sushi, the station, the arcade, and the island open, on the guest save, with no forced prologue. Online without secrets stays on the mode card. Online while signed in opens the co-op lobby with story gates on.

## Deleted scratch files

GUID scan of `NihongoLife/Assets` (text references only; `Resources/`, `StreamingAssets`, plugins, and editor folders excluded from the delete set). Audio was not deleted: dialogue, JLPT/IELTS, and NPC lines are loaded by name from data tables.

| Category | Files | Size |
| --- | ---: | ---: |
| `.mtl.bak` material backups plus their `.meta` | 86 (43 pairs) | 11.5 KB |

These were unreferenced Obj backups under the Midori Island survival kit. `.gitignore` now ignores `.mtl.bak`, `.claude/`, `.codex/`, and `.utmp`. No `.claude`, `.codex`, or `.utmp` paths were tracked.

## Probably unused, kept for an owner check

A GUID that never appears in a scene or prefab is not proof the file is junk. Scenes are loaded by name, native plugins are loaded by folder, and FBX files can point at textures from inside the binary. Nothing in this list was deleted.

- Third-party authoring packs with no GUID hit in YAML, about 1 GB combined: the dated Sushi Restaurant zip, Styloo classroom GLTF/FBX, Ultimate Animated Animals (including `Preview.mp4`, 17.6 MB), Ultimate House, Ultimate Food, Mixamo farming FBX, Kenney, KayKit, and `.blend` sources (316 MB). Confirm a pack is unused in a real build report before removing it.
- Agora native binaries (`agora_rtc_sdk.dll`, ffmpeg, lip-sync). Desktop and Android still need them. WebGL does not pack them when the plugin importer excludes that platform.
- `NihongoLife/Generated` previews (about 15 MB) and TextMesh Pro examples.
- Every `.mp3` / `.ogg` / `.wav`, everything under `Resources/` and `StreamingAssets/`, and editor-only art.
