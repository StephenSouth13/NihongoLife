using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Reads what is really on screen and in the world of every area, in English, and lists Vietnamese left behind. The list is
    /// also written to Bao_Cao/lang-leak/zones.txt. Vietnamese has letters no other language of the game uses, so a leak cannot
    /// hide; a text that names a place in Vietnam (Phú Quốc) on purpose is allowed by <see cref="Allowed"/>.
    /// </summary>
    public class ZoneLanguagePlayModeTests
    {
        private static readonly Regex Vietnamese = new Regex("[ăâđêôơưàáảãạằắẳẵặầấẩẫậèéẻẽẹềếểễệìíỉĩịòóỏõọồốổỗộờớởỡợùúủũụừứửữựỳýỷỹỵ]", RegexOptions.IgnoreCase);

        private static readonly string[][] Zones =
        {
            new[] { "20_StationDistrict", "station_entrance" }, new[] { "30_SushiRestaurant", "sushi_entrance" },
            new[] { "40_ HIBARICLASS", "school_entrance" }, new[] { "45_HomeBedroom", "home_bedroom" },
            new[] { "50_GameCenter", "game_center_entrance" }, new[] { "35_HibariClinic", "clinic_entrance" },
            new[] { "55_MusicShop", "music_shop_entrance" }, new[] { "56_FashionStore", "fashion_store_entrance" }, new[] { "60_MidoriIsland", "midori_station" },
            new[] { "65_PhuQuocIsland", "phuquoc_ganh_ngon" },
        };

        private static bool Allowed(string text) => Regex.IsMatch(text, "Phú Quốc|Gánh|Gành|Dương Đông|Hàm Ninh|An Thới|Hòn Thơm|Bãi|Vinh|Việt|Dinh Cậu|DINH CẬU|Chùa|Chợ|Cửa Cạn|Rạch Vẹm|Suối|Núi Chúa|Khải Hoàn|Hoàng Hôn|Hàng Dừa|Biển|Hải Âu|Ngọc|Sao|Cézanne|Velázquez|Manet|Hòa|Hưng Đạo|Đức Thạnh|Tiếng Việt");

        private static void Read(string where, HashSet<string> found)
        {
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (string.IsNullOrWhiteSpace(text.text) || !text.gameObject.activeInHierarchy || text.name == "Bubble") continue; // the crowd of Phu Quoc greets in many languages on purpose
                string clean = Regex.Replace(text.text, "<[^>]+>", "");
                if (!Vietnamese.IsMatch(clean) || Allowed(clean)) continue;
                found.Add(where + " | " + text.name + " | " + clean.Replace("\n", " / "));
            }
        }

        [UnityTest, Timeout(420000)]
        public IEnumerator EveryArea_InEnglish_ShowsNoVietnamese()
        {
            LogAssert.ignoreFailingMessages = true;
            Loc.Set(GameLanguage.English);
            try
            {
                yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                yield return new WaitForSecondsRealtime(2.5f);
                var found = new HashSet<string>();
                Read("town", found);
                var flow = Object.FindFirstObjectByType<SceneFlowController>();
                foreach (var zone in Zones)
                {
                    flow.TransferZone(zone[0], zone[1], zone[0]);
                    for (float t = 0f; t < 30f && (flow.IsLoading || SceneManager.GetActiveScene().name != zone[0]); t += Time.unscaledDeltaTime) yield return null;
                    yield return new WaitForSecondsRealtime(1.5f);
                    Read(zone[0], found);
                }
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/lang-leak"));
                Directory.CreateDirectory(dir);
                File.WriteAllLines(Path.Combine(dir, "zones.txt"), found.OrderBy(x => x), new UTF8Encoding(false));
                Assert.IsEmpty(found, string.Join("\n", found.OrderBy(x => x).Take(30)));
            }
            finally
            {
                Loc.Set(GameLanguage.Vietnamese);
            }
        }
    }
}
