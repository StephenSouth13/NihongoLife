"""NihongoLife original background music, synthesised from code (no samples, no third-party audio).

Writes seamless loops to Assets/NihongoLife/Resources/Audio/Music/<key>.ogg (ffmpeg must be on PATH):
  clinic  — Hibari Clinic, quiet waiting room (marimba + pads, F major pentatonic, 76 bpm)
  rain    — Ame no Hi, rainy day in town (lo-fi electric piano, brushes, 68 bpm)
  sick    — Okayu, get well soon (music-box lullaby in 3/4, 60 bpm)
  spring  — Sakura Michi, spring in town (koto + breathy flute, yo scale, 92 bpm)
  work    — Isogashii Shift, busy at work (shamisen riff + taiko, in scale, 112 bpm)
Usage:  python Tools/audio/compose_music.py [key ...]
The music is the project's own work (generated here), listed in Docs/AUDIO_CREDITS.md.
"""
import os
import subprocess
import sys
import tempfile

import numpy as np
from scipy.io import wavfile
from scipy.signal import fftconvolve

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'NihongoLife', 'Resources', 'Audio', 'Music')
rng = np.random.default_rng(7)


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12.0)


def env(n, attack, decay_rate, release=0.03):
    t = np.arange(n) / SR
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    e = a * np.exp(-t * decay_rate)
    r = int(release * SR)
    if r > 0 and n > r:
        e[-r:] *= np.linspace(1, 0, r)
    return e


def partials(f, seconds, parts, attack=0.004, detune=0.0):
    """parts: (ratio, amplitude, decay rate)."""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for ratio, amp, decay in parts:
        ph = rng.random() * 2 * np.pi
        out += amp * np.sin(2 * np.pi * f * ratio * (1 + detune) * t + ph) * env(n, attack, decay)
    return out


# ─── Instruments (frequency, seconds, velocity) ───
def marimba(f, s, v):
    return v * partials(f, s + 0.6, [(1, 1, 4.5), (3.93, 0.22, 16), (9.2, 0.06, 40)], 0.002)


def music_box(f, s, v):
    return v * partials(f, s + 1.6, [(1, 1, 1.8), (2.0, 0.25, 3.5), (3.0, 0.12, 6), (5.4, 0.05, 12)], 0.001)


def epiano(f, s, v):
    n = int((s + 0.8) * SR)
    t = np.arange(n) / SR
    tone = partials(f, s + 0.8, [(1, 1, 1.6), (2, 0.35, 3.0), (3, 0.12, 5.0), (14, 0.02, 30)], 0.003)
    return v * tone * (1 + 0.12 * np.sin(2 * np.pi * 4.5 * t))


def pluck(f, s, v):  # koto-like
    return v * partials(f, s + 1.0, [(1, 1, 2.6), (2, 0.5, 4), (3, 0.3, 6), (4, 0.18, 9), (5, 0.1, 12), (6.02, 0.05, 16)], 0.002)


def shamisen(f, s, v):  # bright, buzzy, short
    return v * partials(f, s + 0.3, [(1, 1, 7), (2, 0.7, 9), (3, 0.55, 12), (4, 0.4, 15), (5, 0.3, 18), (7, 0.2, 25), (9, 0.12, 30)], 0.001)


def flute(f, s, v):  # breathy, vibrato, slow attack
    n = int((s + 0.25) * SR)
    t = np.arange(n) / SR
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * t) * np.clip(t / 0.4, 0, 1)
    phase = 2 * np.pi * f * np.cumsum(vib) / SR
    tone = np.sin(phase) + 0.18 * np.sin(2 * phase) + 0.06 * np.sin(3 * phase)
    breath = np.convolve(rng.standard_normal(n), np.ones(30) / 30, 'same') * 0.12
    a = np.clip(t / 0.09, 0, 1) * np.clip((s + 0.25 - t) / 0.25, 0, 1)
    return v * (tone + breath) * a * 0.8


def pad(f, s, v):
    n = int((s + 1.2) * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for d in (-0.004, 0.0, 0.004):
        for k, amp in ((1, 1), (2, 0.4), (3, 0.18), (4, 0.08)):
            out += amp * np.sin(2 * np.pi * f * k * (1 + d) * t + rng.random() * 6.28)
    a = np.clip(t / 0.8, 0, 1) * np.clip((s + 1.2 - t) / 1.2, 0, 1)
    return v * out * a * 0.22


def bass(f, s, v):
    return v * partials(f, s + 0.2, [(1, 1, 2.2), (2, 0.3, 4)], 0.006)


def kick(_, s, v):
    n = int(0.35 * SR)
    t = np.arange(n) / SR
    f = 50 + 70 * np.exp(-t * 30)
    return v * np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9)


