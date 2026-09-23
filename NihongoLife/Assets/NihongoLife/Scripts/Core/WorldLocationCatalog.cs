using System;
using System.Collections.Generic;

namespace NihongoLife.Core
{
    public static class WorldLocationCatalog
    {
        public const string CityScene = "90_TestSandbox";
        public const string StationScene = "20_StationDistrict";
        public const string SushiRestaurantScene = "30_SushiRestaurant";
        public const string ShoppingDistrictScene = "40_ShoppingDistrict";
        public const string LearningCenterScene = "50_LearningCenter";

        public const string StationEntrance = "station_entrance";
        public const string SushiEntrance = "sushi_entrance";
        public const string CityStationReturn = "city_station_return";
        public const string CitySushiReturn = "city_sushi_return";
        public const string ShoppingEntrance = "shopping_entrance";
        public const string LearningEntrance = "learning_entrance";

        private static readonly Dictionary<string, LocationInfo> Locations =
            new Dictionary<string, LocationInfo>(StringComparer.OrdinalIgnoreCase)
            {
                [CityScene] = new LocationInfo("nihongo_city", "Nihongo City", "Thanh pho Nihongo", "日本の町"),
                [StationScene] = new LocationInfo("ekimae", "Ekimae Station District", "Khu nha ga Ekimae", "駅前"),
                [SushiRestaurantScene] = new LocationInfo("sushi_hibari", "Sushi Hibari", "Nha hang Sushi Hibari", "ひばり寿司"),
                [ShoppingDistrictScene] = new LocationInfo("shopping_district", "Nihongo Market", "Khu mua sam Nihongo", "ショッピング街")
                , [LearningCenterScene] = new LocationInfo("learning_center", "Learning Center", "Trung tam hoc tap", "学習センター")
            };

        public static LocationInfo Get(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) && Locations.TryGetValue(sceneName, out LocationInfo location)
                ? location
                : new LocationInfo(sceneName ?? string.Empty, sceneName ?? string.Empty, sceneName ?? string.Empty, sceneName ?? string.Empty);
        }
    }

    public readonly struct LocationInfo
    {
        public LocationInfo(string id, string englishName, string vietnameseName, string japaneseName)
        {
            Id = id;
            EnglishName = englishName;
            VietnameseName = vietnameseName;
            JapaneseName = japaneseName;
        }

        public string Id { get; }
        public string EnglishName { get; }
        public string VietnameseName { get; }
        public string JapaneseName { get; }
        public string DisplayName => $"{JapaneseName} / {VietnameseName}";
    }
}
