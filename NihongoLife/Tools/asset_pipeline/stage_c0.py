"""Read-only Stage C0 inspection; emits reports, contact sheets and offline gallery data.

Never imports Unity, edits models/icons/import metadata, or touches exam source content.
"""
import argparse
import hashlib
import json
import math
import re
import struct
from collections import Counter, defaultdict
from pathlib import Path
from urllib.parse import quote, unquote, urlsplit

from PIL import Image, ImageDraw, ImageFont, ImageStat
from audit import digest, write_json

GROUPS = {
    'Crops': ('Seeds and Crops', 'Agriculture'), 'Seeds': ('Seeds and Crops', 'Agriculture'),
    'FarmingTools': ('Farming Tools', 'Agriculture'),
    'FarmAnimals': ('Animals and Animal Products', 'Agriculture'),
    'AnimalProducts': ('Animals and Animal Products', 'Agriculture'),
    'FarmStructures': ('Agriculture', 'Agriculture'), 'Fashion': ('Fashion', 'Fashion'),
    'Technology': ('Technology', 'Technology'), 'ShopItems': ('General Inventory', 'General Inventory'),
    'Unclassified': ('Unclassified', None),
}
CATEGORIES = ['Agriculture', 'Seeds and Crops', 'Farming Tools', 'Animals and Animal Products',
              'Fashion', 'Technology', 'General Inventory', 'Unclassified']


def safe_path(project, value):
    """All manifest reads must stay within the project's Assets directory."""
    if not isinstance(value, str) or '\\' in value or '\x00' in value:
        raise ValueError('Invalid asset path')
    path = (project / value).resolve()
    if not value.startswith('Assets/') or not path.is_relative_to((project / 'Assets').resolve()):
        raise ValueError('Asset path outside Assets')
    return path


def image_metrics(path):
    result = {'exists': path.is_file(), 'valid': False}
    if not path.is_file():
        return result
    result.update(bytes=path.stat().st_size, sha256=digest(path))
    try:
        with Image.open(path) as original:
            original.verify()
        with Image.open(path) as original:
            result.update(width=original.width, height=original.height, sourceMode=original.mode,
                          format=original.format, valid=original.format == 'PNG')
            rgba = original.convert('RGBA')
            alpha = rgba.getchannel('A')
            hist = alpha.histogram()
            size = original.width * original.height
            mask = alpha.point(lambda a: 255 if a > 8 else 0)
            box = mask.getbbox()
            visible = sum(hist[9:])
            result.update(transparentFraction=round(hist[0] / size, 6), visibleFraction=round(visible / size, 6),
                          nonempty=visible > 0, contentBounds=list(box) if box else None,
                          decodedRgbaBytes=size * 4, alphaExtrema=list(alpha.getextrema()))
            if box:
                w, h = original.size
                occupancy = max((box[2] - box[0]) / w, (box[3] - box[1]) / h)
                margins = [box[0] / w, box[1] / h, (w - box[2]) / w, (h - box[3]) / h]
                center_offset = math.hypot((box[0] + box[2]) / (2 * w) - .5, (box[1] + box[3]) / (2 * h) - .5)
                stats = ImageStat.Stat(rgba.convert('RGB'), mask)
                rgb = [p for p in rgba.getdata() if p[3] > 8]
                magenta = sum(p[0] > 240 and p[1] < 25 and p[2] > 240 for p in rgb)
                result.update(maxSubjectExtent=round(occupancy, 6), minMargin=round(min(margins), 6),
                              normalizedMargins=[round(m, 6) for m in margins],
                              centerOffset=round(center_offset, 6), meanRgb=[round(v, 3) for v in stats.mean],
                              rgbStddev=[round(v, 3) for v in stats.stddev],
                              magentaFraction=round(magenta / len(rgb), 6))
                result['pixelDigest'] = hashlib.sha256(struct.pack('<II', w, h) + rgba.tobytes()).hexdigest()
    except Exception as exc:
        result['error'] = type(exc).__name__
    return result


