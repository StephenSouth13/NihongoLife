"""Read-only ownership/safety checks for the delivery; writes only its own report."""
import json
import subprocess
from pathlib import Path

from audit import digest, write_json


def git(repo, *args):
    return subprocess.check_output(['git', '-C', str(repo), *args])


def verify(project):
    repo = project.parent
    output = project / 'Docs/AssetPipeline/Reports'
    catalog = json.loads((output / 'asset_catalog.json').read_text(encoding='utf-8'))
    icons = project / 'Assets/NihongoLife/Generated/AssetPipeline/Icons'
    render = json.loads((output / 'render_results.json').read_text(encoding='utf-8'))
    indexed = {}
    for record in git(repo, 'ls-files', '-s', '-z').split(b'\0'):
        if not record:
            continue
        header, name = record.split(b'\t', 1)
        indexed[name.decode('utf-8')] = header.split()[1].decode('ascii')
    original_mismatches = []
    original_count = 0
    for row in catalog['entries']:
        name = project.name + '/' + row['assetPath']
        if name not in indexed:
            continue
        original_count += 1
        current = git(repo, 'hash-object', '--', name).decode('ascii').strip()
        if current != indexed[name]:
            original_mismatches.append(row['assetPath'])
    tracked_changes = [p for p in git(repo, 'diff', '--name-only', '-z').decode('utf-8').split('\0') if p]
    staged = [p for p in git(repo, 'diff', '--cached', '--name-only', '--diff-filter=AM', '-z').decode('utf-8').split('\0') if p]
    first = repo / '.utmp/asset-pipeline/first_hashes.json'
    first_hashes = json.loads(first.read_text(encoding='utf-8')) if first.exists() else {}
    current_hashes = {p.name: digest(p) for p in sorted(icons.glob('*.png'))}
    common = sorted(first_hashes.keys() & current_hashes.keys())
    data = {
        'trackedModelInputsChecked': original_count, 'changedTrackedModelInputs': original_mismatches,
        'rendered': sum(r['status'] == 'rendered' for r in render['entries']),
        'failed': sum(r['status'] == 'failed' for r in render['entries']),
        'fatalRenderError': render.get('fatalError'), 'persistedGeneratedPngCount': len(current_hashes),
        'fullRunComparison': {'commonPngs': len(common),
                              'identicalPngs': sum(first_hashes[p] == current_hashes[p] for p in common),
                              'changedPngs': [p for p in common if first_hashes[p] != current_hashes[p]],
                              'note': 'The second batch adds Legacy Diffuse support; common outputs should remain identical.'},
        'trackedWorkspaceChangesOutsidePackage': tracked_changes,
        'stagedExamSourceAdditions': [p for p in staged if '/Docs/Exam/' in p and Path(p).suffix.lower() in {'.pdf', '.mp3', '.wav'}],
        'note': 'Shared worktree: concurrent changes and staged removals preserved. No commits or staging performed by this package.'
    }
    write_json(output / 'delivery_verification.json', data)
    print(json.dumps({k: data[k] for k in ('trackedModelInputsChecked', 'changedTrackedModelInputs', 'rendered', 'failed',
                                         'persistedGeneratedPngCount', 'stagedExamSourceAdditions')}, indent=2))


if __name__ == '__main__':
    verify(Path(__file__).resolve().parents[2])
