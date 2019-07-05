using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
using UnityEngine;

namespace KK_Pregnancy
{
    public static class PregnancyDataUtils
    {
        /// <param name="c">ChaFile to test</param>
        /// <param name="afterWasDiscovered">The girl knows about it / tested it</param>
        public static bool IsChaFilePregnant(this ChaFileControl c, bool afterWasDiscovered)
        {
            if (c == null) return false;

            var d = ExtendedSave.GetExtendedDataById(c, PregnancyPlugin.GUID);
            if (d == null) return false;

            DeserializeData(d, out var week, out var _, out var _);
            return afterWasDiscovered ? week > 1 : week > 0;
        }

        /// <param name="heroine">Heroine to test</param>
        /// <param name="afterWasDiscovered">The girl knows about it / tested it</param>
        public static bool IsHeroinePregnant(this SaveData.Heroine heroine, bool afterWasDiscovered)
        {
            if (heroine == null) return false;

            return heroine.GetRelatedChaFiles().Any(control => IsChaFilePregnant(control, afterWasDiscovered));
        }

        public static void DeserializeData(PluginData data, out int week, out bool gameplayEnabled, out float fertility)
        {
            week = 0;
            gameplayEnabled = true;
            fertility = DefaultFertility;

            if (data?.data == null) return;

            if (data.data.TryGetValue("Week", out var value) && value is int w) week = w;
            if (data.data.TryGetValue("GameplayEnabled", out var value2) && value2 is bool g) gameplayEnabled = g;
            if (data.data.TryGetValue("Fertility", out var value3) && value3 is float f) fertility = f;
        }

        public static PluginData SerializeData(int week, bool gameplayEnabled, float fertility)
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
