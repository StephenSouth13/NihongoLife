# Zone transition regression ? 2026-10-06

Unity 6000.3.12f1 Play Mode, graphics enabled, 1600 ? 900.

Passed: CityStationBedroomRoundTrip. The test loads the saved scenes and uses the bedroom portal and shared map button.

Verified:
- Loading the station additively leaves a city player in the city until the transition starts.
- The station becomes the active scene, its entrance receives the player, and city renderers/colliders are disabled together.
- The CharacterController cannot cross the station west boundary.
- Returning restores the original city root states and removes the station HUD.
- The bedroom portal loads the saved room, its spawn has floor collision, and returning restores the city.
- The station and bedroom contain no missing MonoBehaviour components.
- Both map tabs open in city, station and bedroom. PNG captures were visually inspected. The map camera excludes the saved MapRoof layer.

90_TestSandbox remains loaded as the gameplay host during additive visits; its environment is hidden. Keeping the host preserves the shared player, camera, UI and services.

Scope limits: This validates the reported transitions, map display and selected collision checks. It does not certify every gameplay system, the full ticket/ride sequence, online voice, or NPC art/animation. Some station NPC visuals still use placeholder materials/poses.
