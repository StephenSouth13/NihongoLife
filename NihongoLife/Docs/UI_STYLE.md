# UI style, loading and transitions

## Shared frame and type scale (`Scripts/UI/UiFrame.cs`)
Every menu popup (`MenuPopupBase`) gets the same frame through `UiFrame.Apply`: a 1.5 px tinted hairline border
(gold for menus, teal available), a 1 px inner top highlight, and four small corner accents, on top of the existing
rounded card and soft shadow from `UiKit.Card`. Titles use `UiFrame.StyleTitle` (30 px bold, +1 letter-spacing).
Type scale (1920×1080 reference): Display 40 · Title 30 · Heading 22 · Body 15 · Small 13 · Caption 12.
The frame is plain uGUI images: no animation or `Update`, so it is safe in Edit Mode and in batch renders. All shapes
are drawn by code (rounded rects and lines): no generated or AI artwork.

## Fonts and icons: licenses
| Asset | Where | License |
|---|---|---|
| Noto Sans JP (TMP SDF) | `Assets/NihongoLife/Fonts/NotoSansJP.otf` | SIL Open Font License 1.1 |
| Liberation Sans (TMP default fallback) | TextMesh Pro package | SIL Open Font License 1.1 |
| Material Symbols icons (`UiIcons`) | Google Material Symbols | Apache License 2.0 |
| NihongoLife logo / mark | `Resources/Branding` (from `Bao_Cao/Logo`) | Project-owned |

No new third-party fonts or icons were added in this pass.

## Loading speed
* Every runtime scene change is asynchronous: `SceneFlowController` uses `LoadSceneAsync` with
  `allowSceneActivation = false` until 90 %, shows the loading screen (destination picture, progress bar with the
  player's portrait, "learn while you wait" phrase and tip) and only then activates the scene.
* The intro trailer and the non-WebGL fallback loader now also use `LoadSceneAsync`; the EditMode test
  `UiPolishTests.Runtime_NeverLoadsScenesSynchronously` fails if a synchronous `SceneManager.LoadScene` reappears
  outside Editor code.
* Loading tips are read lazily on the first trip (not in `Awake`).

## Scene transitions (`Scripts/Core/SceneTransition.cs`)
One set of timings for all scene changes: fade in 0.38 s, fade out 0.5 s, 0.18 s "Ready" hold, smoothstep easing.
While fading, the loading card's text block and word card wipe in from the left and out to the right (60 px).
Reduce Motion turns the fade into a cut and the wipe off. The progress bar never goes backwards.

## In-game video player
* Preparing: a spinner and a strip showing time used against the 20 s prepare timeout ("Loading video… 3s / 20s").
* Buffering: when playing but no new frame arrives for 0.75 s, the spinner shows "Buffering…"; after 20 s of stall
  the player stops with a Timeout error.
* Every error (offline, bad link, no source, prepare failure, timeout, decode) ends in the error card with **Retry**
  (and **Open on YouTube** when the video has a YouTube link).
