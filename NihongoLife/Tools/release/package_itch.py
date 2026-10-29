"""Packs the WebGL build for itch.io and checks its HTML5 limits.

Usage: python Tools/release/package_itch.py [build folder]   (default ../Build/Release/WebGL)
Limits (itch.io HTML5): at most 1,000 entries (folders count), 500 MB extracted, 200 MB per file, paths ≤ 240 chars.
Writes ../Build/Release/NihongoLife_itch_<date>.zip with index.html at the root. Exits 1 if a limit is broken.
"""
import datetime
import os
import sys
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SKIP = {'_headers', '.htaccess', 'vercel.json', 'WebGLBuildSizeReport.txt'}
MB = 1024 * 1024


def main():
    src = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'Build', 'Release', 'WebGL')
    if not os.path.isfile(os.path.join(src, 'index.html')):
        sys.exit('No index.html in ' + src + ' — build the WebGL player first (Docs/ITCH_IO.md).')
    files, folders, total, biggest, longest = [], set(), 0, (0, ''), 0
    for dirpath, _, names in os.walk(src):
        for name in names:
            path = os.path.join(dirpath, name)
            rel = os.path.relpath(path, src).replace('\\', '/')
            if rel in SKIP:
                continue
            size = os.path.getsize(path)
            files.append((path, rel))
            total += size
            biggest = max(biggest, (size, rel))
            longest = max(longest, len(rel))
            parts = rel.split('/')[:-1]
            for i in range(len(parts)):
                folders.add('/'.join(parts[:i + 1]))
    entries = len(files) + len(folders)
    problems = []
    if entries > 1000: problems.append(f'{entries} entries (files + folders) > 1000')
    if total > 500 * MB: problems.append(f'{total / MB:.0f} MB > 500 MB')
    if biggest[0] > 200 * MB: problems.append(f'{biggest[1]} is {biggest[0] / MB:.0f} MB > 200 MB')
    if longest > 240: problems.append(f'a path is {longest} characters > 240')
    print(f'{len(files)} files, {len(folders)} folders ({entries} entries), {total / MB:.1f} MB, biggest {biggest[1]} {biggest[0] / MB:.1f} MB, longest path {longest}')
    if problems:
        print('itch.io limits broken:\n  ' + '\n  '.join(problems))
        sys.exit(1)
    out = os.path.join(os.path.dirname(src.rstrip('\\/')), f'NihongoLife_itch_{datetime.date.today():%Y%m%d}.zip')
    # Build files are already Brotli/compressed: store them; compress the rest.
    with zipfile.ZipFile(out, 'w') as z:
        for path, rel in sorted(files, key=lambda f: f[1]):
            stored = rel.endswith(('.unityweb', '.mp3', '.ogg', '.jpg', '.png')) or rel.startswith('zones/')
            z.write(path, rel, compress_type=zipfile.ZIP_STORED if stored else zipfile.ZIP_DEFLATED)
    print(f'Ready for itch.io: {out} ({os.path.getsize(out) / MB:.1f} MB)')


if __name__ == '__main__':
    main()
