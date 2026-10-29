using UnityEngine;

namespace NihongoLife.Gestures
{
    public sealed partial class GestureLayer
    {
        private void Pose(float dt)
        {
            var frame = transform;
            Vector3 right = frame.right, up = frame.up, fwd = frame.forward;

            // 1. channel values and influences
            for (int i = 0; i < Count; i++)
            {
                var c = _ch[i];
                var def = _cur != null ? _cur.Channel(Names[i]) : null;
                float targetInf = def != null ? _w : 0f;
                c.inf = dt > 0f ? Mathf.SmoothDamp(c.inf, targetInf, ref c.infVel, 0.07f, Mathf.Infinity, dt) : targetInf;
                if (c.inf < 0.0005f && targetInf <= 0f) { c.inf = 0f; c.infVel = 0f; }
                if (def != null)
                {
                    var goal = def.Eval(_t, _cur.dur, _cur.loop && !_cur.hold);
                    if (_xf < _xfDur) goal = Vector3.Lerp(c.from, goal, Smooth(_xf / _xfDur));
                    if (i == RH || i == LH) goal += _pulse[i];
                    float smooth = i <= LH ? 0.045f : 0.06f;
                    c.value = dt > 0f ? Vector3.SmoothDamp(c.value, goal, ref c.vel, smooth, Mathf.Infinity, dt) : goal;
                }
            }
            for (int i = 0; i < 2; i++) _pulse[i] = Vector3.Lerp(_pulse[i], Vector3.zero, 1f - Mathf.Exp(-14f * dt));

            // 2. what the animator did
            Vector3[] animHand = { _arm[2].position, _arm[5].position };
            Vector3[] animFoot = { _leg[2].position, _leg[5].position };
            Quaternion[] animFootRot = { _leg[2].rotation, _leg[5].rotation };

            // 3. hips, spine, head
            var hp = _ch[HP]; var hr = _ch[HR]; var sp = _ch[SP]; var hd = _ch[HD];
            if (hp.inf > 0f) _hips.position += (right * hp.value.x + up * hp.value.y + fwd * hp.value.z) * (_legLen * hp.inf);
            if (hr.inf > 0f) RotateAbout(_hips, Euler(hr.value * hr.inf, right, up, fwd), _hips.position);
            if (sp.inf > 0f)
            {
                var e = sp.value * sp.inf;
                if (_upperChest != null && _chest != null) { Part(_spine, e, 0.3f, right, up, fwd); Part(_chest, e, 0.4f, right, up, fwd); Part(_upperChest, e, 0.3f, right, up, fwd); }
                else if (_chest != null) { Part(_spine, e, 0.45f, right, up, fwd); Part(_chest, e, 0.55f, right, up, fwd); }
                else Part(_spine, e, 1f, right, up, fwd);
            }
            if (hd.inf > 0f)
            {
                var e = hd.value * hd.inf;
                if (_neck != null) { Part(_neck, e, 0.35f, right, up, fwd); Part(_head, e, 0.65f, right, up, fwd); }
                else Part(_head, e, 1f, right, up, fwd);
            }

            // 4. arms
            for (int s = 0; s < 2; s++)
            {
                var c = _ch[s];
                var upper = _arm[s * 3]; var lower = _arm[s * 3 + 1]; var hand = _arm[s * 3 + 2];
                float side = s == 0 ? 1f : -1f;
                Vector3 shoulder = upper.position;
                Vector3 d = animHand[s] - shoulder;
                var animRel = new Vector3(Vector3.Dot(d, right) * side, Vector3.Dot(d, up), Vector3.Dot(d, fwd)) / Mathf.Max(0.01f, _armLen);
                if (c.inf < 0.02f) { c.value = animRel; c.vel = Vector3.zero; }
                if (c.inf <= 0.001f) continue;
                Vector3 desired = shoulder + (right * (side * c.value.x) + up * c.value.y + fwd * c.value.z) * _armLen;
                Vector3 target = Vector3.Lerp(animHand[s], desired, Mathf.Clamp01(c.inf));
                Vector3 pole = -fwd * 0.6f + right * (side * 0.5f) - up * 0.5f;
                TwoBoneIk.Solve(upper, lower, hand, target, pole);
            }

            // 5. legs: planted where the animator put them (plus the gesture's foot offset) whenever the hips or a foot move
            bool legs = hp.inf > 0.001f || hr.inf > 0.001f || _ch[RF].inf > 0.001f || _ch[LF].inf > 0.001f;
            if (legs)
            {
                for (int s = 0; s < 2; s++)
                {
                    var c = _ch[RF + s];
                    float side = s == 0 ? 1f : -1f;
                    Vector3 offset = (right * (side * c.value.x) + up * c.value.y + fwd * c.value.z) * (_legLen * Mathf.Clamp01(c.inf));
                    Vector3 target = animFoot[s] + offset;
                    Vector3 pole = fwd + right * (side * 0.25f);
                    TwoBoneIk.Solve(_leg[s * 3], _leg[s * 3 + 1], _leg[s * 3 + 2], target, pole);
                    _leg[s * 3 + 2].rotation = animFootRot[s];
                }
            }
        }

        private static Quaternion Euler(Vector3 e, Vector3 right, Vector3 up, Vector3 fwd)
            => Quaternion.AngleAxis(e.y, up) * Quaternion.AngleAxis(e.x, right) * Quaternion.AngleAxis(-e.z, fwd);

        private static void RotateAbout(Transform t, Quaternion world, Vector3 pivot)
        {
            t.position = pivot + world * (t.position - pivot);
            t.rotation = world * t.rotation;
        }

        private static void Part(Transform bone, Vector3 e, float fraction, Vector3 right, Vector3 up, Vector3 fwd)
            => bone.rotation = Euler(e * fraction, right, up, fwd) * bone.rotation;
    }
}