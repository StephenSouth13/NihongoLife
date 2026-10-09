# Stage C0 quality audit

Re-inspected 438 actual PNGs against 599 catalog entries and 556 jobs.

No Unity session, source edit, image regeneration or owner approval was performed.

| Status | Count | Meaning |
| --- | --- | --- |
| Accepted candidate | 421 | Passed automated PNG and manifest checks; visual/material/semantic approval pending |
| Needs visual review | 17 | PNG exists, with measured or visually identified review flags |
| Failed | 118 | Render failed, or an existing image failed technical checks |
| Not generated | 43 | Not queued; no substitute icon supplied |

## Findings

- 0 existing PNGs fail source mapping checks; 0 orphan PNG files.
- 33 groups have identical decoded pixels. Format alternatives often render identically; these are not independent shop items.
- 116 failed assets have a same-pack/basename alternative with a PNG. Equivalence still needs review.
- The two failed camera-controller prefabs are Unclassified, based on serialized Camera components and absent mesh renderer blocks.
- Crop pack Harvested meshes can show a plant/tree after harvesting; the suffix does not establish a harvested produce item.
- All category labels remain provisional metadata mappings. No new filename guesser or imagined fashion/seed icon was added.

## Current import metadata

- 438 icons: Crunch requested; inspect effective format (Uncompressed default may make it inert).
- 438 icons: Mipmaps enabled for UI thumbnail.

The shared `AutoOptimizationProcessor.OnPreprocessTexture` explicitly enables mipmaps and Crunch for Default/other non-Sprite textures. Default platform settings are inspected separately from legacy root maximum-size fields. A Crunch flag with textureCompression=0 does not prove effective GPU compression. No import metadata was changed.

## Limits

- Automated candidate acceptance is not visual attractiveness, item semantics, license clearance, or owner approval.
- Source mapping checks manifest paths, GUIDs, source/icon hashes and historical render records; it cannot prove which geometry was rasterized.
- No Unity launch or regeneration in Stage C0; material bindings and actual source orientation remain review items.
- Image occupancy, color variance and magenta diagnostics are review triggers, not proof of material errors.
- Existing catalog categories are provisional; controller-camera failures are corrected from serialized component metadata.

## Per-asset results

