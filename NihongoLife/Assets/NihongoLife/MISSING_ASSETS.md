# NihongoLife - Asset gaps

Story, career and online expansion design: [SOCIAL_STORY_ROADMAP.md](SOCIAL_STORY_ROADMAP.md).

> **Important project rule:** Do not add `[MenuItem]` setup/build commands and do not create another scene without explicit approval. Finished work must be integrated directly into the existing scene/prefab/assets and work immediately with Play.

For the complete project overview, progress assessment, roadmap, import rules and verified source list, see [PROJECT_STATUS_AND_SOURCES.md](PROJECT_STATUS_AND_SOURCES.md).

This list only contains assets that are not already available in the project.
Do not import another generic furniture or food pack: the current Kenney kits already cover basic shelves, tables, lights, refrigerators and food props.

## Installed and integrated (2026-09-20)

- Sushi Restaurant Kit fixtures: counters, cabinets, shelves, refrigerator, cold display, steamer, bell, sign and plants.
- Sushi/food shelf stock: onigiri, dango, gyoza, rolls, tamago, bottles, soda and soy sauce.
- Train Pack: a lightweight station landmark using one high-speed front, one wagon and one track section.
- Ultimate Animated Animals: Shiba Inu with a generated Generic Animator controller using Idle, Walk, Eating, Hit, Attack and Jump clips.
- Kenney interface/impact audio packs: 230 OGG files imported. A curated set is now wired through `GameAudioCatalog` for UI clicks, doors, pickup/drop, portals, NPC hits and surface-aware footsteps.
- Integration is baked into `90_TestSandbox.unity` under `ThirdParty_Integrated_World`.
- Third-party integration is stored directly in the gameplay scenes. No manual build, integration or validation menu is required.

## Priority 1 - Character animation

> Deferred by project owner: environment/scene production may continue now. Add and retarget these Humanoid clips in a later animation pass.

All new clips must be Humanoid-compatible and preferably in-place.

- Run / jog loop
- Jump takeoff, airborne loop and landing (the physical jump is implemented; these clips are still required for final visual quality)
- Left and right turn-in-place
- Pick up item from a shelf
- Carry a shopping basket or bag
- Use cash register / scan product
- Sit down and stand up
- Positive and negative reaction
- Conversation listening idle

The human characters currently have: Idle, Walk, Talk, Bow and Point. The animal clips do not replace the missing Humanoid clips above.

## Priority 1 - Convenience store

- Japanese convenience-store checkout/POS terminal
- Barcode scanner and receipt printer
- Shopping basket and basket stand
- Shelf price rails and editable price cards
- Packaged Japanese products: bento, cup noodles, snacks, drinks and toiletries
- Store decals/signage with editable blank variants

The integrated store now has counters, shelves, refrigerator/cold display, hot-food steamer, food, bottles and a real onigiri visual. POS terminal, barcode scanner, receipt printer, shopping basket, shelf labels and Japanese packaged non-food products are still missing.

## Priority 2 - Story locations and additive zones

- Small ramen shop exterior and interior set
- Japanese station ticket machine, gate and platform signage props
- Summer festival stalls, lantern strings, torii and portable shrine props

Current architecture keeps the walkable city in `90_TestSandbox.unity` and loads heavy interiors/districts additively. `20_StationDistrict.unity` and `30_SushiRestaurant.unity` are now built and enabled. Do not split every street or house into a separate scene.

## Priority 1 - Train interior visual replacement

The installed Train Pack contains exterior trains and tracks only. The playable ticket, gate, boarding, ride, window scenery, conversation and arrival flow is implemented in `20_StationDistrict`, but the carriage shell is functional in-project geometry rather than final art.

- Low-poly Japanese commuter train interior with openable doors
- Bench seats, hand straps, route display, luggage racks and priority-seat decals
- Modular window/wall pieces
- Seated commuters compatible with the existing Humanoid rigs

