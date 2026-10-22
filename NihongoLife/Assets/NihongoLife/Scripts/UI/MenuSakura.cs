using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// A few unlit petals drifting in front of the menu camera. No shadows, no post-processing.
    /// Hidden while reduce-motion is on, and kept small on WebGL.
    /// </summary>
    public sealed class MenuSakura : MonoBehaviour
    {
        private Transform[] _petals;
        private Vector3[] _speed;
        private Material _material;
        private Transform _field;
        private bool _built;

        public static MenuSakura Ensure(Transform host)
        {
            if (host == null) return null;
            var existing = host.GetComponent<MenuSakura>();
            if (existing != null) return existing;
            return host.gameObject.AddComponent<MenuSakura>();
        }

        private void OnEnable() => MotionSettings.Changed += RefreshVisible;
        private void OnDisable() => MotionSettings.Changed -= RefreshVisible;

        private void LateUpdate()
        {
            if (MotionSettings.ReduceMotion)
            {
                SetVisible(false);
                return;
            }
            if (!_built) Build();
            SetVisible(true);
            float dt = Time.unscaledDeltaTime;
            var camera = Camera.main;
            Vector3 origin = camera != null ? camera.transform.position + camera.transform.forward * 8f : Vector3.zero;
            for (int i = 0; i < _petals.Length; i++)
            {
                Transform petal = _petals[i];
                if (petal == null) continue;
                Vector3 p = petal.position;
                Vector3 v = _speed[i];
                p += v * dt;
                petal.Rotate(0f, 40f * dt, 80f * dt, Space.Self);
                if (p.y < origin.y - 6f) p = origin + new Vector3(Random.Range(-9f, 9f), 6f, Random.Range(-4f, 4f));
                petal.position = p;
            }
        }

        private void RefreshVisible()
        {
            if (MotionSettings.ReduceMotion) SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_petals == null) return;
            for (int i = 0; i < _petals.Length; i++)
                if (_petals[i] != null && _petals[i].gameObject.activeSelf != visible)
                    _petals[i].gameObject.SetActive(visible);
        }

        private void Build()
        {
            _built = true;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                _petals = System.Array.Empty<Transform>();
                return;
            }
            int count = Application.platform == RuntimePlatform.WebGLPlayer ? 10 : 18;
            _material = new Material(shader);
            var tint = new Color(1f, 0.78f, 0.84f, 0.85f);
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", tint);
            else _material.color = tint;
            _field = new GameObject("MenuSakuraField").transform;
            _petals = new Transform[count];
            _speed = new Vector3[count];
            var camera = Camera.main;
            Vector3 origin = camera != null ? camera.transform.position + camera.transform.forward * 8f : Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "Petal";
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                go.transform.SetParent(_field, true);
                go.transform.position = origin + new Vector3(Random.Range(-9f, 9f), Random.Range(-4f, 6f), Random.Range(-4f, 4f));
                go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
                go.transform.rotation = Random.rotation;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = _material;
                _petals[i] = go.transform;
                _speed[i] = new Vector3(Random.Range(-0.25f, 0.35f), Random.Range(-0.55f, -0.25f), Random.Range(-0.1f, 0.1f));
            }
        }

        private void OnDestroy()
        {
            if (_field != null) Destroy(_field.gameObject);
            if (_material != null) Destroy(_material);
        }
    }
}
