using UnityEngine;

namespace NihongoLife.Trailer
{
    public enum TrailerAnimationCue
    {
        None,
        Talk,
        Bow,
        Point
    }

    [CreateAssetMenu(fileName = "TrailerShotDefinition", menuName = "NihongoLife/Trailer Shot Definition")]
    public class TrailerShotDefinition : ScriptableObject
    {
        [Header("Ordering")]
        public int order;

        [Header("Camera Anchors")]
        public Transform startAnchor;
        public Transform endAnchor;
        public Vector3 startPosition;
        public Vector3 startEulerAngles;
        public Vector3 endPosition;
        public Vector3 endEulerAngles;

        [Header("Motion")]
        [Min(0.1f)] public float durationSeconds = 3f;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public bool lookAtFocusTarget = true;
        public bool fadeToBlackAfterShot = true;

        [Header("Focus")]
        public Transform focusTarget;
        public string focusTargetName;
        public TrailerAnimationCue animationCueToTrigger = TrailerAnimationCue.None;

        [Header("Overlay")]
        public string textJa;
        public string textEn;

        public Vector3 ResolveStartPosition()
        {
            return startAnchor != null ? startAnchor.position : startPosition;
        }

        public Quaternion ResolveStartRotation()
        {
            return startAnchor != null ? startAnchor.rotation : Quaternion.Euler(startEulerAngles);
        }

        public Vector3 ResolveEndPosition()
        {
            return endAnchor != null ? endAnchor.position : endPosition;
        }

        public Quaternion ResolveEndRotation()
        {
            return endAnchor != null ? endAnchor.rotation : Quaternion.Euler(endEulerAngles);
        }
    }
}
