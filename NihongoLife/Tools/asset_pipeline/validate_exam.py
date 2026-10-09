"""Validate future JSON matching ExamDefinition/ExamModels; no runtime or data writes."""
import argparse
import json
import math
from pathlib import Path, PureWindowsPath
from urllib.parse import urlsplit


def number(value, minimum=0, maximum=None, integer=False):
    return (type(value) in ((int,) if integer else (int, float)) and (type(value) is int or math.isfinite(value))
            and value >= minimum and (maximum is None or value <= maximum))


def validate(data, media_root):
    errors = []
    root = Path(media_root).resolve()

    def error(code, location):
        errors.append({'code': code, 'location': location})

    def identifier(value, seen, location):
        if not isinstance(value, str) or not value.strip():
            error('missing_identifier', location)
        elif value in seen:
            error('duplicate_identifier', location)
        else:
            seen.add(value)

    def array(value, location):
        if not isinstance(value, list):
            error('invalid_array', location)
            return []
        return value

    def media(obj, location):
        for key in ('audioPath', 'imagePath', 'videoPath', 'mediaPath'):
            value = obj.get(key)
            if value is None or value == '':
                continue
            if not isinstance(value, str) or '\x00' in value or '\\' in value or ':' in value:
                error('invalid_media_path', location + '.' + key)
                continue
            path = Path(value)
            resolved = (root / path).resolve()
            if path.is_absolute() or PureWindowsPath(value).is_absolute() or not resolved.is_relative_to(root):
                error('invalid_media_path', location + '.' + key)
            elif not resolved.is_file():
                error('missing_audio' if key == 'audioPath' else 'missing_media', location + '.' + key)
        for key in ('audioUrl', 'videoUrl', 'youtubeUrl'):
            if obj.get(key):
                try:
                    parsed = urlsplit(obj[key])
                    valid = parsed.scheme == 'https' and bool(parsed.hostname) and not parsed.username
                except (TypeError, ValueError, AttributeError):
                    valid = False
                if not valid:
                    error('invalid_media_url', location + '.' + key)
                else:
                    error('remote_media_unverified', location + '.' + key)
        if obj.get('audioClip'):
            error('unity_audio_reference_unresolved', location + '.audioClip')
        for key in ('mediaStartSeconds', 'mediaDurationSeconds', 'mediaAspectRatio'):
            if key in obj and not number(obj[key]):
                error('invalid_media_metadata', location + '.' + key)

    if not isinstance(data, dict):
        return [{'code': 'invalid_document', 'location': '$'}]
    identifier(data.get('id'), set(), '$.id')
    family = data.get('examType')
    if type(family) is not int or family not in (0, 1):
        error('unsupported_exam_type', '$.examType')
    sections = array(data.get('sections'), '$.sections')
    if not sections:
        error('missing_sections', '$.sections')
    seen_sections, seen_questions = set(), set()
    previous = -1
    total_scale = 0
    for i, section in enumerate(sections):
        loc = f'$.sections[{i}]'
        if not isinstance(section, dict):
            error('invalid_section', loc)
            continue
        identifier(section.get('id'), seen_sections, loc + '.id')
        kind = section.get('type')
        allowed = (0, 1, 2) if family == 0 else (3, 4, 5, 6)
        if type(kind) is not int or kind not in allowed:
            error('unsupported_section_type', loc + '.type')
        elif kind <= previous:
            error('incorrect_section_order', loc + '.type')
        else:
            previous = kind
        for key in ('scoreScaleMax', 'timeLimitSeconds'):
            if not number(section.get(key), 1 if key == 'scoreScaleMax' else 0, integer=True):
                error('invalid_score_metadata' if key == 'scoreScaleMax' else 'invalid_time_limit', loc + '.' + key)
        if number(section.get('scoreScaleMax'), 1, integer=True):
            total_scale += section['scoreScaleMax']
            if family == 0 and number(data.get('jlptSectionPassScore')) and data['jlptSectionPassScore'] > section['scoreScaleMax']:
                error('invalid_score_metadata', '$.jlptSectionPassScore')
        passages = array(section.get('passages', []), loc + '.passages')
        seen_passages = set()
        for j, passage in enumerate(passages):
            ploc = f'{loc}.passages[{j}]'
            if not isinstance(passage, dict):
                error('invalid_passage', ploc)
                continue
            identifier(passage.get('id'), seen_passages, ploc + '.id')
            media(passage, ploc)
        media(section, loc)
        questions = array(section.get('questions'), loc + '.questions')
        if not questions:
            error('missing_questions', loc + '.questions')
        for j, question in enumerate(questions):
            qloc = f'{loc}.questions[{j}]'
            if not isinstance(question, dict):
                error('invalid_question', qloc)
                continue
            identifier(question.get('id'), seen_questions, qloc + '.id')
            qtype = question.get('type')
            if type(qtype) is not int or qtype not in range(5):
                error('unsupported_question_type', qloc + '.type')
            elif qtype in (0, 1):
                choices = array(question.get('choices'), qloc + '.choices')
                index = question.get('correctChoiceIndex')
                if not number(index, 0, len(choices) - 1, integer=True):
                    error('missing_answer_key', qloc + '.correctChoiceIndex')
                if len(choices) < 2 or any(not isinstance(c, dict) for c in choices) or (qtype == 1 and len(choices) != 3):
                    error('invalid_choices', qloc + '.choices')
            elif qtype == 2:
                answers = array(question.get('acceptedAnswers'), qloc + '.acceptedAnswers')
                if not answers or any(not isinstance(a, str) or not a.strip() for a in answers):
                    error('missing_answer_key', qloc + '.acceptedAnswers')
            # Essay and speaking are runtime-supported subjective types: no fabricated binary answer key.
            if not number(question.get('points'), 1, integer=True):
                error('invalid_score_metadata', qloc + '.points')
            reference = question.get('passageId')
            if reference and (not isinstance(reference, str) or reference not in seen_passages):
                error('missing_passage', qloc + '.passageId')
            media(question, qloc)
        if kind in (2, 3):
            media_objects = [section] + [p for p in passages if isinstance(p, dict)] + [q for q in questions if isinstance(q, dict)]
            if not any(any(o.get(k) for k in ('audioPath', 'audioUrl', 'audioClip', 'videoPath', 'videoUrl', 'youtubeUrl')) for o in media_objects):
                error('missing_listening_audio', loc)
    for key in ('recommendedBandMin', 'recommendedBandMax'):
        if key in data and not number(data[key], 0, 9):
            error('invalid_score_metadata', '$.' + key)
    if all(number(data.get(k), 0, 9) for k in ('recommendedBandMin', 'recommendedBandMax')):
        if data['recommendedBandMin'] > data['recommendedBandMax']:
            error('invalid_score_metadata', '$.recommendedBandMax')
    if family == 0:
        for key in ('jlptTotalPassScore', 'jlptSectionPassScore'):
            if not number(data.get(key), 1 if key == 'jlptTotalPassScore' else 0, integer=True):
                error('invalid_score_metadata', '$.' + key)
        if number(data.get('jlptTotalPassScore')) and data['jlptTotalPassScore'] > total_scale:
            error('invalid_score_metadata', '$.jlptTotalPassScore')
    return errors


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('dataset', type=Path)
    parser.add_argument('--media-root', type=Path, required=True)
    args = parser.parse_args()
    try:
        issues = validate(json.loads(args.dataset.read_text(encoding='utf-8-sig')), args.media_root)
    except (ValueError, OSError):
        issues = [{'code': 'unreadable_or_malformed_json', 'location': '$'}]
    print(json.dumps({'valid': not issues, 'issues': issues}, indent=2))
    raise SystemExit(1 if issues else 0)
