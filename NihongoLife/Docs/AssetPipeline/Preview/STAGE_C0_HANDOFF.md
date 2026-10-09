# Stage C0 handoff

Double-click `index.html` in this folder. It uses relative local PNG paths and classic scripts: no server, CDN, package installation or Unity setup is needed. Keep this Preview folder within the repository alongside Assets and Reports; copying the HTML alone loses the images.

## Verified delivery

- 599 source candidates, 556 queued jobs, 438 real valid PNGs, 118 failed jobs, 43 unqueued candidates. No orphan PNGs or original source/hash/GUID/manifest mismatches.
- 421 images pass the technical candidate checks without review flags; 17 require measured or semantic review. Every one of the 438 PNGs was inspected in eleven actual contact sheets. Technical passes and contact-sheet inspection are not owner approval: approvals remain zero.
- 116 failures are valid GLB/glTF containers that the historical Unity render could not load as GameObjects. Each has an existing same-pack/basename alternative icon. Recovery links preserve separate identities; source equivalence is unverified. Two failed prefabs are camera controllers without renderable meshes and should not be shop items.
- 33 groups of identical images, 15.48 MiB PNG payload. Mipmap and shared-importer issues are documented in the readiness report.

## Owner review

Search by filename, ID or path; select a category; toggle Include missing to see failed/unqueued records. Status filters, pagination, light/dark surfaces and checkerboard controls work locally. Open a card for the full PNG, source/icon paths and copy controls, technical measurements, contact-sheet cell, warnings and 32/48/64 px samples. Shortlist hearts save candidates locally; Export shortlist downloads JSON with ownerApproved=false. A shortlist is not approval.

Shop study contains Agriculture, Fashion, Technology and General Inventory tabs with a selected preview. Fashion is explicitly empty. Japanese item labels and prices remain marked unassigned unless linked to an existing runtime item definition. Balance is unassigned; owned/unavailable controls are mock display states. No purchase action exists.

Quality & recovery provides all eleven contact sheets and individual diagnoses for all 118 failures, including links to available alternatives. See [quality](../Reports/STAGE_C0_QUALITY.md), [readiness](../Reports/STAGE_C0_READINESS.md), and the JSON reports for machine-readable details. `stage_c0_visual_review.json` records all 438 contact-sheet inspections and curated category exceptions. Source names remain unchanged. The food_apple semantic warning is backed by a serialized prefab reference to Kenney rice-ball.fbx.

## Claude integration

Use the exported owner shortlist and exact source/icon paths after owner review; do not bulk-integrate technical passes. Resolve category, labels, price and source equivalence first. The current generated icon folder is not automatically a runtime item catalog. Make import/runtime changes in a separately authorized task, accounting for the shared texture postprocessor. Coordinate Unity access before rerendering or Play Mode checks. Do not swap an alternative icon into a failed source record.

No scenes, runtime UI, shop logic, player/camera systems, exam files, source models, original PNGs or import metadata were changed. No Unity session, staging or commit was performed. Concurrent workspace changes were preserved.

## Validation and reproduction

25 pipeline unit tests passed, including nine C0 synthetic PNG/path/container tests. Twenty headless Chrome checks passed against direct file:// URLs: all 438 PNGs loaded, pagination covered all 438, and filters, missing records, semantic detail, copy feedback, dialog dismissal, background toggles, shop states and mobile/tablet layouts worked with no page errors or failed local requests. Browser screenshots and results are in `../Reports/StageC0BrowserQA/`. The in-app browser reported no available browser, so standalone headless Chrome supplied browser QA.

Run from repository root:

```powershell
python -m unittest discover -s NihongoLife/Tools/asset_pipeline -p 'test*.py'
python NihongoLife/Tools/asset_pipeline/stage_c0.py
# Browser QA only; point to an installed Playwright package and installed Chrome.
$env:C0_PLAYWRIGHT = '<absolute path to playwright package>'
node NihongoLife/Tools/asset_pipeline/gallery_smoke.cjs
```

The generator writes only C0 reports, contact sheets and gallery data. Existing older utilities can overwrite their own old reports; they were not run against the shared delivery. Original icon/import/report hashes were compared to the start-of-C0 snapshot and remained unchanged. See `stage_c0_verification.json` for evidence. No Unity material fidelity, source orientation, scene readiness or owner approval is claimed.
