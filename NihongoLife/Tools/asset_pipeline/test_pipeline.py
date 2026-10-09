"""Synthetic metadata only. No copyrighted question content in fixtures."""
import copy
import json
import tempfile
import unittest
from pathlib import Path

from audit import asset_catalog, classify, runtime_inventory, write_json
from validate_exam import validate


def dataset():
    return {'id': 'synthetic', 'examType': 0, 'jlptTotalPassScore': 40, 'jlptSectionPassScore': 19,
            'sections': [{'id': 's1', 'type': 0, 'scoreScaleMax': 60, 'timeLimitSeconds': 60,
                          'passages': [], 'questions': [{'id': 'q1', 'type': 0, 'points': 1,
                                                       'choices': [{}, {}], 'correctChoiceIndex': 0}]}]}


class ValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.data = dataset()
        self.section = self.data['sections'][0]
        self.question = self.section['questions'][0]

    def codes(self):
        return {e['code'] for e in validate(self.data, self.root)}

    def test_valid_objective_dataset(self):
        self.assertEqual(set(), self.codes())

    def test_identifiers_are_required_and_globally_unique(self):
        self.section['questions'].append(copy.deepcopy(self.question))
        self.assertIn('duplicate_identifier', self.codes())
        self.question['id'] = ''
        self.assertIn('missing_identifier', self.codes())

    def test_missing_and_out_of_range_keys(self):
        self.question.pop('correctChoiceIndex')
        self.assertIn('missing_answer_key', self.codes())
        self.question['correctChoiceIndex'] = 2
        self.assertIn('missing_answer_key', self.codes())

    def test_fill_blank_requires_nonempty_accepted_answers(self):
        self.question.update(type=2, acceptedAnswers=[' '])
        self.assertIn('missing_answer_key', self.codes())

    def test_malformed_choice_objects(self):
        self.question['choices'] = [None, 'bad']
        self.assertIn('invalid_choices', self.codes())

    def test_subjective_questions_do_not_need_binary_keys(self):
        self.question.update(type=3)
        self.question.pop('correctChoiceIndex')
        self.assertEqual(set(), self.codes())

    def test_unsupported_question_types(self):
        for value in (5, 'MultipleChoice', True, None, {}):
            self.question['type'] = value
            self.assertIn('unsupported_question_type', self.codes())

    def test_section_order_and_family(self):
        next_section = copy.deepcopy(self.section)
        next_section.update(id='s2', type=0)
        next_section['questions'][0]['id'] = 'q2'
        self.data['sections'].append(next_section)
        self.assertIn('incorrect_section_order', self.codes())
        next_section['type'] = 6
        self.assertIn('unsupported_section_type', self.codes())

    def test_media_safety_and_missing_audio(self):
        for path in ('../escape.wav', '/absolute.wav', 'C:/secret.wav', 'a\\b.wav', 'a\x00.wav'):
            self.question['audioPath'] = path
            self.assertIn('invalid_media_path', self.codes())
        self.question['audioPath'] = 'missing.wav'
        self.assertIn('missing_audio', self.codes())
        (self.root / 'present.wav').write_bytes(b'local fixture')
        self.question['audioPath'] = 'present.wav'
        self.assertEqual(set(), self.codes())

    def test_listening_requires_media_and_valid_passage(self):
        self.section['type'] = 2
        self.assertIn('missing_listening_audio', self.codes())
        self.question['passageId'] = 'unknown'
        self.assertIn('missing_passage', self.codes())

    def test_remote_urls_are_never_fetched_or_assumed_available(self):
        self.question['audioUrl'] = 'https://example.invalid/a.wav'
        self.assertIn('remote_media_unverified', self.codes())
        self.question['audioUrl'] = 'file:///private.wav'
        self.assertIn('invalid_media_url', self.codes())

    def test_invalid_scores_including_booleans_and_nonfinite(self):
        for value in (-1, 0, True, float('nan'), float('inf'), '1'):
            self.question['points'] = value
            self.assertIn('invalid_score_metadata', self.codes())
        self.question['points'] = 1
        self.data['jlptTotalPassScore'] = 100
        self.assertIn('invalid_score_metadata', self.codes())

    def test_malformed_shapes_return_issues(self):
        for data in (None, [], {'sections': None}, {'id': 'a', 'sections': [None]},
                     {'id': 'a', 'sections': [{'id': 's', 'questions': [None], 'passages': [None]}]}):
            self.assertTrue(validate(data, self.root))


class AuditTests(unittest.TestCase):
    def test_categories_do_not_confuse_stool_with_tool(self):
        self.assertEqual('Unclassified', classify('stool', 'Assets/stool.fbx'))
        self.assertEqual('Technology', classify('computerKeyboard', 'Assets/a.fbx'))
        self.assertEqual('Crops', classify('Carrot_4', 'Assets/a.fbx'))
        self.assertEqual('Unclassified', classify('Drink', 'Assets/Animations/Food/Drink.fbx'))
        self.assertEqual('Unclassified', classify('cabinetTelevision', 'Assets/cabinetTelevision.fbx'))
        self.assertEqual('ShopItems', classify('egg-cup', 'Assets/egg-cup.fbx'))

    def test_catalog_guid_identity_determinism_and_original_safety(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            folder = root / 'Assets/Example'
            folder.mkdir(parents=True)
            model = folder / 'Carrot_2.fbx'
            model.write_bytes(b'geometry-fixture')
            meta = Path(str(model) + '.meta')
            meta.write_text('guid: ' + 'a' * 32 + '\n')
            before = model.read_bytes()
            rows = asset_catalog(root)
            self.assertEqual(rows, asset_catalog(root))
            self.assertEqual(2, rows[0]['growthStage'])
            self.assertTrue(rows[0]['id'].endswith('a' * 32))
            self.assertIsNone(rows[0]['provenance']['license'])
            self.assertEqual(before, model.read_bytes())

    def test_json_serialization_is_reproducible_utf8(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'a.json'
            value = {'b': '日本語', 'a': 1}
            write_json(path, value)
            before = path.read_bytes()
            write_json(path, value)
            self.assertEqual(before, path.read_bytes())
            self.assertEqual(value, json.loads(path.read_text(encoding='utf-8')))


if __name__ == '__main__':
    unittest.main()
