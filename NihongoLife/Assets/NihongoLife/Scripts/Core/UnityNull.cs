namespace NihongoLife
{
    /// <summary>
    /// Unity's "fake null": in the Editor, GetComponent&lt;T&gt;() for a missing component returns an object that is
    /// == null but is not a C# null, so <c>GetComponent&lt;T&gt;() ?? AddComponent&lt;T&gt;()</c> never adds anything and the
    /// next access throws MissingComponentException. Write <c>GetComponent&lt;T&gt;().OrNull() ?? …</c> instead.
    /// </summary>
    public static class UnityNull
    {
        public static T OrNull<T>(this T value) where T : UnityEngine.Object => value != null ? value : null;
    }
}
