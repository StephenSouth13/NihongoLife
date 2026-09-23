# -*- coding: utf-8 -*-
"""Builder for ExamDefinition assets (JLPT / IELTS practice tests).

Mirrors Tools/story/scen_lib.py: build the data with plain Python calls, then write() emits
Unity YAML directly (LF-only, matching every other hand-authored asset in this project).
Adding a new exam is: write a new gen_*.py using this library, run it, done — no Editor step.
"""
import json

SCRIPT_GUID = "f5214dc098a14546900e76a0b9a4682c"  # ExamModels.cs (defines ExamDefinition)

EXAM_TYPE = {"jlpt": 0, "ielts": 1}
SECTION_TYPE = {
    "jlpt_vocab": 0, "jlpt_grammar_reading": 1, "jlpt_listening": 2,
    "ielts_listening": 3, "ielts_reading": 4, "ielts_writing": 5, "ielts_speaking": 6,
}
QUESTION_TYPE = {"mc": 0, "tfng": 1, "fill": 2, "essay": 3, "speaking": 4}


def q(s):
    return json.dumps(s or "", ensure_ascii=False)


class Choice:
    def __init__(self, vi, en, ja=""):
        self.vi, self.en, self.ja = vi, en, ja


class Question:
    def __init__(self, id, type, prompt_vi="", prompt_en="", prompt_ja="", prompt_reading="",
                 passage_id="", choices=None, correct=-1, accepted=None,
                 task_vi="", task_en="", min_words=0, time_limit=0,
                 explanation_vi="", explanation_en="", tags=None, points=1):
        self.id = id
        self.type = QUESTION_TYPE[type]
        self.prompt_vi, self.prompt_en, self.prompt_ja, self.prompt_reading = prompt_vi, prompt_en, prompt_ja, prompt_reading
        self.passage_id = passage_id
        self.choices = choices or []
        self.correct = correct
        self.accepted = accepted or []
        self.task_vi, self.task_en = task_vi, task_en
        self.min_words = min_words
        self.time_limit = time_limit
        self.explanation_vi, self.explanation_en = explanation_vi, explanation_en
        self.tags = tags or []
        self.points = points


class Passage:
    def __init__(self, id, title_vi="", title_en="", title_ja="",
                 body_vi="", body_en="", body_ja="", body_reading="", max_plays=0):
        self.id = id
        self.title_vi, self.title_en, self.title_ja = title_vi, title_en, title_ja
        self.body_vi, self.body_en, self.body_ja, self.body_reading = body_vi, body_en, body_ja, body_reading
        self.max_plays = max_plays


class Section:
    def __init__(self, id, type, title_vi, title_en, title_ja="", time_limit_seconds=0, score_scale_max=60,
                 passages=None, questions=None):
        self.id = id
        self.type = SECTION_TYPE[type]
        self.title_vi, self.title_en, self.title_ja = title_vi, title_en, title_ja
        self.time_limit_seconds = time_limit_seconds
        self.score_scale_max = score_scale_max
        self.passages = passages or []
        self.questions = questions or []


class Exam:
    def __init__(self, id, exam_type, level, title_vi, title_en, title_ja="",
                 description_vi="", description_en="", sections=None,
                 jlpt_total_pass=80, jlpt_section_pass=19):
        self.id = id
        self.exam_type = EXAM_TYPE[exam_type]
        self.level = level
        self.title_vi, self.title_en, self.title_ja = title_vi, title_en, title_ja
        self.description_vi, self.description_en = description_vi, description_en
        self.sections = sections or []
        self.jlpt_total_pass = jlpt_total_pass
        self.jlpt_section_pass = jlpt_section_pass

    def write(self, path, name):
        w_lines = []
        w = w_lines.append
        w("%YAML 1.1")
        w("%TAG !u! tag:unity3d.com,2011:")
        w("--- !u!114 &11400000")
        w("MonoBehaviour:")
        for l in ["  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
                  "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
                  "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % SCRIPT_GUID, "  m_Name: " + name,
                  "  m_EditorClassIdentifier: NihongoLife::NihongoLife.Exam.ExamDefinition"]:
            w(l)
        w("  id: " + self.id)
        w("  examType: %d" % self.exam_type)
        w("  level: " + q(self.level))
        w("  titleVi: " + q(self.title_vi))
        w("  titleEn: " + q(self.title_en))
        w("  titleJa: " + q(self.title_ja))
        w("  descriptionVi: " + q(self.description_vi))
        w("  descriptionEn: " + q(self.description_en))
        w("  sections:")
        for s in self.sections:
            w("  - id: " + s.id)
            w("    type: %d" % s.type)
            w("    titleVi: " + q(s.title_vi))
            w("    titleEn: " + q(s.title_en))
            w("    titleJa: " + q(s.title_ja))
            w("    timeLimitSeconds: %d" % s.time_limit_seconds)
            w("    scoreScaleMax: %d" % s.score_scale_max)
            if s.passages:
                w("    passages:")
                for p in s.passages:
                    w("    - id: " + p.id)
                    w("      titleVi: " + q(p.title_vi))
                    w("      titleEn: " + q(p.title_en))
                    w("      titleJa: " + q(p.title_ja))
                    w("      bodyVi: " + q(p.body_vi))
                    w("      bodyEn: " + q(p.body_en))
                    w("      bodyJa: " + q(p.body_ja))
                    w("      bodyReading: " + q(p.body_reading))
                    w("      maxPlays: %d" % p.max_plays)
            else:
                w("    passages: []")
            if s.questions:
                w("    questions:")
                for qn in s.questions:
                    w("    - id: " + qn.id)
                    w("      type: %d" % qn.type)
                    w("      passageId: " + qn.passage_id)
                    w("      promptVi: " + q(qn.prompt_vi))
                    w("      promptEn: " + q(qn.prompt_en))
                    w("      promptJa: " + q(qn.prompt_ja))
                    w("      promptReading: " + q(qn.prompt_reading))
                    if qn.choices:
                        w("      choices:")
                        for c in qn.choices:
                            w("      - textVi: " + q(c.vi))
                            w("        textEn: " + q(c.en))
                            w("        textJa: " + q(c.ja))
                    else:
                        w("      choices: []")
                    w("      correctChoiceIndex: %d" % qn.correct)
                    if qn.accepted:
                        w("      acceptedAnswers:")
                        for a in qn.accepted:
                            w("      - " + q(a))
                    else:
                        w("      acceptedAnswers: []")
                    w("      taskInstructionsVi: " + q(qn.task_vi))
                    w("      taskInstructionsEn: " + q(qn.task_en))
                    w("      minWords: %d" % qn.min_words)
                    w("      timeLimitSeconds: %d" % qn.time_limit)
                    w("      explanationVi: " + q(qn.explanation_vi))
                    w("      explanationEn: " + q(qn.explanation_en))
                    if qn.tags:
                        w("      tags:")
                        for t in qn.tags:
                            w("      - " + q(t))
                    else:
                        w("      tags: []")
                    w("      points: %d" % qn.points)
            else:
                w("    questions: []")
        w("  jlptTotalPassScore: %d" % self.jlpt_total_pass)
        w("  jlptSectionPassScore: %d" % self.jlpt_section_pass)
        open(path, "w", encoding="utf-8", newline="\n").write("\n".join(w_lines) + "\n")
