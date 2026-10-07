using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Interaction
{
    public enum EmoteGesture { Talk, Bow, Jump, Point }

    public sealed class EmoteDefinition
    {
        public EmoteDefinition(string id, string ja, string reading, string vi, EmoteGesture gesture)
        {
            Id = id; Ja = ja; Reading = reading; Vi = vi; Gesture = gesture;
        }

        public string Id { get; }
        public string Ja { get; }
        public string Reading { get; }
        public string Vi { get; }
        public EmoteGesture Gesture { get; }
    }

    /// <summary>The eight emotes of the hold-E wheel, clockwise from the top. Each one is a short
    /// everyday Japanese phrase, so emoting doubles as speaking practice.</summary>
    public static class EmoteCatalog
    {
        public static readonly IReadOnlyList<EmoteDefinition> All = new[]
        {
            new EmoteDefinition("hello", "こんにちは！", "konnichiwa", "Xin chào!", EmoteGesture.Point),
            new EmoteDefinition("thanks", "ありがとう！", "arigatou", "Cảm ơn!", EmoteGesture.Bow),
            new EmoteDefinition("wow", "すごい！", "sugoi", "Tuyệt quá!", EmoteGesture.Jump),
            new EmoteDefinition("fun", "たのしい♪", "tanoshii", "Vui quá!", EmoteGesture.Talk),
            new EmoteDefinition("yatta", "やった！", "yatta", "Yeah, làm được rồi!", EmoteGesture.Jump),
            new EmoteDefinition("oyasumi", "おやすみ…", "oyasumi", "Chúc ngủ ngon", EmoteGesture.Bow),
            new EmoteDefinition("huh", "えっ？", "e?", "Hả? Gì cơ?", EmoteGesture.Talk),
            new EmoteDefinition("sorry", "すみません", "sumimasen", "Xin lỗi", EmoteGesture.Bow),
        };

        private static readonly Dictionary<string, Sprite> Sprites = new();

        public static Sprite Icon(string id) => Load("Emotes/emote_" + id);

        public static Sprite Load(string resourcePath)
        {
            if (Sprites.TryGetValue(resourcePath, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            texture.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            sprite.name = texture.name;
            Sprites[resourcePath] = sprite;
            return sprite;
        }
    }
}
