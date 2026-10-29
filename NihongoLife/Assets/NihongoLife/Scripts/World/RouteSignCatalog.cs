using System;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.World
{
    [Serializable]
    public sealed class RouteSignBoardData
    {
        public string id;
        /// <summary>"green" for places the player works or shops at, "blue" for everything else.</summary>
        public string color;
        public LocalizedLine label;
        /// <summary>Name or path of a scene object the board points at (resolved when the builder bakes the sign)...</summary>
        public string anchor;
        /// <summary>...or a fixed world position [x, y, z] when there is no object to point at.</summary>
        public float[] pos;
    }

    [Serializable]
    public sealed class RouteSignPostData
    {
        public string id;
        public string name;
        public float[] pos;
        public float yaw;
        public RouteSignBoardData[] boards;
    }

    [Serializable]
    public sealed class RouteSignMapData
    {
        public string scene;
        /// <summary>Scene root the builder (re)creates for this map.</summary>
        public string root;
        public RouteSignPostData[] posts;
    }

    [Serializable]
    public sealed class RouteSignFile
    {
        public string schema;
        public RouteSignMapData[] maps;
    }

    /// <summary>The direction signs of every map (Resources/Navigation/route_signs.json); RouteSignBuilder bakes them, RouteSignPost keeps their text in the interface language.</summary>
    public static class RouteSignCatalog
    {
        public const string ResourcePath = "Navigation/route_signs";
        private static RouteSignFile _file;

        public static RouteSignFile File => _file ??= Load();

        public static RouteSignFile Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing " + ResourcePath + ".json");
            var file = JsonUtility.FromJson<RouteSignFile>(asset.text);
            if (file == null || file.maps == null) throw new InvalidOperationException("route_signs.json has no maps.");
            return file;
        }

        public static void Reload() => _file = null;

        public static RouteSignMapData Map(string scene) => File.maps.FirstOrDefault(m => m.scene == scene);

        public static RouteSignPostData Post(string scene, string postId) => Map(scene)?.posts?.FirstOrDefault(p => p.id == postId);

        public static RouteSignBoardData Board(string scene, string postId, string boardId) => Post(scene, postId)?.boards?.FirstOrDefault(b => b.id == boardId);

        public static Vector3 ToVector(float[] values) => values != null && values.Length >= 3 ? new Vector3(values[0], values[1], values[2]) : Vector3.zero;
    }
}