def importer_settings(path):
    if not path.is_file():
        return {'exists': False, 'issues': ['Missing PNG import metadata']}
    text = path.read_text(encoding='utf-8-sig')

    def field(key, indent=2):
        match = re.search(r'^' + ' ' * indent + re.escape(key) + r':\s*([^\n\r]*)', text, re.M)
        return match.group(1).strip() if match else None

    default = {}
    platform = {}
    for block in re.split(r'(?m)^  - serializedVersion:', text)[1:]:
        values = dict(re.findall(r'^    ([A-Za-z0-9_]+):\s*([^\r\n]*)', block, re.M))
        if values.get('buildTarget') == 'DefaultTexturePlatform':
            default = values
        if values.get('buildTarget') == 'WebGL':
            platform = values
    issues = []
    if field('enableMipMap', 4) != '0':
        issues.append('Mipmaps enabled for UI thumbnail')
    if field('textureType') != '0':
        issues.append('Texture type differs from current Texture2D runtime loader')
    if field('alphaIsTransparency') != '1':
        issues.append('Alpha transparency not enabled')
    if default.get('crunchedCompression') == '1':
        issues.append('Crunch requested; inspect effective format (Uncompressed default may make it inert)')
    if default.get('maxTextureSize') not in {'256', '512'}:
        issues.append('Unexpected default platform maximum size')
    return {'exists': True, 'mipmaps': field('enableMipMap', 4) == '1', 'textureType': field('textureType'),
            'alphaIsTransparency': field('alphaIsTransparency'), 'readable': field('isReadable'),
            'srgb': field('sRGBTexture', 4), 'wrapU': field('wrapU', 4), 'wrapV': field('wrapV', 4),
            'rootMaxTextureSize': field('maxTextureSize'), 'defaultPlatform': default,
            'webglPlatform': platform, 'issues': issues}


def geometry_document(path, pack_root=None):
    """Inspect glTF/GLB JSON and URI existence locally, not Unity importability or geometric equivalence."""
    report = {'containerValid': False, 'externalReferences': [], 'missingLocalReferences': [], 'issues': []}
    try:
        if path.suffix.lower() == '.glb':
            raw = path.read_bytes()
            if len(raw) < 20:
                raise ValueError('Short GLB header')
            magic, version, length = struct.unpack('<III', raw[:12])
            if magic != 0x46546c67 or version != 2 or length != len(raw):
                raise ValueError('Invalid GLB header')
            chunk_length, chunk_type = struct.unpack('<II', raw[12:20])
            if chunk_type != 0x4e4f534a or 20 + chunk_length > len(raw):
                raise ValueError('Missing GLB JSON chunk')
            document = json.loads(raw[20:20 + chunk_length].decode('utf-8').rstrip(' \x00'))
        else:
            document = json.loads(path.read_text(encoding='utf-8-sig'))
        if document.get('asset', {}).get('version') != '2.0':
            raise ValueError('Unsupported glTF version')
        report.update(containerValid=True, meshCount=len(document.get('meshes', [])),
                      materialCount=len(document.get('materials', [])), imageCount=len(document.get('images', [])))
        for resource in document.get('buffers', []) + document.get('images', []):
            uri = resource.get('uri')
            if not uri or uri.startswith('data:'):
                continue
            parsed = urlsplit(uri)
            if parsed.scheme or parsed.netloc:
                report['externalReferences'].append(uri)
                continue
            target = (path.parent / unquote(parsed.path)).resolve()
            report['externalReferences'].append(uri)
            # No arbitrary path reads outside the containing downloaded pack.
            if not target.is_relative_to((pack_root or path.parent).resolve()) or not target.is_file():
                report['missingLocalReferences'].append(uri)
    except (ValueError, OSError, KeyError, TypeError, UnicodeError, struct.error) as exc:
        report['issues'].append(str(exc))
    return report


def source_family(asset_path):
    """Same-pack alternatives only: identical basename is a clue, never proof of equivalent geometry."""
    parts = Path(asset_path).parts
    for marker in ('Models', 'FBX', 'OBJ', 'glTF', 'GLTF', 'GLB', 'Blend'):
        if marker in parts:
            return '/'.join(parts[:parts.index(marker)])
    return str(Path(asset_path).parent.parent).replace('\\', '/')


