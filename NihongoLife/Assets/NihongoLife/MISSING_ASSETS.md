# NihongoLife - Asset gaps

For the complete project overview, progress assessment, roadmap, import rules and verified source list, see [PROJECT_STATUS_AND_SOURCES.md](PROJECT_STATUS_AND_SOURCES.md).

This list only contains assets that are not already available in the project.
Do not import another generic furniture or food pack: the current Kenney kits already cover basic shelves, tables, lights, refrigerators and food props.

## Priority 1 - Character animation

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

The project already has: Idle, Walk, Talk, Bow and Point.

## Priority 1 - Convenience store

- Japanese convenience-store checkout/POS terminal
- Barcode scanner and receipt printer
- Commercial glass-door drink refrigerator
- Retail gondola shelf with end caps
- Shopping basket and basket stand
- Shelf price rails and editable price cards
- Packaged Japanese products: bento, cup noodles, snacks, drinks and toiletries
- Store decals/signage with reusable blank variants

The project already has generic refrigerators, cabinets, ceiling lights, food, cans, bottles, cartons and the rice-ball model.

## Priority 2 - Story locations in the same gameplay scene

- Small ramen shop exterior and interior set
- Japanese station entrance, ticket machine, gate and platform props
- Summer festival stalls, lantern strings, torii and portable shrine props

These locations must be added as zones inside `90_TestSandbox.unity`; no additional gameplay scene is required.

## Priority 2 - Story props

- Rigged cat with idle/walk animations
- Japanese garbage bags, recycling crates and collection signs
- Train ticket and IC card props
- Ramen bowl variants and restaurant menu board

## Audio

- Licensed town ambience and indoor convenience-store ambience
- Door chime, scanner beep, receipt, bag and shelf interaction SFX
- Recorded Japanese dialogue or approved TTS files for every story node

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
