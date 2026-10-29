using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using UnityEngine;

namespace NihongoLife.Player
{
    // ─────────── Data: Resources/Characters/Looks ───────────

    /// <summary>A hair colour of the look panel (look_options.json). Empty hex = the model's own colour.</summary>
    [Serializable]
    public sealed class LookHairOption
    {
        public string id, vi, en, ja, hex;
        public string Name => Loc.Pick(vi, en, ja);
    }

    /// <summary>One ellipsoid of a beard, in face units (x right, y up, z forward, 1 = the face's width) from the
    /// front of the mouth. Tone "hair" = the hair colour, "stubble" = hair blended into the skin.</summary>
    [Serializable]
    public sealed class LookBeardPiece
    {
        public string tone = "hair";
        public float[] pos = { 0f, 0f, 0f };
        public float[] scale = { 0.1f, 0.1f, 0.1f };
        public float[] rot = { 0f, 0f, 0f };
    }

    [Serializable]
    public sealed class LookBeardOption
    {
        public string id, vi, en, ja;
        /// <summary>The character must be at least this old (the player grows a year each birthday).</summary>
        public int minAge;
        public LookBeardPiece[] pieces = Array.Empty<LookBeardPiece>();
        public string Name => Loc.Pick(vi, en, ja);
    }

    /// <summary>Lines of an older face (look_options.json "wrinkles"): every set at or under the age shows.</summary>
    [Serializable]
    public sealed class LookWrinkleSet
    {
        public int minAge;
        public LookBeardPiece[] pieces = Array.Empty<LookBeardPiece>();
    }

    [Serializable]
    public sealed class LookOptions
    {
        public string schema;
        /// <summary>From this age the hair greys by itself (when "grey with age" is on), fully at grayFullAge.</summary>
        public int grayFromAge = 45, grayFullAge = 75;
        public string grayHex = "#C8C8CE";
        /// <summary>Older characters walk and talk slower: 1 until slowFromAge, down to slowestWalk / slowestTalk at slowestAge.</summary>
        public int slowFromAge = 55, slowestAge = 85;
        public float slowestWalk = 0.72f, slowestTalk = 0.6f;
        public LookWrinkleSet[] wrinkles = Array.Empty<LookWrinkleSet>();
        public LookHairOption[] hairs = Array.Empty<LookHairOption>();
        public LookBeardOption[] beards = Array.Empty<LookBeardOption>();
        public LookHairOption Hair(string id) => hairs.FirstOrDefault(h => h.id == id);
        public LookBeardOption Beard(string id) => beards.FirstOrDefault(b => b.id == id);
    }

    /// <summary>What CharacterLookBuilder measured on one Kids-pack model (looks.json, generated — do not edit):
    /// the hair colour and mask on its atlas, the skin colour, and the face in head-bone space for the beard.</summary>
    [Serializable]
    public sealed class LookModel
    {
        public string model, texture, mask, hairHex, skinHex;
        public bool boy;
        public float[] forward, up, mouth;
        public float faceWidth;
    }

    [Serializable]
    public sealed class LookModels
    {
        public string schema;
        public LookModel[] models = Array.Empty<LookModel>();
    }

    /// <summary>The player's chosen look (saved on this device with the chosen character).</summary>
    [Serializable]
    public sealed class CharacterLookState
    {
        public string hair = "natural";
        public string beard = "none";
        public bool grayWithAge = true;
    }

    /// <summary>
    /// Hair colour and beard for the Kids-pack characters. The hair is recoloured on the GPU (the atlas blitted through
    /// Hidden/NihongoLife/HairRecolor with the model's baked hair mask, one shared material per model and colour, so
    /// clones such as the HUD portrait keep it); the beard is a few ellipsoids on their own child ("Look_Beard") of the
    /// head bone, so it follows every animation. Beards open with age (look_options.json minAge, boys' models only),
    /// and with "grey with age" the hair greys from <see cref="LookOptions.grayFromAge"/>. The player's look is set in
    /// <see cref="NihongoLife.UI.CharacterLookUI"/>; NPCs (elders) set theirs in the inspector or by code.
    /// </summary>
    public sealed class CharacterLook : MonoBehaviour
    {
        public const string PrefsKey = "NihongoLife.CharacterLook";
        /// <summary>The head bone's child holding the beard and the wrinkles.</summary>
        public const string BeardName = "Look_Face";
        private const string ModelsPath = "Characters/Looks/looks";
        private const string OptionsPath = "Characters/Looks/look_options";