def failure_detail(row, catalog, project):
    path = safe_path(project, row['assetPath'])
    detail = row.get('detail', '')
    entry = {'id': row['id'], 'assetPath': row['assetPath'], 'iconPath': row['iconOutputPath'],
             'originalRenderError': detail, 'sourceExists': path.is_file(), 'regenerated': False,
             'alternatives': [], 'priority': 'Manual inspection'}
    if not path.is_file():
        entry.update(classification='Invalid or missing prefab' if path.suffix == '.prefab' else 'Model import failure',
                     evidence='Source file is missing', recovery='Locate the exact original file before rendering.')
    elif path.suffix.lower() in {'.glb', '.gltf'}:
        document = geometry_document(path, project / source_family(row['assetPath']))
        entry['containerInspection'] = document
        if not document['containerValid']:
            classification = 'Model import failure'
        elif document['missingLocalReferences']:
            classification = 'Missing materials/textures'
        else:
            classification = 'Unsupported file format'
        entry.update(classification=classification,
                     evidence='Valid glTF 2 container' if document['containerValid'] else 'Invalid container',
                     recovery='Review a same-pack FBX/OBJ/DAE alternative; keep its own model/icon identity. No image substitution.',
                     priority='Existing format alternative first')
        for alternative in catalog:
            if (Path(alternative['assetPath']).stem.casefold() == path.stem.casefold()
                    and source_family(alternative['assetPath']) == source_family(row['assetPath'])
                    and alternative['assetType'] in {'fbx', 'obj', 'dae', 'prefab'}):
                entry['alternatives'].append({'id': alternative['id'], 'assetPath': alternative['assetPath'],
                                              'iconPath': alternative['iconOutputPath'],
                                              'iconExists': safe_path(project, alternative['iconOutputPath']).is_file(),
                                              'equivalence': 'Same pack/basename; geometry equivalence needs review'})
        entry['evidence'] += '; historical Unity render could not load GameObject. No importer installed by this task.'
    elif path.suffix == '.prefab':
        text = path.read_text(encoding='utf-8-sig')
        blocks = [name for name in ('Camera:', 'MeshRenderer:', 'SkinnedMeshRenderer:', 'MeshFilter:') if name in text]
        entry['serializedComponentEvidence'] = blocks
        if 'Camera:' in blocks and not any(name in blocks for name in ('MeshRenderer:', 'SkinnedMeshRenderer:')):
            entry.update(classification='Duplicate/non-renderable asset', priority='Exclude from item rendering',
                         evidence='Serialized controller camera; no MeshRenderer/SkinnedMeshRenderer blocks',
                         recovery='Remove from future item job selection after owner review; do not manufacture an icon.')
        else:
            entry.update(classification='Unknown, requiring manual inspection', evidence=detail,
                         recovery='Inspect nested/disabled renderer and mesh references when Unity is available.')
    elif 'material' in detail.lower() or 'shader' in detail.lower():
        entry.update(classification='Missing materials/textures', evidence=detail, recovery='Review material references and shader conversion.')
    elif 'bounds' in detail.lower() or 'edge' in detail.lower() or 'framing' in detail.lower():
        entry.update(classification='Model bounds/camera framing', evidence=detail, recovery='Inspect projected bounds before any rerender.')
    else:
        entry.update(classification='Runtime/editor render failure', evidence=detail, recovery='Inspect the editor log at a coordinated Unity session.')
    return entry


def gallery_category(row, failure=None):
    if failure and failure['classification'] == 'Duplicate/non-renderable asset':
        return 'Unclassified', None, 'Serialized camera/controller metadata; not a technology shop item'
    category, shop = GROUPS.get(row['category'], ('Unclassified', None))
    return category, shop, ('Mapped from catalog category and suggestedShopCategory; pack/source paths retained. '
                            'Catalog categories are provisional; no new filename classifier is used in C0.')


def checks_for(metrics, mapping):
    if not metrics['exists']:
        return {}, [], 'Not generated'
    checks = {
        'dataIntegrity': metrics.get('valid', False),
        'nonemptySubject': metrics.get('nonempty', False),
        'transparentBackground': metrics.get('transparentFraction', 0) > .1,
        'resolution': (metrics.get('width'), metrics.get('height')) in {(256, 256), (512, 512)},
        'sourceMapping': mapping,
        'noCanvasEdgeContact': metrics.get('minMargin', 0) >= .02,
    }
    warnings = []
    if metrics.get('maxSubjectExtent', 0) < .50:
        warnings.append('Subject extent below 50% of canvas; inspect at small UI sizes')
    if metrics.get('maxSubjectExtent', 0) > .90:
        warnings.append('Subject extent above 90%; inspect framing')
    if metrics.get('centerOffset', 0) > .12:
        warnings.append('Subject bounding box off center by more than 12% of canvas diagonal units')
    if metrics.get('magentaFraction', 0) > .01:
        warnings.append('Magenta-like subject pixels; inspect shader/material')
    if metrics.get('meanRgb') and max(metrics.get('rgbStddev', [0])) < 4:
        warnings.append('Low subject color variance; verify material visibility')
    if not all(checks.values()):
        return checks, warnings, 'Failed'
    return checks, warnings, 'Needs visual review' if warnings else 'Accepted candidate'


