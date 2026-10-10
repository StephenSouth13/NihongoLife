# -*- coding: utf-8 -*-
"""Helpers to build local IELTS packages (schema nihongolife.ielts.v1) from OCR'd Cambridge pages.

This module holds no exam content. The per-test build scripts that do (passages, questions, keys) live in
NihongoLife/LocalContent/IELTS/_build/ — gitignored with the packages they write — because the material is licensed:
never commit it, never import it into Assets/, never ship it.
"""
import json
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
IELTS = os.path.join(ROOT, "LocalContent", "IELTS")
SOURCES = os.path.join(IELTS, "_sources")

NOISE = re.compile(r"(fb\W{0,2}co|facebook|louis ?quang|lowsquang|ou/5quang|quang ?vo|louis vo|0962004051|ieltsfocus|ielts ?focus|the1elts|giasuielts|zalo|0847705973|ielts qu)", re.I)
RUNNING_HEAD = re.compile(r"^\s*(Test \d|Reading|Listening|Writing|Speaking|\d{1,3})\s*$")


def page_lines(book, pages, cols=False):
    """Lines of the OCR text of the given pages, without watermarks, running heads and page numbers.
    `cols`: read the column-by-column OCR (pNNN.col.txt, ielts_ocr.py --columns) of two-column passages."""
    out = []
    for p in pages:
        with open(os.path.join(SOURCES, book, f"p{p:03d}.col.txt" if cols else f"p{p:03d}.txt"), encoding="utf-8") as fh:
            started = False  # a gap before the first text of a page (under the running head) is not a paragraph break
            for raw in fh:
                line = raw.strip()
                if NOISE.search(line) or RUNNING_HEAD.match(line):
                    continue
                if line == "<col>":  # column turn: a paragraph break only when the column ends like a paragraph
                    if out and out[-1] == "":  # the gap above a page number at the column foot
                        out.pop()
                    _turn(out)
                    started = False  # nor is the gap under a running head at the column top
                    continue
                if not line:  # paragraph break kept from the page layout (one marker at most)
                    if started and out and out[-1] != "":
                        out.append("")
                    continue
                started = True
                out.append(line)
        if out and out[-1] == "":  # the gap above the page number is not a paragraph break either
            out.pop()
        _turn(out)
    return out


def _turn(out):
    """Across a page or column turn a paragraph ends when the last line ends a sentence well short of the column
    width (justified text fills every other line)."""
    if out and out[-1] == "":
        return
    page = [l for l in out[-60:] if l]
    if page and re.search(r"[.?!'’\")]$", page[-1]) and len(page[-1]) < 0.88 * max(len(l) for l in page):
        out.append("")


def between(lines, start, end=None):
    """Lines after the one containing `start` up to (excluding) the one containing `end`."""
    i = next(k for k, l in enumerate(lines) if start in l) + 1
    j = next((k for k in range(i, len(lines)) if end and end in lines[k]), len(lines)) if end else len(lines)
    return lines[i:j]


def paragraphs(lines, fixes=None, labelled=False, short=0.82, starts=(), joins=()):
    """`starts`: line beginnings that open a new paragraph the layout did not reveal (checked on the page image).
    `joins`: line beginnings that continue the paragraph across a page or column turn the layout took for a break."""
    if joins:
        lines = [l for k, l in enumerate(lines) if not (l == "" and k + 1 < len(lines) and any(lines[k + 1].startswith(j) for j in joins))]
    if starts:
        marked = []
        for line in lines:
            if any(line.startswith(s) for s in starts) and marked and marked[-1] != "":
                marked.append("")
            marked.append(line)
        lines = marked
    return _paragraphs(lines, fixes, labelled, short)


