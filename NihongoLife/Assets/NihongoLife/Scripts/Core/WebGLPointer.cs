using System.Runtime.InteropServices;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>True in a mobile browser. Desktop WebGL stays false, so a resized window does not grow touch controls.</summary>
    public static class WebGLPointer
    {
        public static bool Coarse
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                try { return NihongoCoarsePointer() != 0; }
                catch (System.Exception) { return false; }
#else
                return false;
#endif
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int NihongoCoarsePointer();
#endif
    }
}