Do not import another exterior-only train pack. The replacement must include an inspectable interior and permit redistribution in a built game.

## Priority 2 - Story props

- Rigged cat with idle/walk animations
- Japanese garbage bags, recycling crates and collection signs
- Train ticket and IC card props
- Ramen bowl variants and restaurant menu board

## Audio

- Still missing: licensed town ambience and indoor convenience-store ambience
- Still missing or unassigned: dedicated scanner beep, receipt printer and bag handling SFX
- Recorded Japanese dialogue or approved TTS files for every story node

Runtime SFX is integrated directly through `Assets/NihongoLife/Resources/Audio/GameAudioCatalog.asset`; no build menu or setup step is required. To replace a sound later, assign the new clip directly to this catalog in the Inspector. The full 230-file library is intentionally not loaded at runtime; only referenced clips are included in the build.

Do not add generated placeholder tones. Missing audio should remain silent and be reported by validation.

## Recommended free sources (verified 2026-09-20)

Download only the packs needed for the next milestone. Keep source files outside `Assets` and import selected FBX/PNG files, not entire demo projects.

### Best visual match

- **Kenney Food Kit** (CC0, 200 models): https://kenney.nl/assets/food-kit
  - Use for shelf products, actual food pickups and inventory thumbnails.
  - Import FBX/GLB meshes at scale factor 1, generate colliders only for pickup objects.
- **Quaternius Sushi Restaurant Kit** (free): https://quaternius.com/
  - Use selected counter, refrigerator, kitchen, plate and sushi props for the convenience store and later sushi location.
  - Do not import its demo scene or gameplay scripts.
- **Quaternius Ultimate Food Pack** (free): https://quaternius.com/
  - Secondary source for packaged/consumable food when Kenney has no matching object.
- **Mixamo** (free with Adobe account): https://www.mixamo.com/
  - Download animation-only FBX, `Without Skin`, 30 FPS, in-place where available.
  - Needed clips: running, pickup, cashier scan, listening idle, hit reaction, farming and basic combat.
- **Quaternius Ultimate Crops Pack** (CC0, 100+ meshes in five growth stages): https://quaternius.com/packs/ultimatecrops.html
  - Preferred future farming pack because growth stages are already separate and inexpensive to render.
- **Quaternius Ultimate Animated Animal Pack / Farm Animal Pack** (free): https://quaternius.com/
  - Preferred future pets/farm animals. Import only used animals and animation clips.
- **Quaternius Ultimate Monsters / Cute Animated Monsters** (free): https://quaternius.com/
  - Preferred future monster farming and spirit-like creatures; the stylized proportions match the current low-poly world.
- **Lowpoly Environment Extreme Pack** (free, URP compatible): https://assetstore.unity.com/packages/3d/environments/lowpoly-environment-extreme-pack-238098
  - Optional source for later forest/fantasy zones inside the same gameplay scene.

### Avoid for the current build

- Do not import multi-gigabyte Japanese city packs for one shop. They increase repository size, shader variants and WebGL download time.
- Do not mix realistic/PBR supermarket interiors with the current low-poly town.
- Do not import another character skeleton. Retarget animation clips to the existing Humanoid avatars.
- Do not keep 2K/4K textures for small props. Use 512 px, or 1024 px only for shared atlases/signage.

## Import budget for WebGL/mobile

- Static shop props: mesh compression Medium, Read/Write disabled, no per-object shadows for shelf products.
- Characters: one skinned renderer where possible, maximum four bone weights, Animator culling enabled off-screen.
- Textures: WebGL ASTC/ETC2 where available; 512 px props, 1024 px characters, mipmaps on world textures.
- Audio: mono Vorbis for dialogue, streaming for long ambience, decompress-on-load only for short UI/SFX clips.
- Reuse shared materials and atlases. A shelf row should not create one material per product.
- Add LOD only to objects visible from the street; small indoor props should use distance culling instead.
