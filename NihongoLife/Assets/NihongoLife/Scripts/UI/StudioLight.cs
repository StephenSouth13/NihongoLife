using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NihongoLife.UI
{
    /// <summary>
    /// Bright, front-facing "photo studio" key light for character pictures (HUD portrait, Tab profile). The toon
    /// shaders of the character models are lit by the main light only, i.e. the world's sun: without this a
    /// portrait taken at night is black and by day lit from wherever the sun happens to be. The sun is borrowed
    /// only while the picture camera renders and restored straight after, so the world never sees the change.
    /// </summary>
    public static class StudioLight
    {
        private static readonly Color KeyColor = new Color(1f, 0.97f, 0.92f);

        public readonly struct Scope : IDisposable
        {
            private readonly Light _sun;
            private readonly Quaternion _rotation;
            private readonly Color _color;
            private readonly float _intensity, _shadowStrength;
            private readonly bool _enabled;

            public Scope(Light sun, Transform subject)
            {
                _sun = sun;
                _rotation = sun != null ? sun.transform.rotation : Quaternion.identity;
                _color = sun != null ? sun.color : Color.white;
                _intensity = sun != null ? sun.intensity : 0f;
                _shadowStrength = sun != null ? sun.shadowStrength : 0f;
                _enabled = sun != null && sun.enabled;
                if (sun == null) return;
                // Subjects face +Z (towards the camera): light from the front, a little above and to the side.
                Vector3 facing = subject != null ? subject.forward : Vector3.forward;
                Vector3 side = subject != null ? subject.right : Vector3.right;
                sun.transform.rotation = Quaternion.LookRotation((-facing - Vector3.up * 0.55f + side * 0.35f).normalized, Vector3.up);
                sun.color = KeyColor;
                sun.intensity = 1.25f;
                sun.shadowStrength = 0f;
                sun.enabled = true;
            }

            public void Dispose()
            {
                if (_sun == null) return;
                _sun.transform.rotation = _rotation;
                _sun.color = _color;
                _sun.intensity = _intensity;
                _sun.shadowStrength = _shadowStrength;
                _sun.enabled = _enabled;
            }
        }

        public static Scope Use(Transform subject) => new Scope(Sun(), subject);

        private static Light Sun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional && light.isActiveAndEnabled) return light;
            return null;
        }

        /// <summary>For a camera that renders every frame: the studio light is applied around that camera only.</summary>
        public sealed class CameraHook : IDisposable
        {
            private readonly Camera _camera;
            private readonly Func<Transform> _subject;
            private Scope? _scope;

            public CameraHook(Camera camera, Func<Transform> subject)
            {
                _camera = camera;
                _subject = subject;
                RenderPipelineManager.beginCameraRendering += Begin;
                RenderPipelineManager.endCameraRendering += End;
            }

            private void Begin(ScriptableRenderContext context, Camera camera)
            {
                if (camera == _camera && _scope == null) _scope = Use(_subject?.Invoke());
            }

            private void End(ScriptableRenderContext context, Camera camera)
            {
                if (camera != _camera || _scope == null) return;
                _scope.Value.Dispose();
                _scope = null;
            }

            public void Dispose()
            {
                RenderPipelineManager.beginCameraRendering -= Begin;
                RenderPipelineManager.endCameraRendering -= End;
                _scope?.Dispose();
                _scope = null;
            }
        }
    }
}
