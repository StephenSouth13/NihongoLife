"""Turns recorded frames + timeline.tsv into a finished MP4: music bed, per-beat narration (ducked music),
optional burned-in subtitles. Usage:
  python mix_video.py <frames_dir> <tts_dir> <music.wav> <out.mp4> [--lead 0.4] [--mvol 0.5] [--subs] [--skip id,id]
<tts_dir> holds <id>.mp3 and lines.tsv (id\ttext); a beat id without an mp3 is silent."""
import argparse
import subprocess
from pathlib import Path

ap = argparse.ArgumentParser()
ap.add_argument("frames"); ap.add_argument("tts"); ap.add_argument("music"); ap.add_argument("out")
ap.add_argument("--lead", type=float, default=0.4)
ap.add_argument("--mvol", type=float, default=0.5)
ap.add_argument("--subs", action="store_true")
ap.add_argument("--skip", default="")
a = ap.parse_args()

frames = Path(a.frames); tts = Path(a.tts)
count = len(list(frames.glob("f*.jpg")))
length = count / 30.0
beats = [(int(f) / 30.0, i) for f, i in (l.split("\t") for l in (frames / "timeline.tsv").read_text().split("\n") if l.strip())]
lines = dict(l.split("\t", 1) for l in (tts / "lines.tsv").read_text(encoding="utf-8").split("\n") if "\t" in l)
skip = set(filter(None, a.skip.split(",")))


def dur(p):
    return float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(p)]).decode().strip())


voices = [(t + a.lead, i, tts / f"{i}.mp3") for t, i in beats if i not in skip and (tts / f"{i}.mp3").exists()]
work = Path(a.out).with_suffix(".work"); work.mkdir(exist_ok=True)

# Voice track: every line delayed to its beat, summed.
inputs, chains = [], []
for k, (t, i, p) in enumerate(voices):
    inputs += ["-i", str(p)]
    ms = int(t * 1000)
    chains.append(f"[{k}:a]aresample=48000,aformat=channel_layouts=stereo,adelay={ms}|{ms}[v{k}]")
mix = "".join(f"[v{k}]" for k in range(len(voices)))
fc = ";".join(chains) + f";{mix}amix=inputs={len(voices)}:normalize=0,apad,atrim=0:{length:.3f}[vo]"
subprocess.run(["ffmpeg", "-y", "-v", "error", *inputs, "-filter_complex", fc, "-map", "[vo]", str(work / "voice.wav")], check=True)

# Music ducked under the voice, then summed with it.
fc = (f"[0:a]atrim=0:{length:.3f},volume={a.mvol},afade=t=out:st={max(0, length - 3):.3f}:d=3[m];"
      f"[1:a]asplit=2[vo1][vo2];[m][vo1]sidechaincompress=threshold=0.03:ratio=6:attack=40:release=500[duck];"
      f"[duck][vo2]amix=inputs=2:normalize=0,alimiter=limit=0.95,loudnorm=I=-15:TP=-1.5:LRA=11,aresample=48000[a]")
subprocess.run(["ffmpeg", "-y", "-v", "error", "-i", a.music, "-i", str(work / "voice.wav"), "-filter_complex", fc, "-map", "[a]", str(work / "mix.wav")], check=True)

vf = "format=yuv420p"
if a.subs:
    def ts(x):
        h = int(x // 3600); m = int(x % 3600 // 60); s = x % 60
        return f"{h}:{m:02d}:{s:05.2f}"
    ass = ["[Script Info]", "ScriptType: v4.00+", "PlayResX: 1920", "PlayResY: 1080", "",
           "[V4+ Styles]",
           "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding",
           "Style: Sub,Segoe UI,40,&H00FFFFFF,&H00FFFFFF,&H00000000,&H90000000,0,0,0,0,100,100,0,0,3,10,0,2,240,240,150,1",
           "", "[Events]", "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text"]
    for t, i, p in voices:
        text = lines.get(i, "").strip()
        if text:
            ass.append(f"Dialogue: 0,{ts(t)},{ts(t + dur(p) + 0.3)},Sub,,0,0,0,,{text}")
    (work / "subs.ass").write_text("\n".join(ass), encoding="utf-8-sig")
    vf = "subtitles=subs.ass," + vf

subprocess.run(["ffmpeg", "-y", "-v", "error", "-framerate", "30", "-i", str(frames / "f%05d.jpg"), "-i", "mix.wav",
                "-vf", vf, "-c:v", "libx264", "-preset", "slow", "-crf", "19", "-c:a", "aac", "-b:a", "192k",
                "-movflags", "+faststart", "-shortest", str(Path(a.out).resolve())], check=True, cwd=work)
print(f"{a.out}: {length:.1f}s, {len(voices)} voice lines")