def font(size=12):
    for name in ('C:/Windows/Fonts/segoeui.ttf', 'C:/Windows/Fonts/arial.ttf'):
        if Path(name).is_file():
            return ImageFont.truetype(name, size)
    return ImageFont.load_default()


def sheets(rows, reports):
    folder = reports / 'StageC0ContactSheets'
    folder.mkdir(parents=True, exist_ok=True)
    actual = [row for row in rows if row['image'].get('valid')]
    index = []
    for start in range(0, len(actual), 42):
        subset = actual[start:start + 42]
        page = start // 42 + 1
        columns, tile, height, header = 6, 190, 222, 64
        image = Image.new('RGB', (columns * tile, header + math.ceil(len(subset) / columns) * height), '#f3f5f2')
        draw = ImageDraw.Draw(image)
        draw.text((18, 12), f'NIHONGO LIFE / C0 / {start + 1}-{start + len(subset)} of {len(actual)}', fill='#23372d', font=font(18))
        draw.text((18, 40), 'Actual PNGs, downscaled for inspection. Visual scan is not owner approval.', fill='#4b6355', font=font(12))
        for i, row in enumerate(subset):
            x, y = i % columns * tile, header + i // columns * height
            # Checkerboard is presentation only; original PNG bytes are never changed.
            for xx in range(x + 6, x + tile - 6, 10):
                for yy in range(y + 4, y + 168, 10):
                    draw.rectangle((xx, yy, min(xx + 9, x + tile - 7), min(yy + 9, y + 167)),
                                   fill='#eef0ed' if ((xx - x) // 10 + (yy - y) // 10) % 2 else '#ffffff')
            with Image.open(row['_absoluteIcon']) as icon:
                icon = icon.convert('RGBA')
                icon.thumbnail((162, 162), Image.Resampling.LANCZOS)
                image.paste(icon, (x + (tile - icon.width) // 2, y + (168 - icon.height) // 2), icon)
            label = row['name'][:24]
            draw.text((x + 8, y + 174), f'{start + i + 1:03d}  {label}', fill='#26392f', font=font(12))
            draw.text((x + 8, y + 191), row['assetType'].upper() + ' / ' + row['id'][-10:], fill='#59675d', font=font(10))
            draw.text((x + 8, y + 206), row['category'][:27], fill='#59675d', font=font(10))
            row['contactSheet'] = f'../Reports/StageC0ContactSheets/sheet-{page:02d}.png'
            row['contactSheetCell'] = i + 1
        name = f'sheet-{page:02d}.png'
        image.save(folder / name)
        index.append({'path': 'StageC0ContactSheets/' + name, 'ids': [r['id'] for r in subset]})
    write_json(reports / 'stage_c0_contact_sheets.json', {'pages': index, 'iconCount': len(actual)})
    return index


def runtime_labels(project):
    path = project / 'Assets/NihongoLife/Scripts/Shop/KonbiniCatalog.cs'
    text = path.read_text(encoding='utf-8-sig')
    return {m[0]: {'japanese': m[1], 'price': int(m[2]), 'source': path.relative_to(project).as_posix()}
            for m in re.findall(r'new KonbiniProduct\("([^"]+)",\s*"([^"]*)",\s*"[^"]*",\s*"[^"]*",\s*"[^"]*",\s*(\d+)', text)}


def build(project):
    reports = project / 'Docs/AssetPipeline/Reports'
    preview = project / 'Docs/AssetPipeline/Preview'
    preview.mkdir(parents=True, exist_ok=True)
    load = lambda name: json.loads((reports / name).read_text(encoding='utf-8'))
    catalog = load('asset_catalog.json')['entries']
    jobs = {row['id']: row for row in load('icon_jobs.json')['entries']}
    rendered = {row['id']: row for row in load('render_results.json')['entries']}
    old_metrics = {row['path']: row for row in load('icon_quality.json')['entries']}
    runtime = load('runtime_items.json')['entries']
    labels = runtime_labels(project)
    failure_rows = [failure_detail(row, catalog, project) for row in rendered.values() if row['status'] == 'failed']
    failures = {row['id']: row for row in failure_rows}
    visuals_path = reports / 'stage_c0_visual_review.json'
    visuals = json.loads(visuals_path.read_text(encoding='utf-8')) if visuals_path.is_file() else {'entries': {}}
    hash_groups, pixels_groups = defaultdict(list), defaultdict(list)
    rows = []
    for row in catalog:
        source = safe_path(project, row['assetPath'])
        icon = safe_path(project, row['iconOutputPath'])
        metrics = image_metrics(icon)
        meta = Path(str(source) + '.meta')
        guid = re.search(r'^guid:\s*([0-9a-f]{32})\s*$', meta.read_text(encoding='utf-8'), re.M) if meta.is_file() else None
        job = jobs.get(row['id'])
        render = rendered.get(row['id'])
        old = old_metrics.get(row['iconOutputPath'])
        mapping_checks = {
            'sourceExists': source.is_file(), 'sourceHashMatchesCatalog': source.is_file() and digest(source) == row['sha256'],
            'guidMatchesMetadata': bool(guid and guid.group(1) == row['guid']),
            'nameMatchesId': icon.name == row['id'] + '.png',
            'jobMatchesCatalog': bool(job and job['assetPath'] == row['assetPath'] and job['iconOutputPath'] == row['iconOutputPath'] and job['guid'] == row['guid']),
            'renderMatchesJob': bool(render and render['assetPath'] == row['assetPath'] and render['iconOutputPath'] == row['iconOutputPath']),
            'renderReportedSuccess': bool(render and render['status'] == 'rendered'),
            'iconHashMatchesPreviousAudit': bool(old and old.get('sha256') == metrics.get('sha256')),
        }
        checks, warnings, status = checks_for(metrics, all(mapping_checks.values()))
        if not metrics['exists'] and render and render['status'] == 'failed':
            status = 'Failed'
        category, shop, evidence = gallery_category(row, failures.get(row['id']))
        manual = visuals.get('entries', {}).get(row['id'], {})
        if manual.get('categoryOverride'):
            category, shop = manual['categoryOverride'], manual['shopOverride']
            evidence = manual['evidence']
        warnings.extend(manual.get('warnings', []))
        if metrics.get('valid') and manual.get('warnings') and status == 'Accepted candidate':
            status = 'Needs visual review'
        imported = importer_settings(Path(str(icon) + '.meta')) if metrics['exists'] else None
        linked = [r for r in runtime if r.get('modelReference') == row['assetPath']]
        definition = labels.get(linked[0]['id']) if linked else None
        entry = {
            'id': row['id'], 'name': row['englishDisplayName'], 'assetType': row['assetType'],
            'sourcePath': row['assetPath'], 'iconPath': row['iconOutputPath'],
            'imageUrl': '../../../' + quote(row['iconOutputPath'], safe='/') if metrics.get('valid') else None,
            'category': category, 'shopCategory': shop, 'originalCategory': row['category'], 'categoryEvidence': evidence,
            'categoryConfirmed': False, 'japanese': definition['japanese'] if definition else row.get('japaneseDisplayName'),
            'englishLabelEvidence': 'Source model filename; item display name unapproved',
            'priceYen': definition['price'] if definition else None, 'priceSource': definition['source'] if definition else None,
            'runtimeItemIds': [r['id'] for r in linked], 'variant': row.get('variant'), 'growthStage': row.get('growthStage'),
            'provenance': row['provenance'], 'renderStatus': render['status'] if render else 'not queued',
            'renderDetail': render.get('detail') if render else 'Unclassified candidate; not queued for rendering',
            'image': metrics, 'mappingChecks': mapping_checks, 'automatedChecks': checks,
            'technicalScore': round(100 * sum(checks.values()) / len(checks)) if checks else None,
            'status': status, 'qualityWarnings': warnings, 'importSettings': imported,
            'manualReview': visuals.get('entries', {}).get(row['id'], {'status': 'Not yet visually inspected', 'ownerApproved': False}),
            'materialReview': 'PNG pixel diagnostics only; source texture bindings and URP fidelity require Unity visual review',
            'orientationReview': 'Renderer camera rotation is 24/-32/0 degrees; source mesh orientation is not proven by PNG metrics',
            'failure': failures.get(row['id']), '_absoluteIcon': str(icon),
        }
        rows.append(entry)
        if metrics.get('valid'):
            hash_groups[metrics['sha256']].append(row['id'])
            pixels_groups[metrics['pixelDigest']].append(row['id'])
    for group in pixels_groups.values():
        if len(group) > 1:
            for entry in rows:
                if entry['id'] in group:
                    entry['duplicateIds'] = [identifier for identifier in group if identifier != entry['id']]
    actual_paths = {p.name for p in (project / 'Assets/NihongoLife/Generated/AssetPipeline/Icons').glob('*.png')}
    expected_paths = {Path(r['iconPath']).name for r in rows if r['image']['exists']}
    actual_rows = [r for r in rows if r['image'].get('valid')]
    contact = sheets(rows, reports)
    for entry in rows:
        entry.pop('_absoluteIcon', None)
    summary = {
        'catalogCandidates': len(rows), 'queuedJobs': len(jobs), 'actualGeneratedPngs': len(actual_paths),
        'validGeneratedPngs': len(actual_rows), 'failedRenderJobs': len(failure_rows),
        'notGeneratedCandidates': sum(r['renderStatus'] == 'not queued' for r in rows),
        'orphanPngFiles': sorted(actual_paths - expected_paths),
        'statusCounts': dict(Counter(r['status'] for r in rows)),
        'categoryCounts': {name: sum(r['category'] == name or (name == 'Agriculture' and r['shopCategory'] == 'Agriculture') for r in rows) for name in CATEGORIES},
        'totalPngBytes': sum(r['image']['bytes'] for r in actual_rows),
        'rgbaBytesNoMipmaps': sum(r['image']['decodedRgbaBytes'] for r in actual_rows),
        'rgbaBytesWithFullMipChain': sum(sum(max(1, r['image']['width'] >> level) * max(1, r['image']['height'] >> level) * 4
                                             for level in range(int(math.log2(max(r['image']['width'], r['image']['height']))) + 1)) for r in actual_rows),
        'pngByteDuplicateGroups': [ids for _, ids in sorted(hash_groups.items()) if len(ids) > 1],
        'pixelDuplicateGroups': [ids for _, ids in sorted(pixels_groups.items()) if len(ids) > 1],
        'importIssueCounts': dict(Counter(issue for r in actual_rows for issue in r['importSettings']['issues'])),
        'sourceMappingFailures': [r['id'] for r in actual_rows if not all(r['mappingChecks'].values())],
        'failureClassifications': dict(Counter(r['classification'] for r in failure_rows)),
        'failuresWithExistingAlternateIcon': sum(any(a['iconExists'] for a in f['alternatives']) for f in failure_rows),
        'contactSheetCount': len(contact), 'ownerApprovedCount': 0,
    }
    limits = [
        'Automated candidate acceptance is not visual attractiveness, item semantics, license clearance, or owner approval.',
        'Source mapping checks manifest paths, GUIDs, source/icon hashes and historical render records; it cannot prove which geometry was rasterized.',
        'No Unity launch or regeneration in Stage C0; material bindings and actual source orientation remain review items.',
        'Image occupancy, color variance and magenta diagnostics are review triggers, not proof of material errors.',
        'Existing catalog categories are provisional; controller-camera failures are corrected from serialized component metadata.',
    ]
    write_json(reports / 'stage_c0_quality.json', {'schemaVersion': 1, 'summary': summary, 'criteria': {
        'alphaThreshold': 8, 'minimumTransparentFraction': .10, 'minimumCanvasMargin': .02,
        'subjectExtentReviewRange': [.50, .90], 'centerOffsetReviewThreshold': .12,
        'technicalScore': 'Six binary PNG/mapping checks; import and manual/material review remain separate'},
        'limitations': limits, 'entries': rows})
    write_json(reports / 'stage_c0_failures.json', {'entries': failure_rows, 'counts': summary['failureClassifications'],
               'regenerated': 0, 'note': 'No substitutes or rerenders. Same-pack/basename alternatives are review links, not equivalent-source claims.'})
    write_json(reports / 'stage_c0_categories.json', {'categories': summary['categoryCounts'], 'rules': GROUPS,
               'note': limits[4], 'entries': [{'id': r['id'], 'originalCategory': r['originalCategory'], 'category': r['category'],
                                            'shopCategory': r['shopCategory'], 'evidence': r['categoryEvidence'],
                                            'sourcePath': r['sourcePath'], 'confirmed': r['categoryConfirmed']} for r in rows]})
    payload = {'schemaVersion': 1, 'summary': summary, 'categories': CATEGORIES, 'entries': rows,
               'contactSheets': contact, 'limitations': limits, 'currencyBalance': None}
    (preview / 'gallery-data.js').write_text('window.NIHONGO_ASSETS = ' + json.dumps(payload, ensure_ascii=False, separators=(',', ':')) + ';\n', encoding='utf-8')
    human_report(rows, summary, reports, limits)
    print(json.dumps(summary, ensure_ascii=True, indent=2))


def human_report(rows, summary, reports, limits):
    lines = ['# Stage C0 quality audit', '',
             f"Re-inspected {summary['actualGeneratedPngs']} actual PNGs against {summary['catalogCandidates']} catalog entries and {summary['queuedJobs']} jobs.", '',
             'No Unity session, source edit, image regeneration or owner approval was performed.', '',
             '| Status | Count | Meaning |', '| --- | --- | --- |']
    for status in ('Accepted candidate', 'Needs visual review', 'Failed', 'Not generated'):
        meaning = {'Accepted candidate': 'Passed automated PNG and manifest checks; visual/material/semantic approval pending',
                   'Needs visual review': 'PNG exists, with measured or visually identified review flags',
                   'Failed': 'Render failed, or an existing image failed technical checks',
                   'Not generated': 'Not queued; no substitute icon supplied'}[status]
        lines.append(f"| {status} | {summary['statusCounts'].get(status, 0)} | {meaning} |")
    lines += ['', '## Findings', '',
              f"- {len(summary['sourceMappingFailures'])} existing PNGs fail source mapping checks; {len(summary['orphanPngFiles'])} orphan PNG files.",
              f"- {len(summary['pixelDuplicateGroups'])} groups have identical decoded pixels. Format alternatives often render identically; these are not independent shop items.",
              f"- {summary['failuresWithExistingAlternateIcon']} failed assets have a same-pack/basename alternative with a PNG. Equivalence still needs review.",
              '- The two failed camera-controller prefabs are Unclassified, based on serialized Camera components and absent mesh renderer blocks.',
              '- Crop pack Harvested meshes can show a plant/tree after harvesting; the suffix does not establish a harvested produce item.',
              '- All category labels remain provisional metadata mappings. No new filename guesser or imagined fashion/seed icon was added.',
              '', '## Current import metadata', '']
    for issue, count in sorted(summary['importIssueCounts'].items()):
        lines.append(f'- {count} icons: {issue}.')
    lines += ['', 'The shared `AutoOptimizationProcessor.OnPreprocessTexture` explicitly enables mipmaps and Crunch for Default/other non-Sprite textures. '
              'Default platform settings are inspected separately from legacy root maximum-size fields. '
              'A Crunch flag with textureCompression=0 does not prove effective GPU compression. No import metadata was changed.', '',
              '## Limits', ''] + ['- ' + limit for limit in limits]
    lines += ['', '## Per-asset results', '', '| Asset | Category | Status | Technical score | Review flags |',
              '| --- | --- | --- | --- | --- |']
    for row in rows:
        warnings = '; '.join(row['qualityWarnings']) or (row['renderDetail'] if row['status'] in ('Failed', 'Not generated') else 'Visual/material approval pending')
        lines.append(f"| {row['name'].replace('|', '/')} ({row['id'][-8:]}) | {row['category']} | {row['status']} | {row['technicalScore'] if row['technicalScore'] is not None else '—'} | {warnings} |")
    (reports / 'STAGE_C0_QUALITY.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    build(args.project.resolve())