        [Tooltip("Use the player's saved look (the player and the menu preview). Off: the fields below (NPCs).")]
        [SerializeField] private bool playerLook = true;
        [SerializeField] private string hair = "natural";
        [SerializeField] private string beard = "none";
        [SerializeField] private int age = 70;
        [SerializeField] private bool grayWithAge = true;
        [Tooltip("This character's own pace (walk and talk ×), 0 = by age.")]
        [SerializeField] private float pace;

        /// <summary>False in tests (PlayModeSaveGuard): changes stay in memory, the device's look is not touched.</summary>
        public static bool Persist = true;
        /// <summary>Tests: the player's age instead of the calendar's.</summary>
        public static int? AgeOverride;
        /// <summary>Bumped on every change (the HUD portrait re-shoots when it moves).</summary>
        public static int Version { get; private set; }
        public static event Action Changed;

        private static LookOptions _options;
        private static LookModels _models;
        private static CharacterLookState _state;
        private static Mesh _sphere;
        private static Material _hairBlit, _paletteBlit, _beardBase;
        private static readonly Dictionary<string, Material> HairMaterials = new Dictionary<string, Material>();
        private static readonly Dictionary<Color, Material> BeardMaterials = new Dictionary<Color, Material>();

        private int _appliedVersion = -1, _appliedAge = -1, _visualId;
        /// <summary>Skin / top / bottom colours of a variant (NPCs); null = the model's own.</summary>
        private LookTint _tint;
        private SkinnedMeshRenderer _probe;
        private float _nextCheck;

        public static LookOptions Options => _options ??= Load<LookOptions>(OptionsPath) ?? new LookOptions();
        public static LookModels Models => _models ??= Load<LookModels>(ModelsPath) ?? new LookModels();

        private static T Load<T>(string path) where T : class
        {
            var text = Resources.Load<TextAsset>(path);
            return text != null ? JsonUtility.FromJson<T>(text.text) : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _state = null; Changed = null; }

        // ─────────── The player's look ───────────

        public static CharacterLookState State
        {
            get
            {
                if (_state != null) return _state;
                string json = Persist ? PlayerPrefs.GetString(PrefsKey, "") : "";
                try { _state = string.IsNullOrEmpty(json) ? new CharacterLookState() : JsonUtility.FromJson<CharacterLookState>(json); }
                catch (ArgumentException) { _state = null; }
                return _state ??= new CharacterLookState();
            }
        }

        /// <summary>Tests: forget the in-memory look (the next read starts fresh, from the device when Persist).</summary>
        public static void ResetForTests() { _state = null; Version++; }

