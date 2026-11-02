#!/usr/bin/env python3
"""The music under the product tour: each chapter plays the game's own track for that place, crossfaded at the cuts.
Usage: python tour_music.py <frames_dir> <out.wav>   (frames_dir holds timeline.tsv)
"""
import subprocess
import sys
from pathlib import Path

MUSIC = Path(__file__).resolve().parents[2] / 'Assets' / 'NihongoLife' / 'Resources' / 'Audio' / 'Music'
# The beat each track starts at.
CHAPTERS = [('intro', 'menu_bgm'), ('town', 'city_day'), ('konbini', 'konbini'), ('fashion', 'spring'), ('heat', 'clinic'), ('sushi', 'sushi'), ('school', 'school'),
            ('arcade', 'game_center'), ('music', 'spring'), ('island', 'island'), ('career', 'city_day'), ('online', 'menu_bgm')]
FADE = 2.5


def main():
    frames, out = Path(sys.argv[1]), Path(sys.argv[2])
    beats = dict((b, int(f) / 30.0) for f, b in (l.split('\t') for l in (frames / 'timeline.tsv').read_text().splitlines() if l.strip()))
    total = len(list(frames.glob('f*.jpg'))) / 30.0
    starts = [(beats[b] if i else 0.0, track) for i, (b, track) in enumerate(CHAPTERS)]
    inputs, chain = [], []
    for i, (start, track) in enumerate(starts):
        end = starts[i + 1][0] if i + 1 < len(starts) else total
        length = end - start + FADE
        inputs += ['-stream_loop', '-1', '-t', f'{length:.2f}', '-i', str(MUSIC / f'{track}.ogg')]
        chain.append(f'[{i}:a]aresample=48000,aformat=channel_layouts=stereo[s{i}]')
    last = 's0'
    for i in range(1, len(starts)):
        chain.append(f'[{last}][s{i}]acrossfade=d={FADE}:c1=tri:c2=tri[m{i}]')
        last = f'm{i}'
    subprocess.run(['ffmpeg', '-y', '-v', 'error', *inputs, '-filter_complex', ';'.join(chain), '-map', f'[{last}]', str(out)], check=True)
    print(out, f'{total:.1f}s')


if __name__ == '__main__':
    main()