def _paragraphs(lines, fixes=None, labelled=False, short=0.82):
    """Rebuilds paragraphs from OCR lines. Paragraph breaks come from the page layout (empty lines kept by ielts_ocr
    for large vertical gaps); labelled passages also split on a lone letter A, B, … (shown in bold). Only when a text
    has no layout breaks at all does a short, sentence-ending line close the paragraph."""
    fixes = fixes or {}
    paras, cur = [], []
    has_breaks = "" in lines
    width = max((len(l) for l in lines), default=80)
    fresh = True  # cur holds the start of a paragraph that began after a layout break
    for line in lines:
        if line == "":
            if cur and not (labelled and len(cur) == 1 and cur[0].startswith("<b>")):
                paras.append(" ".join(cur))
                cur = []
            fresh = True
            continue
        if labelled and re.fullmatch(r"[A-Z]", line):
            # The margin label is often read after the first line of its paragraph: pull that line back in — when it
            # opened a fresh block, or when it starts a sentence right after a line that ended one.
            first = cur[-1] if cur else ""
            starts_sentence = bool(first) and first[0].isupper() and not first.startswith("<b>") and not first.startswith("* ")
            if starts_sentence and ((fresh and len(cur) == 1) or (len(cur) >= 2 and re.search(r"[.?!'’\")]$", cur[-2]))):
                rest = cur[:-1]
                if rest:
                    paras.append(" ".join(rest))
                cur = [f"<b>{line}</b>  ", first]
                fresh = False
                continue
            fresh = False
            if cur:
                paras.append(" ".join(cur))
            cur = [f"<b>{line}</b>  "]
            continue
        cur.append(line)
        if not has_breaks and not labelled and len(line) < width * short and re.search(r"[.?!'’\")]$", line):
            paras.append(" ".join(cur))
            cur = []
    if cur:
        paras.append(" ".join(cur))
    # Footnotes ("* word: …") and source credits repeat at the foot of every page: keep one copy, at the end.
    def is_note(p):
        return p.startswith("* ") or p.startswith("This text is taken") or p.startswith("Source:")
    body = [p for p in paras if not is_note(p)]
    notes = []
    for p in paras:
        if is_note(p) and not any(n[:40] == p[:40] for n in notes):
            notes.append(p)
    paras = body + notes
    text = "\n\n".join(re.sub(r"(?<=\S) {2,}(?=\S)", " ", p) for p in paras)
    text = re.sub(r"(\w+)- (\w+)", _rejoin, text)  # words broken across lines
    for a, b in fixes.items():
        text = text.replace(a, b)
    return text.strip()


AUDIO_DIR = os.path.join(ROOT, "Docs", "Exam", "IELTS", "CAMBRIDGE 11-19-20261009T150315Z-1-001", "CAMBRIDGE 11-19", "AUDIO LISTENING")


def audio(book_number, track, package, end=None, member=None):
    """Copies one Cambridge track (e.g. "C14T2S1") into the package as OGG Vorbis (mono, 32 kHz, q4) → "audio/<track>.ogg".
    `end` (seconds) trims a track that runs on into the next test (checked by listening / speech recognition).
    `member`: the file name inside the zip when the book's tracks are not named like the track id."""
    import subprocess, tempfile, zipfile
    out_dir = os.path.join(IELTS, package, "audio")
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, track + ".ogg")
    if not os.path.exists(out):
        archive = next(os.path.join(AUDIO_DIR, f) for f in os.listdir(AUDIO_DIR) if f.lower() == f"audio cam {book_number}.zip")
        with zipfile.ZipFile(archive) as z, tempfile.TemporaryDirectory() as tmp:
            member = next(n for n in z.namelist() if (os.path.basename(n) == member if member else os.path.basename(n).upper().startswith(track.upper())))
            src = z.extract(member, tmp)
            trim = ["-t", str(end)] if end else []
            subprocess.run(["ffmpeg", "-y", "-v", "error", "-i", src, *trim, "-ac", "1", "-ar", "32000", "-c:a", "libvorbis", "-q:a", "4", out], check=True)
    return f"audio/{track}.ogg"


def picture(book, page, box, package, name):
    """Crops a plan / diagram from the page image (box = fractions l, t, r, b) into the package → "images/<name>.png"."""
    from PIL import Image
    out_dir = os.path.join(IELTS, package, "images")
    os.makedirs(out_dir, exist_ok=True)
    im = Image.open(os.path.join(SOURCES, book, f"p{page:03d}.png")).convert("RGB")
    w, h = im.size
    im.crop((int(box[0] * w), int(box[1] * h), int(box[2] * w), int(box[3] * h))).save(os.path.join(out_dir, name + ".png"))
    return f"images/{name}.png"


