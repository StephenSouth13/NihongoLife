namespace NihongoLife.Core
{
    /// <summary>
    /// Who may go online: a registered account that is signed in. A guest (no sign-in, or an anonymous session without
    /// an email) plays the whole game — story, map roadmap, school, exams — offline with a local save, and is invited
    /// to register when it reaches for an online feature (online world, friends, chat, co-op, voice, Speaking room,
    /// island visits, ranking submissions).
    /// </summary>
    public static class OnlineAccess
    {
        /// <summary>Tests that drive the online world from a test device (no account) set this; never set in the game.</summary>
        public static bool Bypass;

        public static bool IsRegistered => Bypass ||
            GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated && !string.IsNullOrWhiteSpace(auth.Email);

        public static string Reason => Loc.T(
            "Chế độ Khách chơi đầy đủ nhưng ngoại tuyến. Đăng ký tài khoản rồi đăng nhập để chơi online, kết bạn, chat và học cùng nhau.",
            "Guest mode plays the whole game offline. Register an account and sign in to play online, add friends, chat and study together.",
            "ゲストはオフラインで全部遊べます。オンライン・フレンド・チャットはアカウント登録してログインしてください。");

        /// <summary>True when online is allowed; otherwise tells the player why in the HUD feed.</summary>
        public static bool Check()
        {
            if (IsRegistered) return true;
            NihongoLife.UI.HudFeed.Post(Reason, NihongoLife.UI.HudFeed.Kind.Warning, 6f, "online");
            return false;
        }
    }
}
