import io
P=r'D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife\Assets\NihongoLife\Scripts\World\TownFolk.cs'
t=io.open(P,'rb').read().decode('utf-8').replace('\r\n','\n')
def rep(a,b):
    global t
    assert t.count(a)==1,a
    t=t.replace(a,b)
rep('''        public static bool Ground(Vector3 from, out Vector3 point)
        {
            point = from;
            if (!Physics.Raycast(from + Vector3.up * 6f, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) return false;
            point = hit.point; return true;
        }''','''        private static readonly RaycastHit[] Hits = new RaycastHit[24];

        /// <summary>The ground under a point: the nearest hit below it that is not <paramref name="ignore"/> (a resident's own body).</summary>
        public static bool Ground(Vector3 from, out Vector3 point, Transform ignore = null)
        {
            point = from;
            int n = Physics.RaycastNonAlloc(from + Vector3.up * 6f, Vector3.down, Hits, 40f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = Hits[i];
                if (ignore != null && h.collider.transform.IsChildOf(ignore)) continue;
                if (h.collider.GetComponentInParent<FolkWalker>() != null || h.collider.GetComponentInParent<PlayerController>() != null) continue;
                if (h.distance < best) { best = h.distance; point = h.point; found = true; }
            }
            return found;
        }''')
rep('if (TownFolk.Ground(p, out var g) && Mathf.Abs(g.y - transform.position.y) < 0.6f)','if (TownFolk.Ground(p, out var g, transform) && Mathf.Abs(g.y - transform.position.y) < 0.6f)')
rep('if (!TownFolk.Ground(p, out var g) || Mathf.Abs(g.y - Home.y) > 0.8f','if (!TownFolk.Ground(p, out var g, transform) || Mathf.Abs(g.y - Home.y) > 0.8f')
io.open(P,'wb').write(t.replace('\n','\r\n').encode('utf-8'))
print('ok')