def opts(*pairs):
    return [{"letter": l, "text": t} for l, t in pairs]


def items(*pairs):
    return [{"number": n, "text": t} for n, t in pairs]


def write(package, test, key):
    """Writes test.json and key.json and checks that every question 1..40 has exactly one group and one key."""
    numbers = set()
    for part in test["parts"]:
        for g in part["groups"]:
            for n in range(g["from"], g["to"] + 1):
                assert n not in numbers, f"question {n} twice"
                numbers.add(n)
    total = max(numbers)
    assert numbers == set(range(1, total + 1)), f"questions not contiguous: {sorted(set(range(1, total + 1)) - numbers)}"
    keyed = {a["number"] for a in key["answers"]}
    assert keyed == numbers, f"key mismatch: missing {sorted(numbers - keyed)} extra {sorted(keyed - numbers)}"
    for part in test["parts"]:
        for g in part["groups"]:
            if g["type"] == "completion":
                text = " ".join(l.get("text", "") + " " + l.get("label", "") for l in g.get("lines", []))
                for n in range(g["from"], g["to"] + 1):
                    assert "{%d}" % n in text, f"gap {{{n}}} missing in completion group"
    test.setdefault("schema", "nihongolife.ielts.v1")
    test.setdefault("localOnly", True)
    key.setdefault("schema", "nihongolife.ielts.key.v1")
    key.setdefault("testId", test["id"])
    out = os.path.join(IELTS, package)
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, "test.json"), "w", encoding="utf-8") as fh:
        json.dump(test, fh, ensure_ascii=False, indent=2)
    with open(os.path.join(out, "key.json"), "w", encoding="utf-8") as fh:
        json.dump(key, fh, ensure_ascii=False, indent=2)
    print(f"{package}: {total} questions, {len(test['parts'])} parts")


def key_answers(spec):
    """{1: "creativity", "4-5": ["traffic", "crime"] (either order), 6: ["x", "y"] (alternatives)} → key answers."""
    answers = []
    for k, v in spec.items():
        if isinstance(k, str) and "-" in k:  # IN EITHER ORDER pair/triple
            nums = list(range(int(k.split("-")[0]), int(k.split("-")[1]) + 1))
            for n in nums:
                answers.append({"number": n, "accepted": list(v), "set": nums})
        else:
            answers.append({"number": int(k), "accepted": v if isinstance(v, list) else [v]})
    return sorted(answers, key=lambda a: a["number"])


_SPELL = None


def _known(word):
    """A dictionary word (pyspellchecker), British spellings (-ise, -our, -re, -ll-, -ae-) included."""
    global _SPELL
    if _SPELL is None:
        from spellchecker import SpellChecker
        _SPELL = SpellChecker()
    w = word.lower()
    variants = {w, re.sub(r"is(e|ed|es|ing|ation|ations|er|ers)$", r"iz\1", w), re.sub(r"ys(e|ed|es|ing)$", r"yz\1", w),
                w.replace("our", "or"), re.sub(r"tre(s?)$", r"ter\1", w), w.replace("ll", "l"), w.replace("ae", "e"), w.replace("ogue", "og"),
                w.replace("isation", "ization").replace("ise", "ize"), w.replace("ence", "ense")}
    return bool(_SPELL.known(variants))


def _rejoin(m):
    """'cork- strippers' → 'cork-strippers' when both halves are words and the joined form is not;
    'consider- ation' → 'consideration' otherwise."""
    a, b = m.group(1), m.group(2)
    if not _known(a + b) and _known(a) and _known(b):
        return a + "-" + b
    return a + b


def suspects(text):
    """Lower-case words of a built passage that an English dictionary does not know — mostly OCR damage (faded margins,
    broken letters) to fix by hand against the page image. Capitalised names are skipped."""
    words = {w for w in re.findall(r"(?<![\w'’-])[a-z][a-z]+(?![\w'’-])", text)}
    return sorted(w for w in words if not _known(w))
