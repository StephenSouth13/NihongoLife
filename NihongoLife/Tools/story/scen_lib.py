# -*- coding: utf-8 -*-
"""Small builder for ScenarioDefinition assets (Edu Standard). Nodes are added with N(), choices with C()."""
import json
import re

SCRIPT_GUID = "3ba8f435ab5427145b206cd2a4f4ea7c"


def q(s):
    return json.dumps(s, ensure_ascii=False)


class Scenario:
    def __init__(self):
        self.nodes = []

    def C(self, ja, vi, nxt, cat=None, val=0, reason="", g=(), v=(), flags=()):
        return dict(ja=ja, vi=vi, nxt=nxt, mods=([(cat, val, reason)] if cat else []), g=list(g), v=list(v), flags=list(flags))

    def N(self, id, sp, ja, rd, rom, vi, cue="", nxt="", obj="", choices=None, type=0, npc="", area="", item="", cond="", jump=""):
        self.nodes.append(dict(id=id, sp=sp, ja=ja, rd=rd, rom=rom, vi=vi, cue=cue, nxt=nxt, obj=obj,
                               choices=choices or [], type=type, npc=npc, area=area, item=item, cond=cond, jump=jump))

    def B(self, id, cond, jump, nxt):
        """Branch node: goes to `jump` when the flag condition holds, else to `nxt`."""
        self.N(id, None, "", "", "", "", "", nxt, type=7, cond=cond, jump=jump)

    def validate(self, start, objectives):
        ids = {n["id"]: n for n in self.nodes}
        problems = []
        reach = set()
        stack = [start]
        while stack:
            cur = stack.pop()
            if cur in reach:
                continue
            if cur not in ids:
                problems.append("dangling: " + cur)
                continue
            reach.add(cur)
            n = ids[cur]
            if n["nxt"]:
                stack.append(n["nxt"])
            if n["jump"]:
                stack.append(n["jump"])
            for c in n["choices"]:
                stack.append(c["nxt"])
        for i in ids:
            if i not in reach:
                problems.append("unreachable: " + i)
        completed = {n["obj"] for n in self.nodes if n["obj"]}
        for o in objectives:
            if o[0] not in completed and not o[3]:
                problems.append("objective never completed: " + o[0])
        for n in self.nodes:
            if n["type"] == 0 and n["ja"] and (not n["rd"] or not n["rom"] or not n["vi"]):
                problems.append("missing reading/romaji/vi: " + n["id"])
            if n["type"] == 7 and (not n["jump"] or not n["nxt"] or not n["cond"]):
                problems.append("bad branch: " + n["id"])
            if n["type"] == 0 and not n["choices"] and not n["nxt"]:
                problems.append("dead end: " + n["id"])
        return problems

    def write(self, path, *, name, sid, title_ja, title_vi, desc_ja, desc_vi, chapter, targets, objectives, start):
        w_lines = []
        w = w_lines.append
        w("%YAML 1.1")
        w("%TAG !u! tag:unity3d.com,2011:")
        w("--- !u!114 &11400000")
        w("MonoBehaviour:")
        for l in ["  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
                  "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
                  "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % SCRIPT_GUID, "  m_Name: " + name,
                  "  m_EditorClassIdentifier: NihongoLife::NihongoLife.Scenario.ScenarioDefinition",
                  "  id: " + sid, "  version: 2"]:
            w(l)
        w("  titleJa: " + q(title_ja))
        w("  titleEn: " + q(title_vi))
        w("  descriptionJa: " + q(desc_ja))
        w("  descriptionEn: " + q(desc_vi))
        w("  chapterIndex: %d" % chapter)
        w("  learningTargets:")
        for t in targets:
            w("  - " + t)
        w("  objectives:")
        for o in objectives:
            w("  - id: " + o[0])
            w("    titleJa: " + q(o[1]))
            w("    titleEn: " + q(o[2]))
            w("    isOptional: %d" % (1 if o[3] else 0))
        w("  nodes:")
        for n in self.nodes:
            w("  - id: " + n["id"])
            w("    nodeType: %d" % n["type"])
            w("    nextNodeId: " + n["nxt"])
            w("    objectiveIdToComplete: " + n["obj"])
            sp = n["sp"]
            w("    speakerName: " + (sp[0] if sp else ""))
            w("    speakerId: " + (sp[1] if sp else ""))
            w("    textJa: " + (q(n["ja"]) if n["ja"] else ""))
            w("    textReading: " + (q(n["rd"]) if n["rd"] else ""))
            w("    textEn: " + (q(n["vi"]) if n["vi"] else ""))
            w("    textRomaji: " + (q(n["rom"]) if n["rom"] else ""))
            w("    textEnglishIpa: ")
            w("    animationCue: " + n["cue"])
            w("    voiceClip: {fileID: 0}")
            if n["choices"]:
                w("    choices:")
                for c in n["choices"]:
                    w("    - textJa: " + q(c["ja"]))
                    w("      textEn: " + q(c["vi"]))
                    w("      nextNodeId: " + c["nxt"])
                    if c["mods"]:
                        w("      scoreModifiers:")
                        for m in c["mods"]:
                            w("      - category: " + m[0])
                            w("        value: %d" % m[1])
                            w("        reason: " + q(m[2]))
                    else:
                        w("      scoreModifiers: []")
                    if c["g"]:
                        w("      grammarTags:")
                        for t in c["g"]:
                            w("      - " + t)
                    else:
                        w("      grammarTags: []")
                    if c["v"]:
                        w("      vocabularyTags:")
                        for t in c["v"]:
                            w("      - " + t)
                    else:
                        w("      vocabularyTags: []")
                    if c["flags"]:
                        w("      setFlags:")
                        for t in c["flags"]:
                            w("      - " + t)
                    else:
                        w("      setFlags: []")
            else:
                w("    choices: []")
            w("    flagCondition: " + (q(n["cond"]) if n["cond"] else ""))
            w("    flagJumpNodeId: " + n["jump"])
            w("    targetItemId: " + n["item"])
            w("    targetNpcId: " + n["npc"])
            w("    targetAreaId: " + n["area"])
        w("  startNodeId: " + start)
        open(path, "w", encoding="utf-8", newline="\n").write("\n".join(w_lines) + "\n")
