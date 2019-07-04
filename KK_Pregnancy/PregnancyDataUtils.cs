using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
using UnityEngine;

namespace KK_Pregnancy
{
    public static class PregnancyDataUtils
    {
        public static bool IsChaFilePregnant(this ChaFileControl c)
        {
            if (c == null) return false;

            var d = ExtendedSave.GetExtendedDataById(c, PregnancyPlugin.GUID);
            if (d == null) return false;

            ParseData(d, out var week, out var _, out var _);
            return week > 0;
        }

        public static bool IsHeroinePregnant(this SaveData.Heroine heroine)
        {
            if (heroine == null) return false;

            return heroine.GetRelatedChaFiles().Any(IsChaFilePregnant);
        }

        public static void ParseData(PluginData data, out int week, out bool gameplayEnabled, out float fertility)
        {
            week = 0;
            gameplayEnabled = true;
            fertility = DefaultFertility;

            if (data?.data == null) return;

            if (data.data.TryGetValue("Week", out var value) && value is int w) week = w;
            if (data.data.TryGetValue("GameplayEnabled", out var value2) && value2 is bool g) gameplayEnabled = g;
            if (data.data.TryGetValue("Fertility", out var value3) && value3 is float f) fertility = f;
        }

        public static PluginData WriteData(int week, bool gameplayEnabled, float fertility)
        {
            if (week <= 0 && gameplayEnabled && Mathf.Approximately(fertility, DefaultFertility)) return null;

            var data = new PluginData
            {
                version = 1,
                data =
                {
                    ["Week"] = week,
                    ["GameplayEnabled"] = gameplayEnabled,
                    ["Fertility"] = fertility
                }
            };
            return data;
        }

        public static readonly float DefaultFertility = 0.3f;

        /// <summary>
        /// Week at which pregnancy reaches max level and the girl leaves school
        /// </summary>
        public static readonly int LeaveSchoolWeek = 41;

        /// <summary>
        /// Week at which pregnancy ends and the girl returns to school
        /// </summary>
        public static readonly int ReturnToSchoolWeek = LeaveSchoolWeek + 7;
    }
}