| Asset | Category | Status | Technical score | Review flags |
| --- | --- | --- | --- | --- |
| Bread (66da8c9c) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| prop_fence_02 (666823a9) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| Third Person Camera (01f62690) | Unclassified | Failed | — | No active mesh renderers. |
| Axe (178e05e2) | Farming Tools | Needs visual review | 100 | Subject extent below 50% of canvas; inspect at small UI sizes |
| Fence (05ef4aae) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| Third Person Camera (ed1b911a) | Unclassified | Failed | — | No active mesh renderers. |
| fence_1x4 (1cacd835) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| food_apple (d35f59de) | General Inventory | Needs visual review | 100 | Semantic mismatch: prefab named food_apple renders rice-ball; source prefab reference GUID 392cda31ce705bd4d9a031f7c8e0ef76 resolves to Kenney rice-ball.fbx. Do not integrate as an apple. |
| food_bottle (c941e063) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| construction-fence (c76f8d0c) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| construction-fence (592f0e15) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| construction-fence (7fad74f3) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-1x2 (8872fcce) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-1x3 (4a2ef863) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-1x4 (e30f7733) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-2x2 (0ee82220) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-2x3 (8876591a) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-3x2 (96462329) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-3x3 (9b99afd1) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-low (56c61729) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence (822a6333) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-1x2 (34441a90) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence-1x3 (decee440) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence-1x4 (d329c6a5) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence-2x2 (3ff5198a) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence-2x3 (b9cdd4a6) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence-3x2 (cf5ac148) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence-3x3 (6233fea7) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence-low (8c3d8295) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence (006e5ad5) | Agriculture | Failed | — | No imported GameObject geometry at source path. |
| fence-1x2 (35097808) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-1x3 (20a59986) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-1x4 (01e6d48e) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-2x2 (a5203d71) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-2x3 (48b0083e) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-3x2 (a2acc005) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-3x3 (29b93457) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence-low (95a6664d) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence (c8b0d464) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| apple-half (9a86aedd) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| apple (2a49b642) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| banana (424166b6) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| beet (bf44b6cc) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| bottle-ketchup (182132d4) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| bottle-musterd (b2bac508) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| bottle-oil (a52281b5) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| bread (feb62220) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| burger-cheese-double (3467de6d) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| burger-cheese (33d9f70a) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| burger-double (da384928) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| burger (df5f9ddc) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| cabbage (78ec76aa) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| can-open (f1763c57) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| can-small (7d8be24c) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| can (96faaafd) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| candy-bar-wrapper (9da5ba42) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| candy-bar (a96b8d11) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| carrot (0ef01fab) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| corn-dog (63766af5) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| corn (6c580793) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| egg-cooked (4afaaa1c) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| egg-cup (d2168405) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| egg-half (f30fdb26) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| egg (2747bc9a) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| eggplant (997c28d8) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| ginger-bread-cutter (136c8799) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| ginger-bread (47483eed) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| maki-vegetable (6a677826) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| mushroom-half (e24bc818) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| mushroom (d5370323) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| onion-half (dcc40744) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| onion (36d4cd22) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| orange (eb940948) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| pepper-mill (23c9f0b5) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| pepper (09e6e4fc) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| popsicle-chocolate (a2f38132) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| popsicle-stick (bac616bd) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| popsicle (574e84bb) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| pumpkin-basic (8a8e226f) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| pumpkin (a19f4b99) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| radish (bda99496) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| rice-ball (c8e0ef76) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| sandwich (a7db9014) | General Inventory | Needs visual review | 100 | Subject extent below 50% of canvas; inspect at small UI sizes; Subject bounding box off center by more than 12% of canvas diagonal units |
| shaker-pepper (f9ee0b73) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| skewer-vegetables (9b146495) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| skewer (8546a751) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| soda-bottle (56a092ca) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| soda-can-crushed (5005a3d4) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| soda-can (ffb3c016) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| strawberry (15c099a6) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| sushi-egg (5b4a763e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| sushi-salmon (a4f30a88) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| tomato-slice (56c4dff2) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| tomato (a337365c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| watermelon (d104f156) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| apple-half (228db855) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| apple (6e07b09f) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| banana (f989ba0c) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| beet (dd48bd53) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| bottle-ketchup (25919044) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| bottle-musterd (a3c2432d) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| bottle-oil (fc4da4a5) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| bread (843fa02a) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| burger-cheese-double (0c893ac4) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| burger-cheese (e1b081f9) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| burger-double (2933b402) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| burger (8854fd90) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| cabbage (2de84477) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| can-open (19484c94) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| can-small (b30c19a2) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| can (f1110d0c) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| candy-bar-wrapper (fcebfcec) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| candy-bar (9c46ed1e) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| carrot (0f99f0d4) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| corn-dog (40ef9bef) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| corn (2530554e) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| egg-cooked (48b1ec2b) | Animals and Animal Products | Failed | — | No imported GameObject geometry at source path. |
| egg-cup (2e1df66c) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| egg-half (84450c33) | Animals and Animal Products | Failed | — | No imported GameObject geometry at source path. |
| egg (6ee24eef) | Animals and Animal Products | Failed | — | No imported GameObject geometry at source path. |
| eggplant (262a3a6e) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| ginger-bread-cutter (ef689588) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| ginger-bread (208cc999) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| maki-vegetable (2af566d8) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| mushroom-half (fbc49796) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| mushroom (7080b930) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| onion-half (bee54d49) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| onion (549992fe) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| orange (c76a43c0) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| pepper-mill (3534f6d3) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| pepper (9335dd30) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| popsicle-chocolate (7ee5c18f) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| popsicle-stick (eb26e011) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| popsicle (6143ca06) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| pumpkin-basic (91e60285) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| pumpkin (e4a68dcc) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| radish (f2a5fc75) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| rice-ball (282471fe) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| sandwich (e356e0d8) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| shaker-pepper (7dd81ab3) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| skewer-vegetables (0aa58d7e) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| skewer (16055be7) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| soda-bottle (59dfdef0) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| soda-can-crushed (ae9b47e3) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| soda-can (0ded7a55) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| strawberry (65e90bca) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| sushi-egg (eb3e3168) | Animals and Animal Products | Failed | — | No imported GameObject geometry at source path. |
| sushi-salmon (87fc1740) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| tomato-slice (4c892dab) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| tomato (428258b0) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| watermelon (5b81d925) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| apple-half (9ef24ed9) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| apple (4a255bf2) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| banana (1ef4ed15) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| beet (7030e697) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| bottle-ketchup (1f44fc3a) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| bottle-musterd (f8449b00) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| bottle-oil (e60ceca4) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| bread (14116522) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| burger-cheese-double (ca5d4858) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| burger-cheese (58595b2b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| burger-double (a2eda1ab) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| burger (901f3e0d) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| cabbage (1f4dd536) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| can-open (b636dec5) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| can-small (e9db3c36) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| can (7c4d92db) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| candy-bar-wrapper (d495d10f) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| candy-bar (4a5b23f4) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| carrot (e384c864) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| corn-dog (fc5851f4) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| corn (414692a1) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| egg-cooked (42b9bdbc) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| egg-cup (c11b5d36) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| egg-half (960f0d0a) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| egg (78cd149a) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| eggplant (5561ba8c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| ginger-bread-cutter (68ca8952) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| ginger-bread (5c3b8a58) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| maki-vegetable (907e4b95) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| mushroom-half (f3b965e1) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| mushroom (88f0f010) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| onion-half (2e144010) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| onion (8f5281a5) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| orange (abba71e2) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| pepper-mill (94a9a537) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| pepper (c28b3c25) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| popsicle-chocolate (fb717b1c) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| popsicle-stick (a01630c0) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| popsicle (1b4f9afb) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| pumpkin-basic (d005604e) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| pumpkin (1b19ac05) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| radish (4cbf4a9b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| rice-ball (52b2630e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| sandwich (fc97ff4d) | General Inventory | Needs visual review | 100 | Subject extent below 50% of canvas; inspect at small UI sizes; Subject bounding box off center by more than 12% of canvas diagonal units |
| shaker-pepper (6ee488c3) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| skewer-vegetables (0004bcb2) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| skewer (2414b1c0) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| soda-bottle (6d8e8d39) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| soda-can-crushed (3b4b810b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| soda-can (e10db3d7) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| strawberry (15ce2828) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| sushi-egg (bc4932b9) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| sushi-salmon (26e68a95) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| tomato-slice (f4aa0bd2) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| tomato (aec7e69b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| watermelon (0e3275f1) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| computerKeyboard (7720720b) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| computerMouse (3df47b07) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| computerScreen (019ba15b) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| laptop (77aae25b) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionAntenna (1a28822c) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionModern (e179280a) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionVintage (e71d1548) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| computerKeyboard (f3a735bc) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| computerMouse (6c751865) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| computerScreen (55508d11) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| laptop (2b705864) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionAntenna (02c0145a) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionModern (514ab377) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionVintage (0201b989) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| computerKeyboard (0c93f3be) | Technology | Failed | — | No imported GameObject geometry at source path. |
| computerMouse (f12b5b5f) | Technology | Failed | — | No imported GameObject geometry at source path. |
| computerScreen (e57f2fc0) | Technology | Failed | — | No imported GameObject geometry at source path. |
| laptop (44035dff) | Technology | Failed | — | No imported GameObject geometry at source path. |
| televisionAntenna (3818011b) | Technology | Failed | — | No imported GameObject geometry at source path. |
| televisionModern (b613e564) | Technology | Failed | — | No imported GameObject geometry at source path. |
| televisionVintage (e76b2f96) | Technology | Failed | — | No imported GameObject geometry at source path. |
| computerKeyboard (4594221b) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| computerMouse (a77dd45a) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| computerScreen (32c10d54) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| laptop (79ef477d) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionAntenna (3461ddef) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionModern (20d30baf) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| televisionVintage (236ce63f) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| TV (38a59a8b) | Technology | Accepted candidate | 100 | Visual/material approval pending |
| TV (98acdae1) | Technology | Failed | — | No imported GameObject geometry at source path. |
| Environment_Bottle (584d9fca) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Environment_CanFridge (9d4859a0) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Environment_Bottle (271af907) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Environment_CanFridge (773cdb4c) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Environment_Bottle (8c9c6c30) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Environment_CanFridge (ad51ae77) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Chukaman (50a17907) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Dango (5aa0a74e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_EbiNigiri (3da54a62) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Gyoza (a6737685) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_MaguroNigiri (5b45c420) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_OctopusNigiri (9eafdc1b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Onigiri (cab3eb51) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Ramen (7edeb7c1) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Roll (46fbb8b1) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_SalmonNigiri (baeea544) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_SalmonRoll (89ba9887) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_SeaUrchinRoll (3bc6ce6e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_TamagoNigiri (a2d6f864) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Udon (1ff239be) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Wasabi (9e202b32) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Avocado (517f8cef) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Crabsticks (f2e6bf69) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Cucumber (83b01ebb) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Ebi (96fbb0ad) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Eel (67858720) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Fish (4b996422) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_FishFillet (7905d13a) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Flounder (c4d69959) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Mackerel (3b074d8e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Nori (892b83a9) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Octopus (3cce4cf4) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Rice (be6ecf0b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Salmon (38f0c231) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_SalmonFish (65912b75) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_SeaUrchin (46cfea66) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_SeaUrchinOpen (0e43681a) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Shimesaba (14f99fe2) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_SlicedCucumber (d3e7eb04) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Squid (c0c582d1) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Tentacle (6a8478a8) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Tuna (2d6af424) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Chukaman (851fcc0a) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_Dango (c793221b) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_EbiNigiri (fe180fd8) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_Gyoza (f3dcf426) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_MaguroNigiri (f62a5418) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_OctopusNigiri (beabca2d) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_Onigiri (e635bc49) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_Ramen (c6b7e2ec) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_Roll (b5cfd194) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_SalmonNigiri (d55ac3ef) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_SalmonRoll (ee898ebe) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_SeaUrchinRoll (845c167e) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_TamagoNigiri (867e9c9c) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_Udon (9a6c41d9) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_Wasabi (fee9a374) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Avocado (4c6c6966) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Crabsticks (e125ce88) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Cucumber (2f11f646) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Ebi (346fdb66) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Eel (e46f4bdd) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Fish (cfc46277) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_FishFillet (e3c3cb12) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Flounder (dc1ee056) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Mackerel (db80ab74) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Nori (7a0c4782) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Octopus (4cb01876) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Rice (f8f0c591) | Seeds and Crops | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Salmon (4daa3b82) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_SalmonFish (50648cc4) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_SeaUrchin (629d4a37) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_SeaUrchinOpen (a6d105ac) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Shimesaba (8d46c50b) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_SlicedCucumber (7da8f747) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Squid (84d36fa1) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Tentacle (9f8e4b17) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| FoodIngredient_Tuna (779d9859) | General Inventory | Failed | — | No imported GameObject geometry at source path. |
| Food_Chukaman (b1455697) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Dango (fdca815b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_EbiNigiri (dc219354) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Gyoza (837f7ad8) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_MaguroNigiri (053c75d8) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_OctopusNigiri (651076d3) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Onigiri (b53d067d) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Ramen (c73d8c69) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Roll (34c6454b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_SalmonNigiri (4e1c4ed7) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_SalmonRoll (bde99de5) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_SeaUrchinRoll (727e0b02) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_TamagoNigiri (b570d90e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Udon (03ad04bd) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Food_Wasabi (6023834e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Avocado (b6c1d087) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Crabsticks (35424390) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Cucumber (ea735872) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Ebi (aaa6921d) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Eel (600b0a70) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Fish (d32d06e5) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_FishFillet (89732bd3) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Flounder (b9d82268) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Mackerel (1e657368) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Nori (d5f2ee1c) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Octopus (d4df0c6c) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Rice (ce114821) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Salmon (0cfdc7ed) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_SalmonFish (2cc3aa9b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_SeaUrchin (6834f874) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_SeaUrchinOpen (2ed2c598) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Shimesaba (db7f5a5d) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_SlicedCucumber (26dea4aa) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Squid (0b9deea1) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Tentacle (14ecc2d3) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| FoodIngredient_Tuna (c3d1a06e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Bull (45cf170a) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Cow (84fc5da2) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Horse (721e290d) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Horse_White (bae40b96) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Bull (62b2b89d) | Animals and Animal Products | Failed | — | No imported GameObject geometry at source path. |
| Cow (b1b8a783) | Animals and Animal Products | Failed | — | No imported GameObject geometry at source path. |
| Horse (fe8ff35a) | Animals and Animal Products | Failed | — | No imported GameObject geometry at source path. |
| Horse_White (d2dedc67) | Animals and Animal Products | Failed | — | No imported GameObject geometry at source path. |
| Bull (11233810) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Cow (ac5c50ac) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Horse (38ac2c5f) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Horse_White (5d99bca2) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Apple (2867877b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Apple_Green (59ee18fd) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Banana (f3b56c80) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Bread (6fb0ad41) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Bread_Slice (f412021a) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Burger (57fad132) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Burger_Bread (3f9f4f2b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| BurgerLarge (46179dc9) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| BurgerPatty_Burned (6f4458f5) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| BurgerPatty_Cooked (72a84b40) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| BurgerPatty_Raw (bb7f82ae) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Carrot (fff606b1) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| ChickenLeg (dc728436) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Egg_Burned (00e0d7a1) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Egg_Fried (5ac98a47) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Egg_Whole (2d8c1dd2) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Egg_Whole_White (87a58bf3) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Eggplant (90e133a9) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| KetchupBottle (da0145da) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce (c5957008) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce_Whole (c9cc75da) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| MayoBottle (b17d5f5c) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom (7b0bc5f7) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom_Sliced (783f7e63) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| MustardBottle (67665dcd) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Orange (4d870ad4) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pepper_Green (fbeb7d7e) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pepper_Red (d2f25483) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Popsicle_Chocolate (62592a0d) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Popsicle_Multiple (e7b7987b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Popsicle_Strawberry (1d3e4490) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Pumpkin (c279e61e) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_Nigiri1 (3052cd7e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_Nigiri2 (05808c54) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_NigiriOctopus (4b2e4e6f) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_Roll1 (46c44124) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_Roll2 (8a740825) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Tomato (84e9fdd7) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Tomato_Slice (280624f1) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Apple (a5e2f5a0) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Apple_Green (ed50a194) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Banana (f697bebd) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Bread (0d6f5dd7) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Bread_Slice (5a11f544) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Burger (4dee05b0) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Burger_Bread (1f3613eb) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| BurgerLarge (9096cca5) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| BurgerPatty_Burned (70bc392e) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| BurgerPatty_Cooked (af7b56f0) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| BurgerPatty_Raw (fb7309f0) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Carrot (209c4145) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| ChickenLeg (62d1129b) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Egg_Burned (61965cc9) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Egg_Fried (0e993d44) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Egg_Whole (0d01a5cc) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Egg_Whole_White (9f865132) | Animals and Animal Products | Accepted candidate | 100 | Visual/material approval pending |
| Eggplant (c67d244e) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| KetchupBottle (b56fff22) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce (fa6de289) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce_Whole (39ec6d30) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| MayoBottle (82fb52a4) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom (5e4e5295) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom_Sliced (b04a4680) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| MustardBottle (a237632f) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Orange (8997940b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pepper_Green (a12261e7) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pepper_Red (d8dd08c9) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Popsicle_Chocolate (e8a3bcec) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Popsicle_Multiple (027ab5d2) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Popsicle_Strawberry (83cde806) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Pumpkin (9b704342) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_Nigiri1 (670baab9) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_Nigiri2 (b6b598f7) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_NigiriOctopus (a970f0ef) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_Roll1 (7851d8a0) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Sushi_Roll2 (af19498b) | General Inventory | Accepted candidate | 100 | Visual/material approval pending |
| Tomato (bf0c682a) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Tomato_Slice (7df694dd) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Barn (d1d2d794) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| BigBarn (73e72003) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| ChickenCoop (88cb172b) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| Fence (898dbf9a) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| Fence2 (7f4c782f) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| OpenBarn (689db42b) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| Silo (0c02c7a5) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| Silo_House (c962c5b2) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| SmallBarn (8af3a0d0) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| TowerWindmill (87efe1c9) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| WaterTower (c2e6ab70) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| Well (0f4d595c) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| Windmill (97014ff7) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| barrel (b4c2a14f) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| barrelOpen (146b2945) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| bedroll (7981729a) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| bedrollPacked (06fab578) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| bedrollFrame (f4928307) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| box (f38c486f) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| boxOpen (c0770f3e) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| campfire (d93162a0) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| chest (e9e041a0) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| fenceFortified (d2bbd42f) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fence (bfe5be16) | Agriculture | Accepted candidate | 100 | Visual/material approval pending |
| fish (e26b4961) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| fishingStand (9a7867a3) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| floor (228154cf) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| grass (df52784f) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| grassLarge (4d785b0e) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| toolPickaxe (97cf30a7) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| resourceStone (92db1659) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| rockA (eb804e2f) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| rockB (ee72e9ed) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| rockC (31894ce4) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| rockFlat (bb8b513a) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| rockFlatGrass (c1808439) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| toolShovel (6eef0e80) | Farming Tools | Accepted candidate | 100 | Visual/material approval pending |
| signpost (29b987af) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| signpostSingle (55ebb5f4) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| structure (f92076da) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| structureBase (33451537) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| structureCloth (93258717) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| structureRoof (dd2fd180) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| tentClosed (572e98c4) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| tent (4962776f) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| tentHalf (6bf8f6fe) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| toolAxe (2aaf4f7f) | Farming Tools | Accepted candidate | 100 | Visual/material approval pending |
| toolHoe (97579fec) | Farming Tools | Accepted candidate | 100 | Visual/material approval pending |
| tree (16ae6134) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| treeFall (02bdb5ed) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| treeFallLarge (326740be) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| treeLarge (81c5baf9) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| resourceWood (2702e21e) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| workbench (964ec0e0) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| workbenchAnvil (b447835b) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| workbenchGrind (62a805fe) | Unclassified | Not generated | — | Unclassified candidate; not queued for rendering |
| Apple_1 (16b9124a) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Apple_2 (f007d931) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Apple_3 (eaf3c490) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Apple_4 (4280a0b3) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Apple_Crop (59e9dc18) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Apple_Harvested (21a990d7) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Bamboo_1 (83d327c3) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Bamboo_2 (9cb69c32) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Bamboo_3 (eef0f303) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Bamboo_4 (65be1e08) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Bamboo_Crop (c4d4a972) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Beet_1 (a8795c49) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Beet_2 (986c1331) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Beet_3 (590b5251) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Beet_4 (8024dd7c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Beet_Crop (5ea60840) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| BushBerries_1 (3416def6) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| BushBerries_2 (fa17ea86) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| BushBerries_3 (b3b37adf) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| BushBerries_4 (c43fe6e4) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| BushBerries_Crop (96a4e7e4) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| BushBerries_Harvested (f4ba734b) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Cactus_1 (8e050197) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Cactus_2 (07d5a694) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Cactus_3 (dd3a0761) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Cactus_4 (06837d97) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Cactus_Crop (f2f57080) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Cactus_Harvested (32541379) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Carrot_1 (d841e99b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Carrot_2 (6aa04fa0) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Carrot_3 (b7ca7c36) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Carrot_4 (73ccbabe) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Carrot_Crop (f1f6bc8c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Coconut_Half (043264b5) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Corn_1 (3ec41e57) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Corn_2 (955d07e3) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Corn_3 (041a3c80) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Corn_4 (8c6252c0) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Corn_Crop (a81ad190) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Corn_Harvested (acbe4e5a) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Flower_1 (7a9a236e) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Flower_2 (8f0262f4) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Flower_3 (7baa3eae) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Flower_4 (a34aaaf2) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Flowers_Crop (0b9168f4) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Flowers_Harvested (4586be6c) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Grass_1 (5e8fc17b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Grass_2 (3cd58edc) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Grass_3 (7cd1e971) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Grass_4 (159f6bdf) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce_1 (dfe2b203) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce_2 (bbbe15ba) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce_3 (5d12f113) | Seeds and Crops | Needs visual review | 100 | Subject extent below 50% of canvas; inspect at small UI sizes |
| Lettuce_4 (a704bb59) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce_Crop (a89adbe1) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Lettuce_Harvested (3571c039) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Mushroom_1 (2dac4fc7) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom_2 (0c096c1e) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom_3 (20f9fbb6) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom_4 (b915e222) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom_Crop (1f21cc4c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Mushroom_Harvested (e4155c98) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Orange_1 (ea05de75) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Orange_2 (a5ba180c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Orange_3 (a3707678) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Orange_4 (4df5fce7) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Orange_Crop (e5286fc5) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Orange_Harvested (dd9127fb) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| PalmTree_1 (89bf7790) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| PalmTree_2 (3fec65d8) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| PalmTree_3 (7742481b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| PalmTree_4 (fd20b44c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| PalmTree_Crop (4b312046) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| PalmTree_Harvested (e6c07a62) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Pumpkin_1 (1d5b127c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pumpkin_2 (b3bca84b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pumpkin_3 (94cd7194) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pumpkin_4 (6547e338) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pumpkin_Crop (2566ecb7) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Pumpkin_Harvested (8980e863) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Rice_1 (a5e8f262) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Rice_2 (c7bc8a9d) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Rice_3 (cc0ab576) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Rice_4 (53697c3c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Rice_Crop (c74679b4) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Tomato_1 (e69f958b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Tomato_2 (b9281509) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Tomato_3 (80176098) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Tomato_4 (8647c747) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Tomato_Crop (c389448c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Tomato_Harvested (ec3ad182) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Watermelon_1 (3e71f21b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Watermelon_2 (e6661cd3) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Watermelon_3 (1485125c) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Watermelon_4 (99bbfd43) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Watermelon_Crop (c96f1d7a) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Watermelon_Harvested (4159a3b6) | Seeds and Crops | Needs visual review | 100 | Actual PNG depicts the plant after harvesting; suffix does not establish harvested produce. Confirm intended inventory semantics. |
| Wheat_1 (b8ab647e) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Wheat_2 (e28cfd0b) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Wheat_3 (3484dc3d) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Wheat_4 (7035e975) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
| Wheat_Crop (44beb3ba) | Seeds and Crops | Accepted candidate | 100 | Visual/material approval pending |
