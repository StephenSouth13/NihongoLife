# Phú Quốc — CC0 assets (credits & licences)

All files under `Assets/ThirdParty/PhuQuocCC0/` are **CC0 1.0 (public domain)**. No attribution is required; we credit
the authors anyway. Downloaded 27 Oct 2026; each licence was read on the source page (and in the pack's `License.txt`).

| Folder | Asset | Author | Source page | Licence | Used for |
|---|---|---|---|---|---|
| `kenney_pirate-kit` | Pirate Kit 2.1 (FBX + textures) | Kenney | https://kenney.nl/assets/pirate-kit | CC0 (License.txt) | palms, rowing boats, docks, rocks/sand patches, watch tower (beaches, fishing villages, ports, prison) |
| `kenney_nature-kit` | Nature Kit (FBX) | Kenney | https://kenney.nl/assets/nature-kit | CC0 (License.txt) | palm trees, bushes (pepper farms), large rocks (Dinh Cậu), cliffs |
| `kenney_city-kit-commercial` | City Kit Commercial 2.1 (FBX + textures) | Kenney | https://kenney.nl/assets/city-kit-commercial | CC0 (License.txt) | shop-houses for Dương Đông / An Thới, Gánh Ngon building, parasols (night market), towers (Grand World) |
| `ambientcg_Ground054` | Ground 054 (1K JPG PBR) | ambientCG (Lennart Demes) | https://ambientcg.com/view?id=Ground054 | CC0 | beach sand material `PQ_Sand` |
| `polyhaven_sky` | Kloofendal 48d Partly Cloudy (Pure Sky), 1K HDR | Poly Haven (Greg Zaal) | https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky | CC0 | optional tropical day skybox |

Already in the project and matching the same low-poly style (not re-downloaded): Kenney City Kit Roads, City Kit
Suburban, Food Kit, Furniture Kit, Interface/Impact Sounds; Quaternius Ultimate Food / House Interior / Animals packs;
KayKit City Builder Bits.

Other CC0 packs checked and suitable for later (URLs verified 27 Oct 2026, HTTP 200, CC0 stated on page):
- Kenney Holiday Kit — https://kenney.nl/assets/holiday-kit (beach chairs, umbrellas)
- Kenney City Kit Suburban — https://kenney.nl/assets/city-kit-suburban
- Quaternius Ultimate Nature — https://quaternius.com/packs/ultimatenature.html (palms, tropical plants)
- Poly Haven Aerial Beach 01 — https://polyhaven.com/a/aerial_beach_01 (sand texture)
- ambientCG Ground 080 — https://ambientcg.com/view?id=Ground080

Rule: only CC0 goes in this folder. Do not add CC-BY / "free for personal use" assets here.

## Added 27 Oct 2026 (second batch, all CC0 — License.txt in each folder)
| Folder | Asset | Source page | Licence | Use |
|---|---|---|---|---|
| `kenney_car-kit` | Car Kit (FBX) | https://kenney.nl/assets/car-kit | CC0 | cars, taxis, vans, trucks, buses-style vehicles |
| `kenney_holiday-kit` | Holiday Kit (FBX) | https://kenney.nl/assets/holiday-kit | CC0 | beach chairs, umbrellas, resort props |
| `kenney_mini-characters` | Mini Characters (FBX) | https://kenney.nl/assets/mini-characters | CC0 | guests / townspeople (low-poly) |
| `kenney_watercraft-kit` | Watercraft Pack (FBX) | https://kenney.nl/assets/watercraft-kit | CC0 | boats, jet-skis, buoys, piers |
Not found as CC0 in Kenney/Quaternius quickly: dedicated motorbike model (TODO).

## Added 31 Oct 2026 — Pixor library (CC0 1.0, "no credit needed" on every page) → `pixor_cc0/`
Downloaded as GLB from https://pixor.art/library/ and converted to OBJ + MTL with trimesh (Unity has no glTF importer
in this project). Materials are rebuilt as URP Lit from the MTL diffuse colours by `PhuQuocLifeBuilder`. A copy of this
table is in `pixor_cc0/CREDITS.md`.

| File | Source page | Use in 65_PhuQuocIsland |
|---|---|---|
| `scooter/pq_scooter.obj` | https://pixor.art/library/vehicles/scooter/ | motorbike traffic (most common vehicle) |
| `motorbike/pq_motorbike.obj` | https://pixor.art/library/vehicles/motorbike/ | motorbike traffic |
| `bicycle/pq_bicycle.obj` | https://pixor.art/library/vehicles/bicycle/ | kerb-lane bicycles |
| `bus/pq_bus.obj` | https://pixor.art/library/vehicles/bus/ | the island bus on Trần Hưng Đạo |
| `seashell/pq_seashell.obj` | https://pixor.art/library/pirate/seashell/ | shells on the beach |
| `starfish/pq_starfish.obj` | https://pixor.art/library/pirate/starfish/ | starfish at the water line |
| `pebbles/pq_pebbles.obj` | https://pixor.art/library/rocks/pebbles/ | pebble clusters |
| `rock-small/pq_rock-small.obj` | https://pixor.art/library/rocks/rock-small/ | small beach rocks |
| `crab/pq_crab.obj` | https://pixor.art/library/creatures/crab/ | beach crabs |

Also used by the life pass (already in the project, CC0): Kenney Car Kit (cars, taxi, van, SUV, delivery, truck),
Kenney Mini Characters (motorbike riders), Kenney Holiday Kit (lanterns, bench), Kenney City Kit Commercial / Suburban
(hotels, guesthouses, shops), Kenney Nature Kit (palms, rocks), ambientCG Ground054 (golden sand), Poly Haven
Kloofendal sky (`PQ_Skybox`). The sea is our own shader (`Shaders/NL_SeaWater.shader`).