        /// <summary>The player's age: the game calendar (a year older each birthday), else the save.</summary>
        public static int PlayerAge
        {
            get
            {
                if (AgeOverride.HasValue) return AgeOverride.Value;
                if (DayNightCycle.Active != null) return DayNightCycle.Active.Age;
                if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repository) && repository.GetProgress() is PlayerProgressDto p)
                    return GameCalendar.AgeOn(p.calendarDay, p.birthdayMonth, p.birthdayDay, p.ageAtEpoch);
                return 16;
            }
        }

        /// <summary>Why <paramref name="beardId"/> cannot be worn yet (null = allowed).</summary>
        public static string BeardBlocker(string beardId, int age, bool boy)
        {
            var option = Options.Beard(beardId);
            if (option == null || option.pieces.Length == 0) return null;
            if (!boy) return Loc.T("Kiểu này dành cho nhân vật nam.", "For the boys' characters.", "男の子のキャラクター用です。");
            if (age < option.minAge) return Loc.F("Mở khi {0} tuổi (bạn {1} tuổi).", "Opens at age {0} (you are {1}).", "{0}歳から（今は{1}歳）。", option.minAge, age);
            return null;
        }

        public static void SetHair(string id)
        {
            if (Options.Hair(id) == null) return;
            State.hair = id;
            Save();
        }

        /// <summary>Wears a beard; null on success, else why not.</summary>
        public static string SetBeard(string id, bool boy)
        {
            string why = BeardBlocker(id, PlayerAge, boy);
            if (why != null) return why;
            if (Options.Beard(id) == null) return Loc.T("Không có kiểu râu này.", "No such beard.", "そのひげはありません。");
            State.beard = id;
            Save();
            return null;
        }

        public static void SetGrayWithAge(bool on)
        {
            State.grayWithAge = on;
            Save();
        }

        private static void Save()
        {
            if (Persist) { PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(State)); PlayerPrefs.Save(); }
            Version++;
            Changed?.Invoke();
        }

        // ─────────── Pace: walking and talking slow down with age ───────────

        private static float Slow(int years, float slowest)
        {
            var o = Options;
            float t = Mathf.InverseLerp(o.slowFromAge, Mathf.Max(o.slowFromAge + 1, o.slowestAge), years);
            return Mathf.Lerp(1f, Mathf.Clamp(slowest, 0.3f, 1f), t);
        }

        public static float WalkFactor(int years) => Slow(years, Options.slowestWalk);
        public static float TalkFactor(int years) => Slow(years, Options.slowestTalk);

        /// <summary>The player's walk factor, cached per in-game day (PlayerController reads it every frame).</summary>
        public static float PlayerWalkFactor
        {
            get
            {
                if (Time.frameCount != _walkFrame && Time.unscaledTime >= _walkCheck) { _walkCheck = Time.unscaledTime + 1f; _walk = WalkFactor(PlayerAge); }
                _walkFrame = Time.frameCount;
                return _walk;
            }
        }
        private static float _walk = 1f, _walkCheck;
        private static int _walkFrame = -1;

        /// <summary>This character's walk pace (its own setting, else its age).</summary>
        public float Walk => pace > 0f ? pace : WalkFactor(Age);
        /// <summary>This character's speaking pace (dialogue text speed): its own setting, else its age.</summary>
        public float Talk => pace > 0f ? pace : TalkFactor(Age);

        /// <summary>Sets a character's own pace (0 = by age).</summary>
        public CharacterLook Pace(float factor) { pace = Mathf.Max(0f, factor); return this; }

        // ─────────── The component ───────────

        /// <summary>The look on <paramref name="actor"/> (added when missing), following the player's or fixed.</summary>
        public static CharacterLook On(GameObject actor, bool player = true)
        {
            var look = actor.GetComponent<CharacterLook>().OrNull() ?? actor.AddComponent<CharacterLook>();
            look.playerLook = player;
            return look;
        }

        /// <summary>A fixed look (NPCs): e.g. an elder with silver hair and a long beard.</summary>
        public CharacterLook Fixed(string hairId, string beardId, int years, bool greyWithAge = true)
        {
            playerLook = false;
            hair = hairId; beard = beardId; age = years; grayWithAge = greyWithAge;
            Refresh();
            return this;
        }

        /// <summary>A fixed look with its own skin, top and bottom colours (a <see cref="CharacterVariant"/>).</summary>
        public CharacterLook Fixed(string hairId, string beardId, int years, LookTint tint, bool greyWithAge = true)
        {
            _tint = tint;
            return Fixed(hairId, beardId, years, greyWithAge);
        }

        public int Age => playerLook ? PlayerAge : age;
        public bool IsBoy => ModelOf(Visual())?.boy == true;

        private void OnEnable() { Changed += Refresh; Refresh(); }
        private void OnDisable() => Changed -= Refresh;

        private void Update()
        {
            // A birthday (grey hair, beards opening) or a new model: checked twice a second, not every frame.
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.5f;
            var visual = Visual();
            int id = visual != null ? visual.GetInstanceID() : 0;
            if (id != _visualId || Age != _appliedAge || (playerLook && Version != _appliedVersion) || AtlasLost(_probe)) Refresh();
        }

        public void Refresh()
        {
            var visual = Visual();
            _visualId = visual != null ? visual.GetInstanceID() : 0;
            _appliedVersion = Version;
            _appliedAge = Age;
            if (visual == null) return;
            _probe = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (playerLook) Apply(visual, State.hair, State.beard, Age, State.grayWithAge);
            else Apply(visual, hair, beard, age, grayWithAge, _tint);
        }

        private GameObject Visual()
        {
            var child = transform.Find("Visual");
            if (child != null) return child.gameObject;
            return GetComponentInChildren<SkinnedMeshRenderer>(true) != null ? gameObject : null;
        }

        // ─────────── Applying a look to a model ───────────

        public static LookModel ModelOf(GameObject visual)
        {
            if (visual == null) return null;
            foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var model = ModelOf(renderer);
                if (model != null) return model;
            }
            return null;
        }

        private static LookModel ModelOf(Renderer renderer)
        {
            var material = renderer.sharedMaterial;
            if (material == null) return null;
            if (HairMaterials.ContainsValue(material) && material.name.Contains("|")) return Models.models.FirstOrDefault(m => material.name.StartsWith(m.model + "|"));
            var texture = material.HasProperty("_Texture2D") ? material.GetTexture("_Texture2D") : null;
            return texture != null ? Models.models.FirstOrDefault(m => m.texture == texture.name) : null;
        }

        /// <summary>The hair colour shown: the chosen one, greyed by age when asked.</summary>
        public static Color HairColor(LookModel model, string hairId, int years, bool greyWithAge)
        {
            var option = Options.Hair(hairId);
            Color color = Hex(option != null && !string.IsNullOrEmpty(option.hex) ? option.hex : model.hairHex);
            if (greyWithAge && years > Options.grayFromAge)
            {
                float t = Mathf.InverseLerp(Options.grayFromAge, Mathf.Max(Options.grayFromAge + 1, Options.grayFullAge), years);
                color = Color.Lerp(color, Hex(Options.grayHex), t * 0.9f);
            }
            return color;
        }

        /// <summary>Recolours the hair and builds the beard of <paramref name="visual"/> (a Kids-pack model).</summary>
        public static void Apply(GameObject visual, string hairId, string beardId, int years, bool greyWithAge, LookTint tint = null)
        {
            if (visual == null) return;
            var model = ModelOf(visual);
            if (model == null) return;
            Color hairColor = HairColor(model, hairId, years, greyWithAge);
            bool tinted = tint != null && !tint.IsEmpty;
            bool natural = hairColor == Hex(model.hairHex) && !tinted;
            foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (ModelOf(renderer) != model) continue;
                var original = Original(renderer.sharedMaterial);
                renderer.sharedMaterial = natural ? original : HairMaterial(model, original, hairColor, tinted ? tint : null);
            }

            var animator = visual.GetComponentInChildren<Animator>(true);
            var head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head == null) return;
            var old = head.Find(BeardName);
            if (old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
            var pieces = new List<LookBeardPiece>();
            var option = Options.Beard(beardId);
            if (option != null && BeardBlocker(beardId, years, model.boy) == null) pieces.AddRange(option.pieces);
            foreach (var set in Options.wrinkles) if (years >= set.minAge) pieces.AddRange(set.pieces);
            if (pieces.Count > 0) BuildFace(head, model, pieces, hairColor, tinted && !string.IsNullOrEmpty(tint.skin) ? Hex(tint.skin) : Hex(model.skinHex));
        }

        private static readonly Dictionary<Material, Material> Originals = new Dictionary<Material, Material>();

        private static Material Original(Material material) => material != null && Originals.TryGetValue(material, out var original) ? original : material;

        private static Material HairMaterial(LookModel model, Material original, Color color, LookTint tint)
        {
            string key = model.model + "|" + ColorUtility.ToHtmlStringRGB(color) + (tint != null ? "|" + tint.Key : "");
            var source = original.GetTexture("_Texture2D");
            var mask = Resources.Load<Texture2D>("Characters/Looks/" + model.mask);
            _hairBlit ??= Resources.Load<Material>("Characters/Looks/NL_HairRecolor");
            if (HairMaterials.TryGetValue(key, out var cached) && cached != null)
            {
                if (!(cached.GetTexture("_Texture2D") is RenderTexture baked) || baked.IsCreated()) return cached;
                // The GPU dropped the baked atlas (WebGL context loss, a device reset): paint it again into the same texture.
                if (source != null && mask != null && _hairBlit != null) { Bake(baked, source, mask, model, color, tint); return cached; }
            }
            if (source == null || mask == null || _hairBlit == null) return original;
            var target = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "Hair_" + key, filterMode = source.filterMode, wrapMode = source.wrapMode, useMipMap = false,
            };
            Bake(target, source, mask, model, color, tint);
            var material = new Material(original) { name = key };
            material.SetTexture("_Texture2D", target);
            HairMaterials[key] = material;
            Originals[material] = original;
            return material;
        }

        private static void Bake(RenderTexture target, Texture source, Texture2D mask, LookModel model, Color color, LookTint tint = null)
        {
            // First the skin / top / bottom colours of a variant (their blocks are told apart by colour), then the hair
            // (told apart by its mask), so a hair colour close to the skin never gets repainted as skin.
            bool palette = tint != null && !tint.IsEmpty;
            bool hair = color != Hex(model.hairHex);
            RenderTexture middle = null;
            Texture current = source;
            if (palette)
            {
                _paletteBlit ??= Resources.Load<Material>("Characters/Variants/NL_PaletteRecolor");
                var colours = LookPaletteCatalog.For(model.model);
                if (_paletteBlit != null && colours != null)
                {
                    var swap = new Material(_paletteBlit);
                    swap.SetTexture("_Mask", mask);
                    SetSwap(swap, 0, colours.skinFrom, tint.skin);
                    SetSwap(swap, 1, colours.topFrom, tint.top);
                    SetSwap(swap, 2, colours.bottomFrom, tint.bottom);
                    RenderTexture into = target;
                    if (hair)
                    {
                        middle = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                        middle.filterMode = FilterMode.Point;
                        into = middle;
                    }
                    Graphics.Blit(current, into, swap);
                    Destroy(swap);
                    current = into;
                }
            }
            if (hair || current == source)
            {
                var blit = new Material(_hairBlit);
                blit.SetTexture("_Mask", mask);
                blit.SetColor("_From", Hex(model.hairHex));
                blit.SetColor("_To", color);
                Graphics.Blit(current, target, blit);
                Destroy(blit);
            }
            if (middle != null) RenderTexture.ReleaseTemporary(middle);
        }

        private static void SetSwap(Material swap, int index, string from, string to)
        {
            bool on = !string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to);
            var f = Hex(from); f.a = on ? 1f : 0f;
            var t = Hex(to); t.a = on ? 1f : 0f;
            swap.SetVector("_From" + index, f);
            swap.SetVector("_To" + index, t);
        }

        /// <summary>True when this model wears a baked hair atlas the GPU no longer has (the character would show black / vanish).</summary>
        private static bool AtlasLost(Renderer renderer) =>
            renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Texture2D") && renderer.sharedMaterial.GetTexture("_Texture2D") is RenderTexture atlas && !atlas.IsCreated();

        private static void BuildFace(Transform head, LookModel model, List<LookBeardPiece> pieces, Color hairColor, Color skin)
        {
            if (_sphere == null)
            {
                var probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _sphere = probe.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(probe);
            }
            var root = new GameObject(BeardName).transform;
            root.SetParent(head, false);
            root.localPosition = V(model.mouth);
            root.localRotation = Quaternion.LookRotation(V(model.forward), V(model.up));
            root.localScale = Vector3.one * model.faceWidth;
            Color stubble = Color.Lerp(hairColor, skin, 0.45f);
            Color wrinkle = Color.Lerp(skin, new Color(0.35f, 0.2f, 0.15f), 0.45f);
            int layer = head.gameObject.layer;
            root.gameObject.layer = layer;
            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                var go = new GameObject("Piece_" + i, typeof(MeshFilter), typeof(MeshRenderer));
                go.layer = layer;
                go.transform.SetParent(root, false);
                go.transform.localPosition = V(piece.pos);
                go.transform.localRotation = Quaternion.Euler(V(piece.rot));
                go.transform.localScale = V(piece.scale);
                go.GetComponent<MeshFilter>().sharedMesh = _sphere;
                go.GetComponent<MeshRenderer>().sharedMaterial = BeardMaterial(piece.tone == "stubble" ? stubble : piece.tone == "wrinkle" ? wrinkle : Color.Lerp(hairColor, Color.black, 0.08f));
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static Material BeardMaterial(Color color)
        {
            if (BeardMaterials.TryGetValue(color, out var cached) && cached != null) return cached;
            _beardBase ??= Resources.Load<Material>("Characters/Looks/NL_LookBeard");
            var material = _beardBase != null ? new Material(_beardBase) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "Beard_" + ColorUtility.ToHtmlStringRGB(color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            BeardMaterials[color] = material;
            return material;
        }

        public static Color Hex(string hex) => !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.gray;
        private static Vector3 V(float[] a) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero;
    }
}
