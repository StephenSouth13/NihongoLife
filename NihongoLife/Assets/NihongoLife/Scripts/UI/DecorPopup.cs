using NihongoLife.Core;
using NihongoLife.Home;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>F7: the decoration catalogue (key X in the bedroom): buy pieces, move or sell what is already in the room.</summary>
    public sealed class DecorPopup : MenuPopupBase
    {
        private const float W = 940f, H = 640f;
        private RectTransform _rows; private TextMeshProUGUI _wallet, _message;
        public string RowsText { get; private set; } = "";
        public UnityEngine.UI.Button FirstBuyButton { get; private set; }

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);
        protected override void GetTitle(out string vi, out string en, out string ja) { vi = "TRANG TRÍ PHÒNG"; en = "ROOM DECORATION"; ja = "部屋の模様替え"; }

        public static DecorPopup GetOrCreate(GameObject host, TMP_FontAsset font)
        {
            var ui = host.GetComponent<DecorPopup>(); if (ui == null) ui = host.AddComponent<DecorPopup>();
            ui.Initialize(font); return ui;
        }

        protected override void Build(RectTransform card)
        {
            _wallet = UiKit.Label(card, "Wallet", "", 20f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.MidlineRight, false);
            UiKit.Place(_wallet.rectTransform, W - 360f, 28f, 250f, 36f);
            _message = UiKit.Label(card, "Message", "", 16f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, false);
            UiKit.Place(_message.rectTransform, 36f, H - 44f, W - 72f, 30f);
            _rows = UiKit.Rect(card, "Rows"); UiKit.Place(_rows, 24f, 104f, W - 48f, H - 156f);
        }

        protected override void OnOpened() => Rebuild();
        protected override void OnLanguageApplied() { if (IsOpen) Rebuild(); }

        private void Say(string s) { if (_message != null) _message.text = s; }

        private void Rebuild()
        {
            if (_rows == null) return;
            UiKit.Clear(_rows);
            var sb = new System.Text.StringBuilder(); var rd = RoomDecor.Instance;
            int yen = PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;
            _wallet.text = Loc.Yen(yen);
            var catHead = UiKit.Label(_rows, "CatHead", Loc.T("Cửa hàng nội thất", "Furniture shop", "家具ショップ"), 19f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
            UiKit.Place(catHead.rectTransform, 0f, 0f, 420f, 30f);
            float y = 36f; FirstBuyButton = null;
            foreach (var d in RoomDecorCatalog.All)
            {
                var def = d;
                var name = UiKit.Label(_rows, "N_" + def.Id, Loc.T(def.Vi, def.En, def.Ja), 18f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, false);
                UiKit.Place(name.rectTransform, 0f, y, 200f, 44f);
                var buy = UiKit.Button(_rows, "Buy_" + def.Id, Loc.Yen(def.Price), () => { if (rd != null && rd.Buy(def.Id)) return; Say(rd != null ? rd.LastMessage : ""); Rebuild(); },
                    yen >= def.Price ? UiKit.Kind.Primary : UiKit.Kind.Quiet, null, 16f);
                UiKit.Place((RectTransform)buy.transform, 210f, y, 170f, 44f);
                if (FirstBuyButton == null) FirstBuyButton = buy;
                sb.AppendLine(name.text + " " + Loc.Yen(def.Price));
                y += 52f;
            }
            var placedHead = UiKit.Label(_rows, "PlacedHead", Loc.T("Đồ trong phòng", "In your room", "部屋にある家具"), 19f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
            UiKit.Place(placedHead.rectTransform, 450f, 0f, 420f, 30f);
            y = 36f;
            if (rd == null || rd.Placed.Count == 0)
            {
                var none = UiKit.Label(_rows, "Empty", Loc.T("Chưa có món nào. Mua bên trái rồi đặt vào phòng.", "Nothing yet. Buy on the left, then place it in the room.", "まだありません。左で買って部屋に置きましょう。"), 16f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
                UiKit.Place(none.rectTransform, 450f, y, 420f, 60f); sb.AppendLine(none.text);
            }
            else for (int i = 0; i < rd.Placed.Count && i < 7; i++)
            {
                int idx = i; var def = rd.Placed[i].Def;
                var name = UiKit.Label(_rows, "P_" + i, Loc.T(def.Vi, def.En, def.Ja), 17f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, false);
                UiKit.Place(name.rectTransform, 450f, y, 150f, 44f);
                var mv = UiKit.Button(_rows, "Move_" + i, Loc.T("Dời", "Move", "移動"), () => { if (rd.StartMove(idx)) return; Rebuild(); }, UiKit.Kind.Secondary, null, 15f);
                UiKit.Place((RectTransform)mv.transform, 606f, y, 90f, 44f);
                var sell = UiKit.Button(_rows, "Sell_" + i, Loc.T("Bán ", "Sell ", "売る ") + Loc.Yen(rd.SellValue(def)), () => { rd.Sell(idx); Say(rd.LastMessage); Rebuild(); }, UiKit.Kind.Danger, null, 15f);
                UiKit.Place((RectTransform)sell.transform, 702f, y, 168f, 44f);
                sb.AppendLine(name.text); y += 52f;
            }
            RowsText = sb.ToString();
        }
    }
}