"""Local-only deterministic asset and exam metadata audit. Never writes source assets."""
import argparse
import hashlib
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

MODEL_EXTENSIONS = {'.fbx', '.obj', '.dae', '.gltf', '.glb', '.prefab'}
OUTPUT_ROOT = 'Assets/NihongoLife/Generated/AssetPipeline/Icons'
CATEGORY_RULES = [
    ('Seeds', r'\bseed'),
    ('FarmingTools', r'\b(hoe|rake|shovel|watering|pitchfork|sickle|scythe|axe)\b'),
    ('AnimalProducts', r'\b(egg|milk|wool)\b'),
    ('FarmAnimals', r'\b(cow|chicken|sheep|pig|goat|horse|duck|bull|rooster|hen)\b'),
    ('FarmStructures', r'\b(barn|silo|greenhouse|coop|farm|fence)\b'),
    ('Fashion', r'\b(shirt|dress|hat|shoe|boot|jacket|trousers|pants|glasses)\b'),
    ('Technology', r'\b(laptop|computer|keyboard|mouse|phone|tablet|monitor|camera|television|tv)\b'),
    ('Crops', r'\b(crop|wheat|rice|corn|carrot|tomato|pumpkin|lettuce|cabbage|potato|beet|radish|onion|apple|orange|watermelon|mushroom|strawberry|banana|grape|pepper|eggplant)\b'),
    ('ShopItems', r'\b(food|fruit|vegetable|bread|candy|popsicle|skewer|snack|sushi|drink|bottle|can|burger|sandwich)\b'),
]
SHOP_CATEGORY = {'Seeds': 'Agriculture', 'FarmingTools': 'Agriculture', 'AnimalProducts': 'Agriculture',
                 'FarmAnimals': 'Agriculture', 'FarmStructures': 'Agriculture', 'Crops': 'Agriculture',
                 'Fashion': 'Fashion', 'Technology': 'Technology'}


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + '\n', encoding='utf-8')


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def classify(stem, asset_path):
    words = re.sub(r'([a-z])([A-Z])', r'\1 \2', stem).replace('_', ' ').replace('-', ' ').lower()
    if any(part.lower() in {'animation', 'animations'} for part in Path(asset_path).parts):
        return 'Unclassified'
    if re.search(r'\bcabinet\b', words):
        return 'Unclassified'
    if re.search(r'\begg\s+cup\b', words):
        return 'ShopItems'
    for category, pattern in CATEGORY_RULES:
        if re.search(pattern, words):
            return category
    # Pack names provide a suggestion only; never assert that a generic numbered mesh is a crop.
    if 'crops pack' in asset_path.lower():
        return 'Crops'
    return 'Unclassified'


def source_license(path, assets):
    for parent in path.parents:
        if parent == assets.parent:
            break
        licenses = sorted(p for p in parent.iterdir() if p.is_file() and
                          'license' in p.name.lower() and p.suffix.lower() in {'.txt', '.md'})
        if licenses:
            evidence = licenses[0]
            text = evidence.read_text(encoding='utf-8', errors='replace')
            return {'license': 'CC0 1.0' if 'CC0 1.0' in text else 'Review local license',
                    'licenseEvidence': evidence.relative_to(assets.parent).as_posix(),
                    'source': 'Quaternius' if 'quaternius' in text.lower() else
                              'Kenney' if 'kenney' in text.lower() else None,
                    'scope': 'Nearest ancestor license; review applicability before distribution'}
        if parent == assets:
            break
    return {'license': None, 'licenseEvidence': None, 'source': None, 'scope': 'Unverified'}


def asset_catalog(project):
    assets = project / 'Assets'
    rows = []
    for path in sorted(assets.rglob('*')):
        if not path.is_file() or path.suffix.lower() not in MODEL_EXTENSIONS:
            continue
        relative = path.relative_to(project).as_posix()
        category = classify(path.stem, relative)
        if category == 'Unclassified' and 'Midori Island' not in relative:
            continue
        meta = Path(str(path) + '.meta')
        match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(encoding='utf-8'), re.M) if meta.exists() else None
        guid = match.group(1) if match else ''
        slug = re.sub(r'[^a-z0-9]+', '_', path.stem.lower()).strip('_') or 'asset'
        stable = guid or hashlib.sha256(relative.encode('utf-8')).hexdigest()[:32]
        identifier = slug + '_' + stable
        variant = re.search(r'_(\d+|Harvested|Crop)$', path.stem, re.I)
        missing = ['japaneseDisplayName', 'vietnameseDisplayName', 'semanticItemMapping']
        if not guid:
            missing.append('unityGuid')
        license_info = source_license(path, assets)
        if not license_info['license']:
            missing.append('license')
        rows.append({'id': identifier, 'assetPath': relative, 'assetType': path.suffix[1:].lower(),
                     'guid': guid, 'modelReference': relative, 'variant': variant.group(1) if variant else '',
                     'growthStage': int(variant.group(1)) if category == 'Crops' and variant and variant.group(1).isdigit() else None,
                     'category': category, 'suggestedShopCategory': SHOP_CATEGORY.get(category),
                     'classificationEvidence': 'Filename/pack-name suggestion; exact geometry semantics require review',
                     'iconOutputPath': f'{OUTPUT_ROOT}/{identifier}.png',
                     'japaneseDisplayName': None, 'englishDisplayName': path.stem,
                     'englishNameStatus': 'Source filename; semantic review required',
                     'vietnameseDisplayName': None, 'missingFields': missing,
                     'bytes': path.stat().st_size, 'sha256': digest(path), 'provenance': license_info})
    return rows


