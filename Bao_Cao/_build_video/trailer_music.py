"""Original trailer music for NihongoLife (procedurally synthesised, no samples, no licence issues).
D 'yo' pentatonic (D E G A B), I–vi–IV–V, 92 BPM: pad + koto-like plucks + bass + soft drums,
ending in a swelling chord and chime for the end card. Output: 44.1 kHz stereo 16-bit WAV."""
import sys
import wave
import numpy as np

SR = 44100
BPM = 92.0
BEAT = 60.0 / BPM
LENGTH = float(sys.argv[2]) if len(sys.argv) > 2 else 68.0
CALM = len(sys.argv) > 3 and sys.argv[3] == 'calm'
rng = np.random.default_rng(7)
N = int(SR * LENGTH)
L = np.zeros(N)
R = np.zeros(N)


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def add(buf_l, buf_r, sig, start, pan=0.0, gain=1.0):
    i = int(start * SR)
    if i >= N:
        return
    sig = sig[: N - i] * gain
    buf_l[i:i + len(sig)] += sig * np.sqrt(0.5 * (1 - pan))
    buf_r[i:i + len(sig)] += sig * np.sqrt(0.5 * (1 + pan))


def envelope(n, a, d, s, r, sustain_len):
    t = np.arange(n) / SR
    env = np.ones(n) * s
    env[t < a] = t[t < a] / a
    dmask = (t >= a) & (t < a + d)
    env[dmask] = 1 - (1 - s) * (t[dmask] - a) / d
    rel = t > sustain_len
    env[rel] = s * np.exp(-(t[rel] - sustain_len) / r)
    return env


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.zeros_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc = (1 - a) * x[i] + a * acc
        y[i] = acc
    return y


def pad(notes, dur):
    n = int(dur * SR)
    t = np.arange(n) / SR
    sig = np.zeros(n)
    for note in notes:
        f = midi(note)
        for det in (-0.12, 0.0, 0.12):
            ff = f * 2 ** (det / 12)
            sig += 2 * ((t * ff) % 1.0) - 1          # saw
    sig = lowpass(sig / (len(notes) * 3), 1400)
    return sig * envelope(n, 0.9, 0.5, 0.8, 1.2, dur - 1.0)


def pluck(note, dur=1.6, bright=0.5):
    f = midi(note)
    period = int(SR / f)
    n = int(dur * SR)
    buf = rng.uniform(-1, 1, period)
    out = np.zeros(n)
    for i in range(n):
        out[i] = buf[i % period]
        buf[i % period] = 0.996 * (bright * buf[i % period] + (1 - bright) * buf[(i + 1) % period])
    out += 0.3 * np.sin(2 * np.pi * f * 2 * np.arange(n) / SR) * np.exp(-np.arange(n) / SR * 6)
    return out * np.exp(-np.arange(n) / SR * 1.6)


def bass(note, dur):
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = midi(note)
    sig = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(2 * np.pi * 2 * f * t)
    return sig * envelope(n, 0.01, 0.2, 0.6, 0.15, dur - 0.15)


def kick():
    n = int(0.35 * SR)
    t = np.arange(n) / SR
    f = 45 + 80 * np.exp(-t * 30)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9)


def hat():
    n = int(0.06 * SR)
    noise = rng.uniform(-1, 1, n)
    noise = noise - lowpass(noise, 6000)
    return noise * np.exp(-np.arange(n) / SR * 70)


def chime(note, dur=4.0):
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = midi(note)
    mod = np.sin(2 * np.pi * f * 3.5 * t) * 2.0 * np.exp(-t * 2)
    return np.sin(2 * np.pi * f * t + mod) * np.exp(-t * 1.2)


# Chords: D, Bm, G, A (one bar = 4 beats each)
chords = [[62, 66, 69, 73], [59, 62, 66, 69], [55, 59, 62, 66], [57, 61, 64, 69]]
roots = [38, 35, 43, 45]
bar = 4 * BEAT
yo = [62, 64, 67, 69, 71, 74, 76, 79, 81]   # D E G A B D' E' G' A'
melody = [4, 5, 6, 5, 4, 2, 3, 4,   2, 3, 4, 3, 2, 0, 1, 2,   4, 5, 7, 6, 5, 4, 2, 3,   4, 3, 2, 1, 2, 3, 4, None]

intro_end = 4 * BEAT * 1.5          # ≈ 3.9 s
main_end = LENGTH - 10.0
t = 0.0
bar_index = 0
while t < main_end:
    c = bar_index % 4
    add(L, R, pad(chords[c], bar + 0.8), t, pan=0.0, gain=0.16)
    if t >= intro_end:
        add(L, R, bass(roots[c], bar * 0.95), t, gain=0.22)
        for b in range(4):
            bt = t + b * BEAT
            if b in (0, 2) and not CALM:
                add(L, R, kick(), bt, gain=0.5)
            for h in range(0 if CALM else 2):
                add(L, R, hat(), bt + h * BEAT / 2, pan=0.35, gain=0.06 if h else 0.09)
        for k in range(8):
            idx = melody[(bar_index * 8 + k) % len(melody)]
            if idx is not None and not (bar_index % 8 == 7 and k > 5):
                add(L, R, pluck(yo[idx], 1.4, 0.45), t + k * BEAT / 2, pan=-0.25, gain=0.2 if CALM else 0.32)
    else:
        for k in range(8):   # gentle arpeggio under the title card
            add(L, R, pluck(chords[c][k % 4] + 12, 1.6, 0.6), t + k * BEAT / 2, pan=0.2, gain=0.22)
    t += bar
    bar_index += 1

# Ending: swell + chimes on the end card
add(L, R, pad([50, 62, 66, 69, 74], 10.0), main_end, gain=0.24)
for k, note in enumerate([74, 78, 81, 86]):
    add(L, R, chime(note, 5.0), main_end + 0.15 + k * 0.32, pan=(-0.4 + 0.27 * k), gain=0.16)

mix = np.stack([L, R], axis=1)
fade_in = int(0.8 * SR)
mix[:fade_in] *= np.linspace(0, 1, fade_in)[:, None]
fade_out = int(4.0 * SR)
mix[-fade_out:] *= np.linspace(1, 0, fade_out)[:, None]
mix /= np.max(np.abs(mix)) * 1.12
data = (mix * 32767).astype(np.int16)
with wave.open(sys.argv[1], 'wb') as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes(data.tobytes())
print('wrote', sys.argv[1], f'{LENGTH:.1f}s')
