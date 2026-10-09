"""Synthetic PNG/container fixtures; never edits delivered assets."""
import tempfile,unittest,json
from pathlib import Path
from PIL import Image,ImageDraw
from stage_c0 import image_metrics,safe_path,geometry_document,checks_for

class C0Tests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)
    def png(self,box=None,opaque=False):
        p=self.root/'test.png';im=Image.new('RGBA',(512,512),(0,0,0,255 if opaque else 0))
        if box: ImageDraw.Draw(im).rectangle(box,fill=(190,140,70,255))
        im.save(p);return image_metrics(p)
    def test_empty_detected(self):
        m=self.png();self.assertTrue(m['valid']);self.assertFalse(m['nonempty'])
    def test_centered_transparent_geometry(self):
        m=self.png((64,64,447,447));self.assertAlmostEqual(m['maxSubjectExtent'],.75);self.assertEqual(m['centerOffset'],0)
    def test_edge_clipping_detected(self):
        m=self.png((0,64,447,447));self.assertEqual(m['minMargin'],0);self.assertIn(False,checks_for(m,True)[0].values())
    def test_opaque_canvas_detected(self):
        self.assertEqual(self.png(opaque=True)['transparentFraction'],0)
    def test_offcenter_measured(self):
        self.assertGreater(self.png((5,5,80,80))['centerOffset'],.12)
    def test_corrupt_png(self):
        p=self.root/'bad.png';p.write_bytes(b'invalid');self.assertFalse(image_metrics(p)['valid'])
    def test_asset_path_escape(self):
        for p in ('Assets/../../secret','../secret','C:/secret','Assets\\secret'):
            with self.assertRaises(ValueError):safe_path(self.root,p)
    def test_missing_gltf_reference(self):
        p=self.root/'mesh.gltf';p.write_text(json.dumps({'asset':{'version':'2.0'},'buffers':[{'uri':'missing.bin'}]}));self.assertEqual(geometry_document(p)['missingLocalReferences'],['missing.bin'])
    def test_invalid_glb_header(self):
        p=self.root/'mesh.glb';p.write_bytes(b'invalid');self.assertFalse(geometry_document(p)['containerValid'])

if __name__=='__main__': unittest.main()