def runtime_inventory(project):
    scripts = project / 'Assets/NihongoLife/Scripts'
    ids = defaultdict(set)
    for path in sorted(scripts.rglob('*.cs')):
        text = path.read_text(encoding='utf-8-sig')
        for match in re.finditer(r'(?:AddItem|new\s+KonbiniProduct)\s*\(\s*"([^"]+)"', text):
            ids[match.group(1)].add(path.relative_to(project).as_posix())
    catalog = (scripts / 'Shop/KonbiniCatalog.cs').read_text(encoding='utf-8-sig')
    labels = {m.group(1): (m.group(2), m.group(3)) for m in re.finditer(
        r'new KonbiniProduct\("([^"]+)",\s*"([^"]*)",\s*"[^"]*",\s*"([^"]*)"', catalog)}
    renderer = (scripts / 'Editor/ItemIconRenderer.cs').read_text(encoding='utf-8-sig')
    food_root = re.search(r'const string Food = "([^"]+)"', renderer)
    existing_map = {m.group(1): food_root.group(1) + m.group(2) + '.fbx'
                    for m in re.finditer(r'\("([^"]+)",\s*"([^"]+)"\)', renderer)} if food_root else {}
    return [{'id': key, 'definitionSources': sorted(sources),
             'japaneseDisplayName': labels.get(key, (None, None))[0],
             'vietnameseDisplayName': labels.get(key, (None, None))[1],
             'translationSource': 'Existing KonbiniCatalog; transcribed without independent verification' if key in labels else None,
             'iconPath': f'Assets/NihongoLife/Resources/Items/{"onigiri_sake" if key == "onigiri" else key}.png',
             'iconAvailable': (project / f'Assets/NihongoLife/Resources/Items/{"onigiri_sake" if key == "onigiri" else key}.png').exists(),
             'modelReference': existing_map.get(key),
             'modelAvailable': (project / existing_map[key]).is_file() if key in existing_map else None,
             'mappingStatus': 'Existing ItemIconRenderer map; output kept untouched' if key in existing_map else 'Claude review required'}
            for key, sources in sorted(ids.items())]


