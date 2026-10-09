# Asset pipeline handoff

This package adds editor-only rendering and local metadata validation. It changes no runtime item definitions, shop behavior, gameplay scene, or exam grading. Work started on `codex/asset-pipeline-audit`; the checkout is shared with another engineer, so unrelated concurrent changes must not be included when staging this package.

## Source audit

From the outer repository root:

```powershell
python NihongoLife/Tools/asset_pipeline/audit.py
python -m unittest discover -s NihongoLife/Tools/asset_pipeline -p test_pipeline.py -v
```

The reports in `Reports/` contain actual local paths, GUIDs where available, SHA-256 checksums, sizes, growth-stage suggestions, missing translations, license evidence, runtime item labels, duplicate files, same-name assets, and proposed shop categories. IDs are `normalized_source_stem_<full Unity GUID>`; missing metadata uses a deterministic path digest and is explicitly flagged. Import once and persist newly generated model metadata before relying on GUID identity. Renaming a model changes its readable ID prefix; review references before regenerating after renames.

`asset_catalog.json` is a candidate catalog, not an approved item database. Numeric crop suffixes are growth-stage suggestions, not confirmed growth timings. English names are source filenames; Japanese and Vietnamese names are left empty unless transcribed from an existing runtime catalog. No external translation verification is claimed. Static runtime extraction covers literal `AddItem`/`KonbiniProduct` calls; dynamic scenario IDs still need review. The three known model mappings are transcribed from the existing `ItemIconRenderer`, which remains untouched.

Closest ancestor license files are recorded as evidence, including locally explicit CC0 statements. This does not establish rights for a downloaded bundle or every nested file. Unverified sources remain unverified. No paid dependency was added.

## Render and review

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.12f1/Editor/Unity.exe' -batchmode `
  -projectPath 'D:/VTC_Academy/NihongoLife/NihongoLife/NihongoLife' `
  -executeMethod NihongoLife.EditorTools.AssetPipelineIconBatch.Render `
  -logFile 'D:/VTC_Academy/NihongoLife/NihongoLife/asset-pipeline-render.log'
python NihongoLife/Tools/asset_pipeline/check_icons.py
python NihongoLife/Tools/asset_pipeline/audit.py
python NihongoLife/Tools/asset_pipeline/verify_delivery.py
```

Use `-iconLimit 12` for a smoke batch, or `-iconManifest <path>` for a reviewed subset of the same schema. The manifest supports 256 or 512 square pixels and a margin of 0.05 to 0.30 per edge. Keep graphics enabled: do not pass `-nographics`. The renderer has its own editor assembly with existing URP references; no shared assembly modification or menu action is needed.

The renderer uses actual imported geometry, an orthographic three-quarter camera, camera-projected bounds, transparent output, and scoped preview lighting. The preview workspace is transient and unsaved; no `.unity` asset is created or gameplay scene opened. Scripts, embedded cameras/lights, and animation playback are disabled in temporary clones. Imported URP materials are copied without changing their source. Opaque Standard and Legacy Diffuse materials receive a temporary URP conversion preserving base texture/color and UV transform. Standard conversion also preserves smoothness and selects the corresponding metallic/specular workflow. Other/custom/transparent Standard materials are reported for reviewed conversion rather than silently substituted. The conversion does not guarantee equivalent advanced PBR maps; compare original appearance before approving an icon.

Outputs persist under `Assets/NihongoLife/Generated/AssetPipeline/Icons/`, separate from all existing icons. A render failure never overwrites its output; previous files can remain from an earlier successful run, so consult `render_results.json` for current status. The batch exits with status 1 if any selected job fails. GLB/GLTF files without a GameObject importer remain unrendered; other imported format alternatives are indexed separately. Identical-byte duplicates and same-name models are distinct reports: same name does not prove equivalent geometry.

`icon_quality.json` reports PNG size, decoded RGBA memory, dimensions, alpha, content bounds and identical-byte duplicates. Existing icon issues are reported without repair. Visual review remains required for material fidelity, LODs, awkward pivots, very sparse meshes and category relevance. Fixed settings and ordering support reproducible runs; verify PNG hashes on the same Unity/GPU setup before expecting byte-identical output across different hardware.

The delivered two full runs have 437 common PNGs with byte-identical SHA-256 hashes. One additional Legacy Diffuse asset rendered after conversion support was added. All five audit JSON reports were also byte-identical across two runs with stable inputs. See `delivery_verification.json` and `metadata_repeatability.json`; this is same-machine evidence, not a cross-platform guarantee.

Unity imported the Midori source folder and created missing `.meta` sidecars. These preserve GUID identity and import settings; no original models/textures were replaced. Other engineers' edits, font updates, and staged removals of copyrighted exam PDFs remain outside this package. Stage explicit owned paths rather than the entire shared checkout.

Claude integration steps:

1. Review candidate categories, growth stages, translations, source/license evidence, duplicates and `render_results.json`.
2. Select exact model-to-item mappings; do not equate every apple growth stage with a harvested inventory apple.
3. Copy only approved thumbnails into `Resources/Items/<existing_item_id>.png`, with ownership of that destination agreed first. Preserve texture imports as Default, alpha transparency, no mipmaps, clamp; existing `ItemIcons.Get` loads Texture2D and builds its Sprite.
4. Keep unknown/unsupported sources in the missing report. Import-ready candidates are persisted, but they are not automatically wired into the runtime inventory.
5. If integrating into scenes or shop UI, perform Play Mode visual and collision testing before calling those scenes complete. This package makes no scene-completion claim.

## Exam metadata and validation

`exam_inventory.json` inventories local PDFs and notes without copying question text. All pages are checked locally for text availability and skill/test/answer-key headings. Keyword matches are suggestions; they do not prove complete test sections or usable answer keys. Audio presence is limited to local files. Volume/book names come from filenames. Distribution permission remains unverified. Existing source PDFs already tracked in the repository were not modified, staged or uploaded by this work.

The audit and validator require Python 3.11 or later and the standard library. Optional local QA uses the already installed PyMuPDF for PDF metadata/text availability and Pillow for PNG metadata. The audit reports an unavailable PDF reader rather than adding a dependency. These tools do not upload content or change exam files.

```powershell
python NihongoLife/Tools/asset_pipeline/validate_exam.py path/to/future_dataset.json --media-root path/to/media
```

The validator matches the current numeric enums: JLPT sections 0/1/2, IELTS sections 3/4/5/6, question types 0 through 4. It checks identifiers, passage references, section ordering, keys, points/scales/pass criteria, band metadata, local media existence and path containment. It tolerates partial ordered practice sections and treats Essay/SpeakingPrompt as subjective types without fabricated binary keys.

Future portable media fields are `audioPath`, `imagePath`, `videoPath`, `mediaPath`, relative to `--media-root`, using forward slashes. These validation-only fields are not a new runtime import contract. Existing Unity `audioClip` references require editor review; HTTPS media is never fetched and is flagged as unverified. The CLI emits only issue codes and field locations, exits 0 for valid data and 1 for issues or malformed JSON. Tests use synthetic metadata with no protected exam questions.
