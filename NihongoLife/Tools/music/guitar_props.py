"""The guitar gesture props (Resources/Animation/gestures.json): "guitar" held in front of the body while playing and
"guitar_back" worn on the back. Every piece is placed along one neck axis, so the body, neck, headstock and strings join
up (hand-typed numbers left the neck sloping one way and the headstock the other — the guitar looked broken apart).
Both props hang from the hips ("H") and are scaled by the gesture layer to the character.
Run from the Unity project folder:  python Tools/music/guitar_props.py
"""
import json
import math
import os

PATH = os.path.join('Assets', 'NihongoLife', 'Resources', 'Animation', 'gestures.json')

WOOD = '#C0773A'      # top
EDGE = '#8A4F22'      # sides
DARK = '#1B1B1B'      # sound hole
NECK = '#5D4037'
HEAD = '#3E2723'
BRIDGE = '#3B2416'
STRING = '#E8E2D0'
PEG = '#D8C27A'


def r3(v):
    return [round(x, 4) for x in v]


def guitar(centre, angle_deg, facing):
    """Pieces of an acoustic guitar. centre = lower bout centre (prop space, metres before the character scale);
    angle_deg = direction of the neck in the x/y plane (0 = +x, 90 = up); facing = +1 front faces +z, -1 faces -z."""
    a = math.radians(angle_deg)
    u = (math.cos(a), math.sin(a), 0.0)            # along the neck
    cx, cy, cz = centre

    def along(t, depth=0.0):
        return (cx + u[0] * t, cy + u[1] * t, cz + facing * depth)

    neck_rot = [0, 0, round(angle_deg, 2)]         # a cube's x axis turned onto the neck direction
    pieces = []
    # Body: lower bout, upper bout (smaller), the waist between, and a rim on the back side.
    pieces.append({'shape': 'cyl', 'pos': r3(along(0.0)), 'rot': [90, 0, 0], 'size': [0.30, 0.09, 0.30], 'hex': WOOD})
    pieces.append({'shape': 'cyl', 'pos': r3(along(0.15)), 'rot': [90, 0, 0], 'size': [0.23, 0.09, 0.23], 'hex': WOOD})
    pieces.append({'shape': 'cyl', 'pos': r3(along(0.07, -0.006)), 'rot': [90, 0, 0], 'size': [0.22, 0.09, 0.22], 'hex': WOOD})
    pieces.append({'shape': 'cyl', 'pos': r3(along(0.0, -0.012)), 'rot': [90, 0, 0], 'size': [0.315, 0.08, 0.315], 'hex': EDGE})
    pieces.append({'shape': 'cyl', 'pos': r3(along(0.15, -0.012)), 'rot': [90, 0, 0], 'size': [0.245, 0.08, 0.245], 'hex': EDGE})
    # Sound hole and bridge on the top.
    pieces.append({'shape': 'cyl', 'pos': r3(along(0.115, 0.046)), 'rot': [90, 0, 0], 'size': [0.085, 0.004, 0.085], 'hex': DARK})
    pieces.append({'shape': 'cube', 'pos': r3(along(-0.05, 0.047)), 'rot': neck_rot, 'size': [0.025, 0.09, 0.012], 'hex': BRIDGE})
    # Neck from the upper bout's edge, then the headstock with pegs.
    neck_from, neck_to = 0.24, 0.62
    pieces.append({'shape': 'cube', 'pos': r3(along((neck_from + neck_to) / 2, 0.015)), 'rot': neck_rot, 'size': [neck_to - neck_from + 0.04, 0.045, 0.035], 'hex': NECK})
    pieces.append({'shape': 'cube', 'pos': r3(along(neck_to + 0.06, 0.012)), 'rot': neck_rot, 'size': [0.13, 0.07, 0.03], 'hex': HEAD})
    for t in (0.6, 0.66, 0.72):
        for side in (-1, 1):
            p = along(t + 0.02, 0.012)
            n = (-u[1] * side * 0.045, u[0] * side * 0.045, 0.0)
            pieces.append({'shape': 'cyl', 'pos': r3((p[0] + n[0], p[1] + n[1], p[2])), 'rot': [0, 0, round(angle_deg, 2)], 'size': [0.012, 0.03, 0.012], 'hex': PEG})
    # Strings: bridge to headstock, just above the top and the neck.
    for k in (-1.5, -0.5, 0.5, 1.5):
        start, end = -0.05, neck_to + 0.01
        p = along((start + end) / 2, 0.05)
        n = (-u[1] * k * 0.008, u[0] * k * 0.008, 0.0)
        pieces.append({'shape': 'cube', 'pos': r3((p[0] + n[0], p[1] + n[1], p[2])), 'rot': neck_rot, 'size': [end - start, 0.003, 0.003], 'hex': STRING})
    tip = r3(along(0.115, 0.06))
    return pieces, tip


def main():
    data = json.load(open(PATH, encoding='utf-8'))
    props = {p['id']: p for p in data['props']}
    # Playing: body in front of the belly, neck to the player's left, rising 25°.
    front, tip = guitar((0.06, 0.17, 0.26), 155.0, +1)
    props['guitar'] = {'id': 'guitar', 'hand': 'H', 'pos': [0, 0, 0], 'rot': [0, 0, 0], 'pieces': front, 'tip': tip}
    # On the back: body behind the waist, neck up over the left shoulder.
    back, _ = guitar((0.05, 0.3, -0.27), 100.0, -1)
    props['guitar_back'] = {'id': 'guitar_back', 'hand': 'H', 'pos': [0, 0, 0], 'rot': [0, 0, 0], 'pieces': back}
    order = [p['id'] for p in data['props']]
    data['props'] = [props[i] for i in order] + [props[i] for i in ('guitar', 'guitar_back') if i not in order]
    with open(PATH, 'w', encoding='utf-8') as f:
        f.write(json.dumps(data, ensure_ascii=False, separators=(',', ':')))
    print(f'guitar: {len(front)} pieces, guitar_back: {len(back)} pieces')


if __name__ == '__main__':
    main()
