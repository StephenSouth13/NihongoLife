"""Inspect PNG thumbnails without changing them. Pillow is an optional local QA dependency."""
import argparse
import json
from collections import defaultdict
from pathlib import Path

from audit import digest, write_json


def inspect(project):
    from PIL import Image
    rows, hashes = [], defaultdict(list)
    roots = ('Assets/NihongoLife/Resources/Items', 'Assets/NihongoLife/Generated/AssetPipeline/Icons')
    for folder in roots:
        for path in sorted((project / folder).glob('*.png')):
            relative = path.relative_to(project).as_posix()
            row = {'path': relative, 'bytes': path.stat().st_size, 'sha256': digest(path), 'issues': []}
            try:
                with Image.open(path) as image:
                    image.load()
                    rgba = image.convert('RGBA')
                    alpha = rgba.getchannel('A')
                    box = alpha.getbbox()
                    row.update(width=image.width, height=image.height, mode=image.mode,
                               alphaExtrema=list(alpha.getextrema()), contentBounds=list(box) if box else None)
                    if not box:
                        row['issues'].append('Empty image')
                    elif box[0] == 0 or box[1] == 0 or box[2] == image.width or box[3] == image.height:
                        row['issues'].append('Content reaches image edge or background is opaque')
                    if folder.endswith('AssetPipeline/Icons') and image.size not in ((256, 256), (512, 512)):
                        row['issues'].append('Unexpected generated resolution')
                    row['decodedRgbaBytes'] = image.width * image.height * 4
            except Exception as exc:
                row['issues'].append(type(exc).__name__)
            rows.append(row)
            hashes[row['sha256']].append(relative)
    return {'entries': rows, 'identicalPngGroups': [v for _, v in sorted(hashes.items()) if len(v) > 1],
            'totalPngBytes': sum(r['bytes'] for r in rows),
            'totalDecodedRgbaBytes': sum(r.get('decodedRgbaBytes', 0) for r in rows)}


def contact_sheet(project):
    from PIL import Image, ImageDraw
    catalog = json.loads((project / 'Docs/AssetPipeline/Reports/asset_catalog.json').read_text(encoding='utf-8'))
    samples = []
    for category in sorted({r['category'] for r in catalog['entries']}):
        matches = [r for r in catalog['entries'] if r['category'] == category and (project / r['iconOutputPath']).is_file()]
        # Include harvested items and stage variants rather than only repeated source format alternatives.
        distinct = {}
        for row in sorted(matches, key=lambda r: (0 if r['variant'] == 'Harvested' else 1, r['assetPath'])):
            distinct.setdefault(Path(row['assetPath']).stem.lower(), row)
        samples.extend(list(distinct.values())[:4])
    if not samples:
        return
    tile, columns = 200, 4
    sheet = Image.new('RGB', (columns * tile, ((len(samples) + columns - 1) // columns) * (tile + 42)), '#edf0f2')
    draw = ImageDraw.Draw(sheet)
    for i, row in enumerate(samples):
        x, y = (i % columns) * tile, (i // columns) * (tile + 42)
        with Image.open(project / row['iconOutputPath']) as icon:
            icon = icon.convert('RGBA')
            icon.thumbnail((tile - 16, tile - 16))
            sheet.paste(icon, (x + (tile - icon.width) // 2, y + (tile - icon.height) // 2), icon)
        draw.text((x + 6, y + tile), row['category'], fill='#18202b')
        draw.text((x + 6, y + tile + 16), Path(row['assetPath']).stem[:27], fill='#18202b')
    sheet.save(project / 'Docs/AssetPipeline/Reports/icon_contact_sheet.png')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    report = inspect(args.project)
    write_json(args.project / 'Docs/AssetPipeline/Reports/icon_quality.json', report)
    contact_sheet(args.project)
    print(f"Inspected {len(report['entries'])} PNGs; {sum(bool(r['issues']) for r in report['entries'])} flagged.")
