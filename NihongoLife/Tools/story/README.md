# Tools/story

Python scripts that generate the story quests (`Assets/NihongoLife/Resources/Scenarios/*.asset`) from written content
and validate the graph (dangling links, unreachable nodes, objectives that never complete, missing reading/romaji/translation).

- `scen_lib.py`: builder (`N` nodes, `C` choices with tags and story flags, `B` flag-branch nodes) and asset writer.
- `gen_<quest>.py`: one quest each. Run: `python gen_house1.py <output .asset path>`.
- `quest_meta.py`: quest metadata (type, giver, rewards, unlock rules, co-op). Run it after generating so the metadata is re-applied.
  Its paths are absolute for the original machine: edit `B` at the top before running elsewhere.

After generating, you may keep editing the assets directly in Unity. Do not do both: regenerating overwrites hand edits.
Not part of the game build (plain Python, no Unity menu, no editor commands).

## Languages (2026-10-20)

Each asset holds Vietnamese (`titleVi`, `textVi`, `descriptionVi`), English (`titleEn`, `textEn`, `descriptionEn`) and
Japanese. The game shows only the chosen interface language (`Loc`); the Japanese line is the learning target.
`gen_*.py` still writes the Vietnamese into the `...En` keys, so after regenerating a quest run
`python i18n/apply_en.py <asset>`: it moves the Vietnamese to the `...Vi` keys and adds the English from
`i18n/en/<quest>.json`. Keep that file in step when you change a quest's Vietnamese lines.
