using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Premium dressing for the M-key map: legend, compass, scale bar, district names, window polish.
    /// All names come with vi/en/ja/ru here, so they follow the language switch.</summary>
    public static class MapPremium
    {
        public static string N4(string vi, string en, string ja, string ru)
        {
            switch (Loc.Code) { case "en": return en; case "ja": return ja; case "ru": return ru; default: return vi; }
        }

        // ── Legend categories (key = map place type) ──
        private static readonly Dictionary<string, string[]> Categories = new Dictionary<string, string[]>
        {
            ["KONBINI"] = new[] { "Cửa hàng", "Shop", "お店", "Магазин" },
            ["PLAZA"] = new[] { "Bảng tin", "Notice board", "掲示板", "Объявления" },
            ["SUSHI"] = new[] { "Quán ăn", "Restaurant", "飲食店", "Ресторан" },
            ["HOME"] = new[] { "Nhà ở", "Home", "住まい", "Жильё" },
            ["STATION"] = new[] { "Nhà ga", "Station", "駅", "Вокзал" },
            ["SCHOOL"] = new[] { "Trường học", "School", "学校", "Школа" },
            ["PARK"] = new[] { "Công viên", "Park", "公園", "Парк" },
            ["ARCADE"] = new[] { "Giải trí", "Arcade", "ゲーム", "Аркада" },
            ["CLINIC"] = new[] { "Y tế", "Clinic", "医療", "Клиника" },
            ["PQ_BEACH"] = new[] { "Bãi biển", "Beach", "ビーチ", "Пляж" },
            ["PQ_TOWN"] = new[] { "Thị trấn", "Town", "町", "Город" },
            ["PQ_MARKET"] = new[] { "Chợ", "Market", "市場", "Рынок" },
            ["PQ_RESTAURANT"] = new[] { "Nhà hàng", "Restaurant", "レストラン", "Ресторан" },
            ["PQ_TRANSPORT"] = new[] { "Giao thông", "Transport", "交通", "Транспорт" },
            ["PQ_NATURE"] = new[] { "Thiên nhiên", "Nature", "自然", "Природа" },
            ["PQ_FARM"] = new[] { "Nông trại", "Farm", "農場", "Ферма" },
            ["PQ_VILLAGE"] = new[] { "Làng", "Village", "村", "Деревня" },
            ["PQ_HISTORY"] = new[] { "Di tích", "History", "史跡", "История" },
            ["PQ_LANDMARK"] = new[] { "Địa danh", "Landmark", "名所", "Ориентир" },
            ["PQ_FACTORY"] = new[] { "Làng nghề", "Workshop", "工房", "Мастерская" },
            ["PQ_ENTERTAINMENT"] = new[] { "Vui chơi", "Fun", "娯楽", "Развлечения" },
            ["PQ_ISLAND"] = new[] { "Đảo", "Island", "島", "Остров" },
            ["EXIT"] = new[] { "Lối ra", "Exit", "出口", "Выход" },
            ["RECEPTION"] = new[] { "Tiếp nhận", "Reception", "受付", "Регистратура" },
            ["MENU"] = new[] { "Lễ tân", "Reception", "受付", "Ресепшн" },
            ["MEDICINE"] = new[] { "Nhận thuốc", "Pharmacy", "薬", "Аптека" },
            ["DOCTOR"] = new[] { "Bác sĩ", "Doctor", "診察", "Врач" },
            ["CHECK"] = new[] { "Khám", "Check-up", "検診", "Осмотр" },
            ["WAIT"] = new[] { "Phòng chờ", "Waiting", "待合室", "Ожидание" },
            ["KANA"] = new[] { "Trò chơi", "Game", "ゲーム", "Игра" },
            ["SHOOT"] = new[] { "Sắp mở", "Coming soon", "準備中", "Скоро" },
            ["PRIZE"] = new[] { "Đổi quà", "Prizes", "景品", "Призы" },
            ["TICKETS"] = new[] { "Vé", "Tickets", "券売機", "Билеты" },
            ["GATE"] = new[] { "Soát vé", "Gate", "改札", "Турникет" },
            ["ORDER"] = new[] { "Quầy", "Counter", "カウンター", "Стойка" },
            ["SEATS"] = new[] { "Bàn ăn", "Dining", "客席", "Зал" },
            ["TEACHER"] = new[] { "Giáo viên", "Teacher", "先生", "Учитель" },
            ["DESKS"] = new[] { "Bàn học", "Desks", "机", "Парты" },
            ["REST"] = new[] { "Nghỉ ngơi", "Rest", "休む", "Отдых" },
            ["STUDY"] = new[] { "Học bài", "Study", "勉強", "Учёба" },
            ["KITCHEN"] = new[] { "Bếp", "Kitchen", "台所", "Кухня" },
        };

        public static bool HasCategory(string type) => type != null && Categories.ContainsKey(type);
        public static string CategoryName(string type) => Categories.TryGetValue(type, out var n) ? N4(n[0], n[1], n[2], n[3]) : null;

        public static string YouLabel => N4("Bạn ở đây", "You are here", "現在地", "Вы здесь");
        public static string LockedLabel => N4("Chưa mở", "Locked", "未開放", "Закрыто");
        public static string DestinationLabel => N4("Điểm đến", "Destination", "目的地", "Цель");
        public static string NorthLabel => N4("B", "N", "北", "С");

        // ── Window polish ──
        public static void PolishWindow(RectTransform window, RectTransform mapFrame)
        {
            var shadow = window.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -14f);
            var line = NewImage(window, "HeaderAccent", new Color(1f, 0.82f, 0.2f, 0.9f));
            NLUi.Anchor(line, new Vector2(0f, 1f), new Vector2(36f, -100f), new Vector2(150f, 3f));
            var frameImage = mapFrame.GetComponent<Image>();
            var outline = mapFrame.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.2f, 0.35f);
            outline.effectDistance = new Vector2(1.5f, 1.5f);
            frameImage.color = new Color(0.05f, 0.08f, 0.1f, 1f);
            // Soft vignette on the four edges of the map.
            AddEdge(mapFrame, "VignetteTop", new Vector2(0f, 1f), new Vector2(1f, 1f), 70f, false, false);
            AddEdge(mapFrame, "VignetteBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), 70f, false, true);
            AddEdge(mapFrame, "VignetteLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), 70f, true, true);
            AddEdge(mapFrame, "VignetteRight", new Vector2(1f, 0f), new Vector2(1f, 1f), 70f, true, false);
        }

        private static RectTransform NewImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null) image.sprite = sprite;
            return (RectTransform)go.transform;
        }

        private static Sprite _fadeV, _fadeH;
        private static Sprite FadeSprite(bool vertical)
        {
            if (vertical && _fadeV != null) return _fadeV;
            if (!vertical && _fadeH != null) return _fadeH;
            int n = 32;
            var tex = new Texture2D(vertical ? 1 : n, vertical ? n : 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "MapEdgeFade" };
            for (int i = 0; i < n; i++)
            {
                float a = 0.5f * Mathf.Pow(1f - i / (n - 1f), 2f);
                var c = new Color(0f, 0f, 0f, a);
                if (vertical) tex.SetPixel(0, i, c); else tex.SetPixel(i, 0, c);
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 32f);
            if (vertical) _fadeV = sprite; else _fadeH = sprite;
            return sprite;
        }

        private static void AddEdge(RectTransform frame, string name, Vector2 anchorMin, Vector2 anchorMax, float thickness, bool vertical, bool natural)
        {
            var rt = NewImage(frame, name, Color.white, FadeSprite(!vertical));
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.anchoredPosition = Vector2.zero;
            if (vertical)
            {
                rt.pivot = new Vector2(anchorMin.x, .5f);
                rt.sizeDelta = new Vector2(thickness, 0f);
                rt.localScale = new Vector3(natural ? 1f : -1f, 1f, 1f);
            }
            else
            {
                rt.pivot = new Vector2(.5f, anchorMin.y);
                rt.sizeDelta = new Vector2(0f, thickness);
                rt.localScale = new Vector3(1f, natural ? 1f : -1f, 1f);
            }
        }

        // ── Legend ──
        public static RectTransform Legend(RectTransform window, TMP_FontAsset font, List<(string key, string name, Color color, string glyph)> items)
        {
            var existing = window.Find("Legend");
            if (existing != null) Object.Destroy(existing.gameObject);
            var root = new GameObject("Legend", typeof(RectTransform), typeof(GridLayoutGroup));
            root.transform.SetParent(window, false);
            var rt = (RectTransform)root.transform;
            NLUi.Anchor(rt, new Vector2(0f, 0f), new Vector2(36f, 58f), new Vector2(980f, 52f));
            rt.pivot = new Vector2(0f, 0f);
            var grid = root.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(140f, 24f);
            grid.spacing = new Vector2(0f, 4f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;
            grid.childAlignment = TextAnchor.UpperLeft;
            foreach (var item in items)
            {
                var cell = new GameObject("Legend_" + item.key, typeof(RectTransform)).GetComponent<RectTransform>();
                cell.SetParent(rt, false);
                var dot = NewImage(cell, "Swatch", item.color, UIStyleKit.RoundedSprite());
                dot.GetComponent<Image>().type = Image.Type.Sliced;
                NLUi.Anchor(dot, new Vector2(0f, .5f), new Vector2(11f, 0f), new Vector2(20f, 20f));
                var g = NLUi.Label(dot, "Glyph", item.glyph, 12f, new Color(.08f, .1f, .12f), font, FontStyles.Bold, TextAlignmentOptions.Center);
                NLUi.Stretch(g.rectTransform);
                g.raycastTarget = false;
                var t = NLUi.Label(cell, "Text", item.name, 13.5f, NLUi.Muted, font, FontStyles.Normal, TextAlignmentOptions.Left);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Ellipsis;
                t.raycastTarget = false;
                t.rectTransform.anchorMin = new Vector2(0f, 0f); t.rectTransform.anchorMax = new Vector2(1f, 1f);
                t.rectTransform.offsetMin = new Vector2(26f, 0f); t.rectTransform.offsetMax = new Vector2(-2f, 0f);
            }
            return rt;
        }

        // ── Compass and scale bar (inside the map area) ──
        public static void Compass(RectTransform mapArea, TMP_FontAsset font, float mapW, float mapH)
        {
            var disc = NLUi.Panel(mapArea, "Compass", new Color(0.04f, 0.07f, 0.09f, 0.82f));
            disc.GetComponent<Image>().raycastTarget = false;
            NLUi.Anchor(disc, new Vector2(.5f, .5f), new Vector2(mapW / 2f - 58f, mapH / 2f - 58f), new Vector2(60f, 60f));
            var needle = NewImage(disc, "Needle", new Color(1f, .82f, .2f, 1f));
            NLUi.Anchor(needle, new Vector2(.5f, .5f), new Vector2(0f, -6f), new Vector2(10f, 10f));
            needle.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var n = NLUi.Label(disc, "N", NorthLabel, 17f, Color.white, font, FontStyles.Bold, TextAlignmentOptions.Center);
            n.raycastTarget = false;
            NLUi.Anchor(n.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -3f), new Vector2(40f, 22f));
            n.rectTransform.pivot = new Vector2(.5f, 1f);
        }

        private static readonly float[] NiceMetres = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000 };

        /// <summary>Returns the bar length in metres chosen for the given metres-per-pixel.</summary>
        public static float ScaleBar(RectTransform mapArea, TMP_FontAsset font, float mapW, float mapH, float metresPerPixel)
        {
            float metres = NiceMetres[0];
            foreach (float m in NiceMetres) { if (m / metresPerPixel <= 220f) metres = m; }
            float px = Mathf.Max(24f, metres / metresPerPixel);
            var root = new GameObject("ScaleBar", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(mapArea, false);
            NLUi.Anchor(root, new Vector2(.5f, .5f), new Vector2(-mapW / 2f + 28f + px / 2f, -mapH / 2f + 34f), new Vector2(px, 20f));
            var bg = NewImage(root, "Back", new Color(0.04f, 0.07f, 0.09f, 0.6f));
            NLUi.Stretch(bg, -8f);
            var bar = NewImage(root, "Bar", Color.white);
            NLUi.Anchor(bar, new Vector2(.5f, 0f), new Vector2(0f, 2f), new Vector2(px, 3f));
            foreach (float x in new[] { 0f, 1f })
            {
                var tick = NewImage(root, "Tick", Color.white);
                NLUi.Anchor(tick, new Vector2(x, 0f), new Vector2(0f, 7f), new Vector2(3f, 14f));
            }
            string text = metres >= 1000f ? (metres / 1000f).ToString("0.#") + " km" : metres.ToString("0") + " m";
            var l = NLUi.Label(root, "Text", text, 14f, Color.white, font, FontStyles.Bold, TextAlignmentOptions.Center);
            l.raycastTarget = false;
            l.textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.Anchor(l.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, 0f), new Vector2(120f, 20f));
            l.rectTransform.pivot = new Vector2(.5f, 0f);
            return metres;
        }

        // ── District / area names drawn as soft tinted zones ──
        public static RectTransform District(RectTransform mapArea, TMP_FontAsset font, string name, Vector2 centre, Vector2 size, Color tint, int siblingIndex)
        {
            var zone = NLUi.Panel(mapArea, "District_" + name, new Color(tint.r, tint.g, tint.b, 0.14f));
            zone.GetComponent<Image>().raycastTarget = false;
            NLUi.Anchor(zone, new Vector2(.5f, .5f), centre, size);
            zone.SetSiblingIndex(Mathf.Min(siblingIndex, mapArea.childCount - 1));
            var outline = zone.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(tint.r, tint.g, tint.b, 0.35f);
            outline.effectDistance = new Vector2(1.2f, 1.2f);
            var l = NLUi.Label(zone, "Name", name.ToUpperInvariant(), 13f, new Color(Mathf.Lerp(tint.r, 1f, .55f), Mathf.Lerp(tint.g, 1f, .55f), Mathf.Lerp(tint.b, 1f, .55f), .95f),
                font, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            l.raycastTarget = false;
            l.characterSpacing = 4f;
            l.textWrappingMode = TextWrappingModes.NoWrap;
            l.rectTransform.anchorMin = new Vector2(0f, 1f); l.rectTransform.anchorMax = new Vector2(1f, 1f);
            l.rectTransform.pivot = new Vector2(0f, 1f);
            l.rectTransform.anchoredPosition = new Vector2(8f, -4f);
            l.rectTransform.sizeDelta = new Vector2(-12f, 20f);
            return zone;
        }

        public static void SeaLabel(RectTransform mapArea, TMP_FontAsset font, string text, Vector2 centre)
        {
            var l = NLUi.Label(mapArea, "District_Sea", text, 17f, new Color(.62f, .82f, .95f, .55f), font, FontStyles.Italic, TextAlignmentOptions.Center);
            l.raycastTarget = false;
            l.characterSpacing = 8f;
            l.textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.Anchor(l.rectTransform, new Vector2(.5f, .5f), centre, new Vector2(420f, 28f));
        }
    }
}