def exam_inventory(project):
    try:
        import fitz
    except ImportError:
        fitz = None
    root = project / 'Docs/Exam'
    paths = sorted(p for p in root.rglob('*') if p.is_file())
    audio = [p.relative_to(project).as_posix() for p in paths if p.suffix.lower() in {'.wav', '.mp3', '.ogg', '.m4a'}]
    rows = []
    for path in paths:
        relative = path.relative_to(project).as_posix()
        volume = re.search(r'(?:Cambridge IELTS|Vocabulary Cambridge IELTS)\s*(\d+)', path.stem, re.I)
        row = {'path': relative, 'examFamily': 'IELTS' if '/IELTS/' in relative else 'JLPT' if '/JLPT/' in relative else 'Notes',
               'volume': volume.group(1) if volume else None, 'book': path.stem,
               'tests': [], 'skills': [], 'format': path.suffix[1:].lower(), 'bytes': path.stat().st_size,
               'audioAvailability': 'No local audio files' if not audio else 'Local audio exists; association unverified',
               'answerKeyAvailability': 'Unknown', 'parseability': 'Not assessed', 'issues': [],
               'distribution': 'Copyrighted local reference; permission unverified; exclude source content from deliverables'}
        session = re.search(r'(N[1-5])[-_](?:T)?(\d{1,2})[-_](\d{4})', path.stem, re.I)
        if session:
            row['level'] = session.group(1).upper()
            row['sessionMonth'] = int(session.group(2))
            row['sessionYear'] = int(session.group(3))
            row['tests'] = [f'{row["level"]}_{row["sessionYear"]}_{row["sessionMonth"]:02d}']
            row['sessionEvidence'] = 'Filename; document identity unverified'
        if path.suffix.lower() == '.pdf':
            if fitz is None:
                row['issues'].append('Optional PyMuPDF unavailable')
            else:
                try:
                    with fitz.open(path) as doc:
                        row['pages'] = len(doc)
                        if doc.needs_pass:
                            row['parseability'] = 'Encrypted; password required'
                        else:
                            lengths, tests, skills, key_hits = [], set(row['tests']), set(), 0
                            for page in doc:
                                text = page.get_text()
                                lengths.append(len(text.strip()))
                                tests.update(re.findall(r'\bTest\s+([1-4])\b', text, re.I))
                                skills.update(s.lower() for s in re.findall(r'\b(Listening|Reading|Writing|Speaking)\b', text, re.I))
                                key_hits += bool(re.search(r'answer\s*key|answers?\s+and\s+scripts?|解答|正解', text, re.I))
                            row['tests'] = sorted(tests)
                            row['skills'] = sorted(skills)
                            row['textPages'] = sum(n >= 40 for n in lengths)
                            row['parseability'] = 'Text extractable; structure unverified' if all(n >= 40 for n in lengths) else 'Mixed/scanned pages; OCR or manual review required'
                            row['answerKeyAvailability'] = 'Heading detected; completeness unverified' if key_hits else 'No heading detected; unknown'
                            row['evidence'] = 'Keyword detection across local pages; no question text retained'
                            if row['textPages'] != len(doc):
                                row['issues'].append('Some pages have little/no extractable text')
                except Exception as exc:
                    row['parseability'] = 'Malformed or unreadable'
                    row['issues'].append(type(exc).__name__)
        elif path.suffix.lower() == '.json':
            try:
                json.loads(path.read_text(encoding='utf-8-sig'))
                row['parseability'] = 'Valid JSON syntax; run dataset validator'
            except (ValueError, OSError):
                row['parseability'] = 'Malformed JSON'
        else:
            row['parseability'] = 'Local notes/documentation'
        rows.append(row)
    return {'schemaVersion': 1, 'files': rows, 'audioFiles': audio,
            'limitations': ['Filename/heading metadata does not prove complete tests or answer keys.',
                            'Technical parseability does not establish distribution rights.',
                            'No network calls, uploads, protected question fixtures, or source document writes.']}


def run(project, output):
    models = asset_catalog(project)
    items = runtime_inventory(project)
    hashes, names, guids = defaultdict(list), defaultdict(list), defaultdict(list)
    for row in models:
        hashes[row['sha256']].append(row['assetPath'])
        names[Path(row['assetPath']).stem.lower()].append(row['assetPath'])
        if row['guid']:
            guids[row['guid']].append(row['assetPath'])
    write_json(output / 'asset_catalog.json', {'schemaVersion': 1, 'entries': models})
    write_json(output / 'runtime_items.json', {'schemaVersion': 1, 'entries': items,
               'limitations': 'Static literal AddItem and KonbiniProduct extraction; dynamic IDs require review.'})
    write_json(output / 'icon_jobs.json', {'schemaVersion': 1, 'resolution': 512, 'margin': 0.10,
               'entries': [{'id': r['id'], 'assetPath': r['assetPath'], 'guid': r['guid'],
                            'iconOutputPath': r['iconOutputPath']} for r in models if r['category'] != 'Unclassified']})
    write_json(output / 'asset_report.json', {
        'modelCount': len(models), 'categoryCounts': dict(sorted(Counter(r['category'] for r in models).items())),
        'totalSourceBytes': sum(r['bytes'] for r in models),
        'largestAssets': sorted([{'path': r['assetPath'], 'bytes': r['bytes']} for r in models], key=lambda r: (-r['bytes'], r['path']))[:25],
        'identicalFileGroups': [v for _, v in sorted(hashes.items()) if len(v) > 1],
        'sameNameGroups': [v for _, v in sorted(names.items()) if len(v) > 1],
        'duplicateGuidGroups': [v for _, v in sorted(guids.items()) if len(v) > 1],
        'missingCategories': sorted(set(SHOP_CATEGORY) - {r['category'] for r in models}),
        'missingRuntimeIcons': [r['id'] for r in items if not r['iconAvailable']],
        'unrenderedIcons': [r['id'] for r in models if not (project / r['iconOutputPath']).exists()],
        'unqueuedCandidates': [r['id'] for r in models if r['category'] == 'Unclassified'],
        'missingQueuedIcons': [r['id'] for r in models if r['category'] != 'Unclassified' and not (project / r['iconOutputPath']).exists()],
        'unverifiedLicenseCount': sum(r['provenance']['license'] is None for r in models),
        'limitations': 'Category and numeric stage suggestions come from filenames; review prefab/model duplicates and exact item semantics.'})
    write_json(output / 'exam_inventory.json', exam_inventory(project))
    print(f'Audited {len(models)} models/prefabs and {len(items)} literal runtime item IDs.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    run(args.project.resolve(), args.output or args.project / 'Docs/AssetPipeline/Reports')
