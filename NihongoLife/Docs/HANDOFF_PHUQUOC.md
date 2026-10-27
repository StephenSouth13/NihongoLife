# Handoff — Phú Quốc / Gánh Ngon (branch feat/phuquoc-restaurant)

## Done
- Scripts/PhuQuoc: GanhNgonCatalog, GanhNgonShift (+GanhNgonSpot), PhuQuocMap; Editor/PhuQuocIslandBuilder (menu NihongoLife/Build/Phú Quốc island (65)).
- Resources/PhuQuoc: ganh_ngon.json (9 dishes, 18 phrases, 6 vi/en/ja dialogues), phu_quoc_map.json (31 OSM places).
- careers.json: career `hospitality` (inserted before `medical`); progression.json: quest `job_ganhngon_shift`; CareerCatalogTests expects 5 careers.
- Tests/EditMode/PhuQuocRestaurantTests.cs (7 tests). Docs: PHU_QUOC.md, PHU_QUOC_ASSETS.md.
- Assets/ThirdParty/PhuQuocCC0 (Kenney nature/pirate/city-commercial, ambientCG Ground054, Poly Haven sky) — all CC0.

## TODO for Claude
- [ ] EditMode suite NOT verified: batchmode import of the fresh worktree hung on Sushi Restaurant Kit `.blend` files (no Blender). Open project in Editor, run all EditMode + PlayMode tests, fix failures.
- [ ] Run PhuQuocIslandBuilder once to generate Scenes/65_PhuQuocIsland.unity (+ materials) and commit it; check it is in Build Settings.
- [ ] Not merged into main yet (tests unverified). Merge after green; main is at f5450255.
- [ ] Builder has no test (EditMode asmdef doesn't reference Editor asm).
- [ ] Island is 1:100 scale (1:1 not feasible); coastline hand-traced (~1 km error); Hồ Quốc coordinate approximate.
- [ ] Hook GanhNgonSpot into career UI/schedule (workStart 10–16) and HUD like ClinicSpot; no NPC guest flow yet (copy ClinicPatientFlow).
- [ ] Skybox HDR + Ground054 material not wired to scene lighting; no screenshots taken.
- [ ] Gánh Ngon has no prices by design; real menu/prices from owner later.
- [ ] docs/readme/*.png show as modified in worktree (LFS artifact) — ignore, not committed.
