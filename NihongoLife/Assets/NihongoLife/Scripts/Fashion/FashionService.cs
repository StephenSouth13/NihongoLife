using System;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Save;

namespace NihongoLife.Fashion
{
    /// <summary>
    /// The player's clothes. Owned outfits and the one being worn live in the account's save (bought with game money,
    /// so they follow the account, unlike the device's hair colour). Trying on only changes what is shown: leaving the
    /// mirror goes back to the outfit actually worn.
    /// </summary>
    public static class FashionService
    {
        private static string _tryOn;

        public static event Action Changed;

        private static PlayerProgressDto Progress =>
            GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;

        /// <summary>The outfit in the save; empty = the character's own clothes.</summary>
        public static string WornId => Progress?.outfit ?? string.Empty;

        /// <summary>The outfit on the character right now, a try-on included.</summary>
        public static string ShownId => _tryOn ?? WornId;

        public static LookOutfit Shown => FashionCatalog.Load().Look(ShownId);

        public static bool Owns(string id) => string.IsNullOrEmpty(id) || Progress?.outfits?.Contains(id) == true;

        public static void TryOn(string id)
        {
            _tryOn = id ?? string.Empty;
            CharacterLook.Touch();
        }

        public static void EndTryOn()
        {
            if (_tryOn == null) return;
            _tryOn = null;
            CharacterLook.Touch();
        }

        /// <summary>Buys an outfit; null on success, else why not.</summary>
        public static string Buy(string id)
        {
            var outfit = FashionCatalog.Load().Outfit(id);
            var progress = Progress;
            if (outfit == null || progress == null) return Loc.T("Không có bộ này.", "That outfit is not available.", "その服はありません。");
            if (Owns(id)) return null;
            var wallet = PlayerInventory.Instance;
            string reason = Loc.Triple("Mua quần áo", "Clothes", "服の買い物");
            if (wallet != null)
            {
                if (!wallet.SpendYen(outfit.price, reason))
                    return Loc.F("Chưa đủ tiền: cần {0}.", "Not enough money: {0} needed.", "お金が足りません：{0}", Loc.Yen(outfit.price));
            }
            else if (progress.yen >= outfit.price) progress.yen -= outfit.price;
            else return Loc.F("Chưa đủ tiền: cần {0}.", "Not enough money: {0} needed.", "お金が足りません：{0}", Loc.Yen(outfit.price));
            progress.outfits.Add(id);
            Save(progress);
            return null;
        }

        /// <summary>Wears an owned outfit; empty = the character's own clothes.</summary>
        public static bool Wear(string id)
        {
            var progress = Progress;
            if (progress == null || !Owns(id)) return false;
            progress.outfit = id ?? string.Empty;
            _tryOn = null;
            Save(progress);
            CharacterLook.Touch();
            return true;
        }

        private static void Save(PlayerProgressDto progress)
        {
            if (GameServices.TryGet(out IProgressRepository repository)) repository.SaveProgress(progress);
            Changed?.Invoke();
        }

        /// <summary>Tests: nothing left on from a try-on.</summary>
        public static void ResetForTests() => _tryOn = null;
    }
}
