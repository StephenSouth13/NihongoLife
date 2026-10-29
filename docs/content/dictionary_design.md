# Handbook word-lookup — design + data (P4)

## Data
- Source (authored by hand, ORIGINAL): `tools/content/p4/src/*.txt`, one concept per line: `en|vi|ru|ja|reading|pos` (`#domain X` switches the domain).
- Builder: `python -X utf8 tools/content/p4/build_dictionary.py` → `NihongoLife/Assets/NihongoLife/Resources/Content/Dictionary/`
  - `entries.json` — concept records `{id,en,vi,ru,ja,reading,pos,domains[]}` (2234)
  - `ru-vi, vi-ru, ru-ja, ja-ru, en-vi, vi-en, ja-vi, vi-ja, en-ru, ru-en, ja-en, en-ja .json` — `{schema,from,to,count,headwords,entries:[{k,v:[{id,t,p,d[],r?}]}]}`; `k` = headword (synonym-split, parenthesis removed; for ja also the kana reading), `t` = translation text, `p` = pos, `d` = domains, `r` = reading of the ja side.
  - `index.json` — counts per domain / pos, file sizes.
- Counts: 2234 entries — daily 890, restaurant 354, medical 302, travel 268, bank 184, school 90, shopping 75, work 72. pos: n 1644, v 267, phr 136, adj 109, adv 26, num 16, interj 15, pron 14, conj 7. 0 validator errors.
- Validator rules: 6 fields, Cyrillic in ru, no Cyrillic in vi/ja, kana-only reading, kanji ⇒ reading present, unique `en` key (same ja ⇒ merged domains), ja homographs listed as warnings (31, expected).

## Runtime design (for the Unity executor)
- Lazy: load ONLY the pair file the user opens (≈210–450 KB, `Resources.Load<TextAsset>("Content/Dictionary/ru-vi")` + `JsonUtility`). Unload with `Resources.UnloadAsset` when leaving the screen.
- Build `Dictionary<string,List<Hit>>` from `entries[]` on load; for prefix search keep the `entries` list (already sorted case-insensitively by `k`) and binary-search with `StringComparison.OrdinalIgnoreCase`.
- Normalise the query: `Trim`, lower-case, NFC; for Japanese also fold katakana→hiragana so the kana reading finds the word.
- UI: language pair dropdown (from/to), domain filter chips (`d`), result card = headword, reading (`r`), pos tag, translation(s), "show in 3 other languages" via `entries.json` + `id`.
- Search latency: 2.3k keys → linear scan is fine; no index needed.

## Licence & attribution (external sources considered — NOT used)
| Source | Licence | Decision |
|---|---|---|
| JMdict / EDRDG (ja↔en, some vi/ru glosses) | CC BY-SA 4.0 (EDRDG licence) | Not used. If ever imported: attribution "This product uses the JMdict dictionary file, © EDRDG" in Credits + the derived file must stay CC BY-SA. |
| Wiktionary (via kaikki.org extracts) | CC BY-SA 4.0 + GFDL | Not used. Share-alike and per-entry attribution required. |
| Tatoeba (example sentences) | CC BY 2.0 FR (some CC0) | Not used. Needs author attribution. |
| OpenCorpora (ru) | CC BY-SA 3.0 | Not used. |
| Anything scraped from commercial dictionaries (Mazii, Jisho UI, Google/Yandex) | proprietary | FORBIDDEN. |
The shipped data is original text authored for NihongoLife — no third-party licence obligations. Needs a native proofread (vi/ru/ja) before public release.

## Known limits / TODO
- 2234 headline entries cover the five requested domains, but no inflections, no example sentences, no pitch accent, no furigana for compounds beyond the reading column.
- Some ru entries carry gender ambiguity (e.g. "I'm pregnant" given in feminine form, "потерял(а)" patterns) — intentional.