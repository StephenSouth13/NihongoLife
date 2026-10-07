using System;
using System.Collections.Generic;

namespace NihongoLife.Core
{
    public static class WorldLocationCatalog
    {
        public const string CityScene = "90_TestSandbox";
        public const string StationScene = "20_StationDistrict";
        public const string SushiRestaurantScene = "30_SushiRestaurant";
        public const string SchoolScene = "40_ HIBARICLASS";
        public const string HomeBedroomScene = "45_HomeBedroom";
        public const string GameCenterScene = "50_GameCenter";

        public const string StationEntrance = "station_entrance";
        public const string SushiEntrance = "sushi_entrance";
        public const string SchoolEntrance = "school_entrance";
        public const string HomeBedroomEntrance = "home_bedroom";
        public const string GameCenterEntrance = "game_center_entrance";
        public const string CityGameCenterReturn = "city_game_center_return";
        public const string CityStationReturn = "city_station_return";
        public const string CitySushiReturn = "city_sushi_return";
        public const string CitySchoolReturn = "city_school_return";
        public const string CityHomeBedroomReturn = "city_home_return";

        private static readonly Dictionary<string, LocationInfo> Locations =
            new Dictionary<string, LocationInfo>(StringComparer.OrdinalIgnoreCase)
            {
                [CityScene] = new LocationInfo("nihongo_city", "Nihongo City", "Thành phố Hibari", "ひばり町"),
                [StationScene] = new LocationInfo("ekimae", "Ekimae Station District", "Ga Hibari", "ひばり駅"),
                [SushiRestaurantScene] = new LocationInfo("sushi_hibari", "Sushi Hibari", "Nhà hàng Sushi Hibari", "ひばり寿司"),
                [SchoolScene] = new LocationInfo("hibari_school", "Hibari Japanese School", "Trường Nhật ngữ Hibari", "ひばり日本語学院"),
                [GameCenterScene] = new LocationInfo("game_center", "Game Center Hibari", "Trung tâm trò chơi Hibari", "ゲームセンター"),
                [HomeBedroomScene] = new LocationInfo("home_bedroom", "Your Bedroom", "Phòng trọ của bạn", "自室")
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
