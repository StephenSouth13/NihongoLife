using UnityEngine;

namespace NihongoLife.Interaction
{
    public interface IInteractable
    {
        string GetPromptJa();
        string GetpromptEn();
        void Interact(GameObject player);
        Transform GetTransform();
    }

    /// <summary>The prompt shown for an interactable: only the interface language (Japanese prompt in the Japanese UI).</summary>
    public static class InteractionPrompt
    {
        public static string Of(IInteractable interactable)
        {
            if (interactable == null) return "";
            string ja = interactable.GetPromptJa();
            if (NihongoLife.Core.Loc.IsJapanese && !string.IsNullOrEmpty(ja)) return ja;
            string local = NihongoLife.Core.Loc.Resolve(interactable.GetpromptEn());
            if (!string.IsNullOrEmpty(local) && Known.TryGetValue(local.Trim(), out var known)) return NihongoLife.Core.Loc.T(known.vi, known.en, known.ja);
            return string.IsNullOrEmpty(local) ? ja ?? "" : local;
        }

        /// <summary>Prompts typed into scenes and prefabs (some English, some Vietnamese, some without accents).</summary>
        private static readonly System.Collections.Generic.Dictionary<string, (string vi, string en, string ja)> Known = new System.Collections.Generic.Dictionary<string, (string vi, string en, string ja)>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Talk"] = ("Nói chuyện", "Talk", "はなす"),
            ["Đi vào"] = ("Đi vào", "Go in", "入る"),
            ["Xuong tau"] = ("Xuống tàu", "Get off the train", "でんしゃを おりる"),
            ["Validate ticket"] = ("Qua cổng soát vé", "Go through the ticket gate", "かいさつを とおる"),
            ["Qua cong soat ve"] = ("Qua cổng soát vé", "Go through the ticket gate", "かいさつを とおる"),
            ["Practice phrases"] = ("Luyện câu", "Practise phrases", "フレーズを練習する"),
            ["Mua ve"] = ("Mua vé", "Buy a ticket", "きっぷを かう"),
            ["Buy ticket"] = ("Mua vé", "Buy a ticket", "きっぷを かう"),
            ["Len tau"] = ("Lên tàu", "Board the train", "でんしゃに のる"),
            ["Bat dau hanh trinh"] = ("Bắt đầu hành trình", "Start the journey", "出発する"),
            ["Xem thực đơn"] = ("Xem thực đơn", "See the menu", "メニューを見る"),
            ["Vào trường"] = ("Vào trường", "Enter the school", "学校に入る"),
            ["Vào quán sushi"] = ("Vào quán sushi", "Enter the sushi restaurant", "おすし屋さんに入る"),
            ["Vào ga tàu"] = ("Vào ga tàu", "Enter the station", "駅に入る"),
            ["Vào Game Center"] = ("Vào Game Center", "Enter the Game Center", "ゲームセンターに入る"),
            ["Về phòng trọ"] = ("Về phòng trọ", "Go home to your room", "部屋に帰る"),
            ["Thanh toán với chị Ito"] = ("Thanh toán với chị Ito", "Pay Ito at the register", "レジで いとうさんに払う"),
            ["Ra phố"] = ("Ra phố", "Go out to the street", "外に出る"),
            ["Ra phố (về Hibari-chō)"] = ("Ra phố (về Hibari-chō)", "Go out (back to Hibari-chō)", "外に出る（ひばり町へ）"),
            ["Mua vé với anh Kimura"] = ("Mua vé với anh Kimura", "Buy a ticket from Kimura", "木村さんから きっぷを かう"),
            ["Mở cửa"] = ("Mở cửa", "Open the door", "ドアを開ける"),
            ["Đóng cửa"] = ("Đóng cửa", "Close the door", "ドアを閉める"),
            ["Mở tủ lạnh"] = ("Mở tủ lạnh", "Open the fridge", "冷蔵庫を開ける"),
            ["Đóng tủ lạnh"] = ("Đóng tủ lạnh", "Close the fridge", "冷蔵庫を閉める"),
            ["Lấy cơm nắm"] = ("Lấy cơm nắm", "Take a rice ball", "おにぎりを取る"),
            ["Kiểm tra trà xanh"] = ("Kiểm tra trà xanh", "Check the green tea", "お茶を見る"),
            ["Kiểm tra nước"] = ("Kiểm tra nước", "Check the water", "水を見る"),
            ["Kiểm tra"] = ("Kiểm tra", "Inspect", "調べる"),
            ["Mua hàng tự do"] = ("Mua hàng tự do", "Shop freely", "自由に買い物する"),
            ["Trở lại thành phố"] = ("Trở lại thành phố", "Back to town", "町へ戻る"),
        };
    }

    /// <summary>Optional bonus (in metres) when several interactables are in reach; doors use it.</summary>
    public interface IInteractionPriority
    {
        float InteractionPriority { get; }
    }

    public interface IConditionalInteractable
    {
        bool IsInteractionAvailable { get; }
    }
}