def taiko(_, s, v):
    n = int(0.6 * SR)
    t = np.arange(n) / SR
    f = 70 + 40 * np.exp(-t * 20)
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 6)
    skin = np.convolve(rng.standard_normal(n), np.ones(12) / 12, 'same') * np.exp(-t * 40) * 0.5
    return v * (body + skin)


def brush(_, s, v):
    n = int(0.25 * SR)
    t = np.arange(n) / SR
    noise = rng.standard_normal(n)
    noise = noise - np.convolve(noise, np.ones(6) / 6, 'same')  # brighter
    return v * noise * np.exp(-t * 18) * 0.35


def hat(_, s, v):
    n = int(0.08 * SR)
    t = np.arange(n) / SR
    noise = rng.standard_normal(n)
    noise = noise - np.convolve(noise, np.ones(3) / 3, 'same')
    return v * noise * np.exp(-t * 70) * 0.3


INSTR = dict(marimba=marimba, box=music_box, ep=epiano, pluck=pluck, sham=shamisen, flute=flute, pad=pad, bass=bass,
             kick=kick, taiko=taiko, brush=brush, hat=hat)


def render(events, bpm, beats, extras=None, reverb=0.35, room=2.2):
    """events: (beat, length in beats, midi or None, instrument, velocity, pan -1..1). Three copies are rendered
    and the middle one kept, so the loop point carries the tails of the bar before it (seamless)."""
    spb = 60.0 / bpm
    loop = int(round(beats * spb * SR))
    total = loop * 3 + SR * 4
    left = np.zeros(total)
    right = np.zeros(total)
    sounds = [INSTR[inst](hz(midi) if midi is not None else 0.0, length * spb, vel) for _, length, midi, inst, vel, _ in events]
    for copy in range(3):  # every copy uses the same rendered notes, so the loop point is continuous
        for (beat, length, midi, inst, vel, pan), sound in zip(events, sounds):
            start = int(round((copy * beats + beat) * spb * SR))
            end = min(total, start + len(sound))
            sound = sound[:end - start]
            gl, gr = np.sqrt((1 - pan) / 2), np.sqrt((1 + pan) / 2)
            left[start:end] += sound * gl
            right[start:end] += sound * gr
    if extras is not None:
        e = np.tile(extras(loop), (1, total // loop + 1))[:, :total]  # periodic, like the notes
        left += e[0]
        right += e[1]
    # Reverb: decaying noise impulse, different per side.
    n = int(room * SR)
    t = np.arange(n) / SR
    ir_l = rng.standard_normal(n) * np.exp(-t * 3.2 / room * 2)
    ir_r = rng.standard_normal(n) * np.exp(-t * 3.2 / room * 2)
    ir_l /= np.sqrt(np.sum(ir_l ** 2)); ir_r /= np.sqrt(np.sum(ir_r ** 2))
    wet_l = fftconvolve(left, ir_l)[:total]
    wet_r = fftconvolve(right, ir_r)[:total]
    left = left * (1 - reverb) + wet_l * reverb * 1.4
    right = right * (1 - reverb) + wet_r * reverb * 1.4
    mid = slice(loop, 2 * loop)
    stereo = np.stack([left[mid], right[mid]], axis=1)
    stereo /= max(1e-9, np.max(np.abs(stereo)))
    rms = np.sqrt(np.mean(stereo ** 2))
    stereo *= min(0.89, 0.16 / max(rms, 1e-9))  # about −16 dBFS RMS, peaks under −1 dBFS
    return np.clip(stereo, -1, 1)


def chord(beat, length, notes, inst, vel, spread=0.0):
    return [(beat + i * spread, length, m, inst, vel, (i / max(1, len(notes) - 1) - 0.5) * 0.6) for i, m in enumerate(notes)]


# ─── Pieces ───
def clinic():
    bpm, bars = 76, 16
    prog = [[53, 57, 60, 64], [50, 53, 57, 60], [46, 50, 53, 57], [48, 53, 55, 60]]  # Fmaj7 Dm7 Bbmaj7 Csus
    scale = [65, 67, 69, 72, 74, 77, 79, 81]  # F pentatonic
    ev = []
    for bar in range(bars):
        c = prog[bar % 4]
        b0 = bar * 4
        ev += chord(b0, 4, c, 'pad', 0.55)
        ev.append((b0, 2, c[0] - 12, 'bass', 0.55, 0))
        ev.append((b0 + 2, 2, c[0] - 12 + 7, 'bass', 0.4, 0))
        arp = [c[0] + 12, c[1] + 12, c[2] + 12, c[3] + 12, c[2] + 12, c[1] + 12, c[2] + 12, c[3] + 12]
        for i, m in enumerate(arp):
            ev.append((b0 + i * 0.5, 0.5, m, 'marimba', 0.32 if i % 2 else 0.42, 0.25))
        if bar % 2 == 1:
            phrase = rng.choice(scale, 3)
            for i, m in enumerate(phrase):
                ev.append((b0 + 1 + i, 1.2, int(m) + 12, 'box', 0.22, -0.35))
    return render(ev, bpm, bars * 4, reverb=0.42)


def rain():
    bpm, bars = 68, 16
    prog = [[57, 60, 64, 67, 71], [53, 57, 60, 64], [50, 53, 57, 60, 64], [52, 56, 59, 62]]  # Am9 Fmaj7 Dm9 E7
    melody = [76, 74, 72, 69, 72, 74, 76, 79, 77, 76, 74, 72, 71, 72, 74, 71]
    ev = []
    for bar in range(bars):
        c = prog[bar % 4]
        b0 = bar * 4
        ev += chord(b0 + 0.05, 1.8, c, 'ep', 0.42, spread=0.03)
        ev += chord(b0 + 2.5, 1.4, c[1:], 'ep', 0.3, spread=0.03)
        ev.append((b0, 3, c[0] - 12, 'bass', 0.6, 0))
        ev.append((b0, 1, None, 'kick', 0.55, 0))
        ev.append((b0 + 2.6, 1, None, 'kick', 0.4, 0))
        ev.append((b0 + 1, 1, None, 'brush', 0.5, 0.2))
        ev.append((b0 + 3, 1, None, 'brush', 0.5, 0.2))
        for h in range(4):
            ev.append((b0 + h + 0.66, 0.2, None, 'hat', 0.35, -0.3))
        if bar >= 4:
            m = melody[bar % 16]
            ev.append((b0 + 0.5, 1.5, m, 'ep', 0.34, -0.2))
            ev.append((b0 + 2.25, 1.0, m - 2 if bar % 2 else m + 3, 'ep', 0.26, -0.2))

    def crackle(total):  # soft vinyl dust
        out = np.zeros(total)
        idx = rng.integers(0, total, total // 1800)
        out[idx] = rng.standard_normal(len(idx)) * 0.08
        out = np.convolve(out, np.exp(-np.arange(40) / 6), 'same')
        hiss = np.convolve(rng.standard_normal(total), np.ones(20) / 20, 'same') * 0.004
        return np.stack([out + hiss, np.roll(out, 777) + hiss])
    return render(ev, bpm, bars * 4, crackle, reverb=0.3)


def sick():
    bpm, bars, beats = 60, 16, 3
    prog = [[48, 52, 55], [45, 48, 52], [41, 45, 48], [43, 47, 50]]  # C Am F G
    tune = [72, 76, 79, 76, 74, 72, 74, 76, 72, 69, 72, 74, 76, 74, 71, 72]
    ev = []
    for bar in range(bars):
        c = prog[bar % 4]
        b0 = bar * beats
        ev += chord(b0, 3, [n + 12 for n in c], 'pad', 0.4)
        ev.append((b0, 3, c[0] - 12, 'bass', 0.35, 0))
        m = tune[bar]
        ev.append((b0, 1, m, 'box', 0.5, -0.1))
        ev.append((b0 + 1, 1, c[1] + 24, 'box', 0.28, 0.25))
        ev.append((b0 + 2, 1, c[2] + 24, 'box', 0.28, 0.25))
        if bar % 4 == 3:
            ev.append((b0 + 2.5, 0.5, m + 12, 'box', 0.2, -0.3))
    return render(ev, bpm, bars * beats, reverb=0.5, room=2.8)


def spring():
    bpm, bars = 92, 16
    yo = [62, 64, 67, 69, 71, 74, 76, 79, 81, 83]  # D yo scale (D E G A B)
    prog = [[50, 57, 62], [47, 54, 59], [45, 52, 57], [43, 50, 55]]
    motif = [0, 2, 3, 4, 3, 2, 1, 2]
    ev = []
    for bar in range(bars):
        c = prog[bar % 4]
        b0 = bar * 4
        ev += chord(b0, 4, [n + 12 for n in c], 'pad', 0.35)
        ev.append((b0, 2, c[0] - 12, 'bass', 0.45, 0))
        shift = [0, 1, -1, 2][bar % 4]
        for i, step in enumerate(motif):
            ev.append((b0 + i * 0.5, 0.5, yo[max(0, min(len(yo) - 1, step + shift + 2))], 'pluck', 0.4 if i % 2 == 0 else 0.3, 0.3))
        if bar % 4 in (1, 3):
            ev.append((b0 + 3.5, 0.5, yo[1] + 12, 'pluck', 0.3, -0.4))
        if bar >= 4 and bar % 2 == 0:
            line = [yo[5], yo[6], yo[4], yo[5]] if bar % 4 == 0 else [yo[7], yo[6], yo[5], yo[3]]
            ev.append((b0, 1.5, line[0], 'flute', 0.32, -0.25))
            ev.append((b0 + 1.5, 0.5, line[1], 'flute', 0.28, -0.25))
            ev.append((b0 + 2, 2, line[2], 'flute', 0.3, -0.25))
            ev.append((b0 + 4, 3.5, line[3], 'flute', 0.3, -0.25))
        for h in range(8):
            ev.append((b0 + h * 0.5, 0.2, None, 'hat', 0.18 if h % 2 else 0.1, 0.4))
    return render(ev, bpm, bars * 4, reverb=0.38)


def work():
    bpm, bars = 112, 16
    insc = [52, 53, 57, 59, 60, 64, 65, 69, 71, 72]  # E in scale (E F A B C)
    riff = [0, 2, 3, 2, 4, 3, 2, 1]
    roots = [40, 45, 41, 40]
    ev = []
    for bar in range(bars):
        b0 = bar * 4
        r = roots[bar % 4]
        shift = [0, 2, 1, 0][bar % 4]
        for i, step in enumerate(riff):
            ev.append((b0 + i * 0.5, 0.45, insc[min(len(insc) - 1, step + shift + 2)], 'sham', 0.42 if i % 2 == 0 else 0.3, 0.25))
        ev.append((b0, 1, r, 'bass', 0.6, 0)); ev.append((b0 + 1.5, 0.5, r, 'bass', 0.4, 0))
        ev.append((b0 + 2, 1, r + 7, 'bass', 0.5, 0)); ev.append((b0 + 3, 1, r, 'bass', 0.45, 0))
        ev.append((b0, 1, None, 'taiko', 0.75, -0.1)); ev.append((b0 + 2, 1, None, 'taiko', 0.6, -0.1))
        ev.append((b0 + 3.5, 1, None, 'taiko', 0.45, 0.1))
        ev.append((b0 + 1, 1, None, 'brush', 0.45, 0.2)); ev.append((b0 + 3, 1, None, 'brush', 0.45, 0.2))
        for h in range(8):
            ev.append((b0 + h * 0.5, 0.2, None, 'hat', 0.25 if h % 2 else 0.15, -0.35))
        if bar >= 8 and bar % 2 == 0:
            ev.append((b0, 2, insc[7], 'flute', 0.3, -0.3)); ev.append((b0 + 2, 2, insc[6], 'flute', 0.28, -0.3))
            ev.append((b0 + 4, 3.5, insc[5], 'flute', 0.28, -0.3))
    return render(ev, bpm, bars * 4, reverb=0.25, room=1.6)


PIECES = dict(clinic=clinic, rain=rain, sick=sick, spring=spring, work=work)


def main(keys):
    os.makedirs(OUT, exist_ok=True)
    for key in keys or PIECES:
        audio = PIECES[key]()
        with tempfile.TemporaryDirectory() as tmp:
            wav = os.path.join(tmp, key + '.wav')
            wavfile.write(wav, SR, (audio * 32767).astype(np.int16))
            ogg = os.path.abspath(os.path.join(OUT, key + '.ogg'))
            subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', wav, '-c:a', 'libvorbis', '-q:a', '4', ogg], check=True)
        print(f'{key}: {len(audio) / SR:.1f} s -> {ogg}')


if __name__ == '__main__':
    main(sys.argv[1:])
