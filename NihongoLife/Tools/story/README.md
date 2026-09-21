# Tools/story

Python scripts that generate the story quests (`Assets/NihongoLife/Resources/Scenarios/*.asset`) from written content
and validate the graph (dangling links, unreachable nodes, objectives that never complete, missing reading/romaji/translation).

- `scen_lib.py`: builder (`N` nodes, `C` choices with tags and story flags, `B` flag-branch nodes) and asset writer.
- `gen_<quest>.py`: one quest each. Run: `python gen_house1.py <output .asset path>`.
- `quest_meta.py`: quest metadata (type, giver, rewards, unlock rules, co-op). Run it after generating so the metadata is re-applied.
  Its paths are absolute for the original machine: edit `B` at the top before running elsewhere.

After generating, you may keep editing the assets directly in Unity. Do not do both: regenerating overwrites hand edits.
Not part of the game build (plain Python, no Unity menu, no editor commands).
