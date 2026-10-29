using System;
using UnityEngine;

namespace NihongoLife.Home
{
    /// <summary>One piece of furniture the player can buy for the bedroom (built from simple shapes, no third-party assets).</summary>
    public sealed class DecorDef
    {
        public string Id, Vi, En, Ja;
        public int Price;
        public Vector2 Size;          // footprint (width x, depth z) in metres
        public bool Flat;             // rugs: no collider, may lie under other things
        public Action<Transform> Build;
    }

    public static class RoomDecorCatalog
    {
        private static Material M(Color c) => NihongoLife.World.AmbientWildlife.MaterialFor(c);

        private static GameObject Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color c)
        {
            var g = GameObject.CreatePrimitive(type);
            var col = g.GetComponent<Collider>(); if (col != null) UnityEngine.Object.Destroy(col);
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = M(c);
            return g;
        }

        public static readonly DecorDef[] All =
        {
            new DecorDef { Id = "plant", Vi = "Chậu cây", En = "Potted plant", Ja = "観葉植物", Price = 800, Size = new Vector2(0.5f, 0.5f), Build = t => {
                Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.18f, 0), new Vector3(0.38f, 0.18f, 0.38f), new Color(0.72f, 0.42f, 0.3f));
                Part(t, PrimitiveType.Sphere, new Vector3(0, 0.62f, 0), new Vector3(0.5f, 0.55f, 0.5f), new Color(0.2f, 0.55f, 0.28f));
                Part(t, PrimitiveType.Sphere, new Vector3(0.12f, 0.95f, 0.05f), new Vector3(0.3f, 0.3f, 0.3f), new Color(0.3f, 0.65f, 0.33f)); } },
            new DecorDef { Id = "lamp", Vi = "Đèn đứng", En = "Floor lamp", Ja = "フロアランプ", Price = 1500, Size = new Vector2(0.4f, 0.4f), Build = t => {
                Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(0.3f, 0.03f, 0.3f), new Color(0.2f, 0.2f, 0.22f));
                Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.8f, 0), new Vector3(0.04f, 0.8f, 0.04f), new Color(0.25f, 0.25f, 0.28f));
                Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.65f, 0), new Vector3(0.34f, 0.17f, 0.34f), new Color(1f, 0.93f, 0.7f));
                var l = new GameObject("Light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 1.5f, 0);
                l.type = LightType.Point; l.range = 4.5f; l.intensity = 1.2f; l.color = new Color(1f, 0.9f, 0.7f); } },
            new DecorDef { Id = "shelf", Vi = "Kệ sách", En = "Bookshelf", Ja = "本棚", Price = 3000, Size = new Vector2(1.0f, 0.4f), Build = t => {
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.9f, 0), new Vector3(1.0f, 1.8f, 0.4f), new Color(0.55f, 0.38f, 0.22f));
                var cols = new[] { new Color(0.8f, 0.25f, 0.25f), new Color(0.25f, 0.5f, 0.8f), new Color(0.9f, 0.8f, 0.3f), new Color(0.3f, 0.7f, 0.45f) };
                for (int row = 0; row < 4; row++) for (int i = 0; i < 5; i++)
                    Part(t, PrimitiveType.Cube, new Vector3(-0.38f + i * 0.19f, 0.3f + row * 0.4f, -0.12f), new Vector3(0.14f, 0.3f, 0.2f), cols[(row + i) % 4]); } },
            new DecorDef { Id = "cushion", Vi = "Gối ngồi", En = "Floor cushion", Ja = "座布団", Price = 600, Size = new Vector2(0.6f, 0.6f), Build = t => {
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.07f, 0), new Vector3(0.6f, 0.14f, 0.6f), new Color(0.85f, 0.45f, 0.5f)); } },
            new DecorDef { Id = "table", Vi = "Bàn nhỏ", En = "Side table", Ja = "サイドテーブル", Price = 2000, Size = new Vector2(0.9f, 0.6f), Build = t => {
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.55f, 0), new Vector3(0.9f, 0.06f, 0.6f), new Color(0.7f, 0.5f, 0.3f));
                foreach (var p in new[] { new Vector2(-0.38f, -0.24f), new Vector2(0.38f, -0.24f), new Vector2(-0.38f, 0.24f), new Vector2(0.38f, 0.24f) })
                    Part(t, PrimitiveType.Cylinder, new Vector3(p.x, 0.26f, p.y), new Vector3(0.06f, 0.26f, 0.06f), new Color(0.5f, 0.35f, 0.2f)); } },
            new DecorDef { Id = "rug", Vi = "Thảm", En = "Rug", Ja = "ラグ", Price = 1200, Size = new Vector2(2.0f, 1.4f), Flat = true, Build = t => {
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.012f, 0), new Vector3(2.0f, 0.024f, 1.4f), new Color(0.35f, 0.5f, 0.7f));
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.026f, 0), new Vector3(1.7f, 0.012f, 1.1f), new Color(0.9f, 0.85f, 0.7f)); } },
            new DecorDef { Id = "tv", Vi = "Tivi và kệ", En = "TV and stand", Ja = "テレビ台", Price = 4500, Size = new Vector2(1.3f, 0.45f), Build = t => {
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(1.3f, 0.5f, 0.45f), new Color(0.3f, 0.22f, 0.15f));
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.85f, 0), new Vector3(1.0f, 0.58f, 0.06f), new Color(0.06f, 0.06f, 0.08f));
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.85f, -0.035f), new Vector3(0.92f, 0.5f, 0.01f), new Color(0.2f, 0.35f, 0.55f)); } },
            new DecorDef { Id = "kotatsu", Vi = "Bàn kotatsu", En = "Kotatsu table", Ja = "こたつ", Price = 3500, Size = new Vector2(1.1f, 1.1f), Build = t => {
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.33f, 0), new Vector3(1.0f, 0.3f, 1.0f), new Color(0.85f, 0.35f, 0.3f));
                Part(t, PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(1.1f, 0.05f, 1.1f), new Color(0.78f, 0.6f, 0.4f)); } },
        };

        public static DecorDef Find(string id) { foreach (var d in All) if (d.Id == id) return d; return null; }
    }

    /// <summary>Marks a spawned piece of decor so placement checks and saves can find it.</summary>
    public sealed class RoomDecorItem : MonoBehaviour
    {
        public DecorDef Def; public float X, Z, Rot;
        public Renderer[] Renderers; public Material[][] Originals;
    }
}