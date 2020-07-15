using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.MainGame;

namespace KK_Pregnancy
{
    public static class PregnancyDataUtils
    {
        /// <param name="c">ChaFile to test</param>
        /// <param name="afterWasDiscovered">The girl knows about it / tested it</param>
        public static int GetPregnancyWeek(this ChaFileControl c)
        {
            if (c == null) return 0;

            var d = ExtendedSave.GetExtendedDataById(c, PregnancyPlugin.GUID);
            if (d == null) return 0;

            return PregnancyData.Load(d).Week;
        }

        /// <param name="heroine">Heroine to test</param>
        /// <param name="afterWasDiscovered">The girl knows about it / tested it</param>
        public static int GetPregnancyWeek(this SaveData.Heroine heroine)
        {
            if (heroine == null) return 0;

            return heroine.GetRelatedChaFiles().Max(GetPregnancyWeek);
        }

        public static HeroineStatus GetHeroineStatus(this SaveData.Heroine heroine)
        {
            if (heroine == null) return HeroineStatus.Unknown;

            // Check if she wants to tell
            if (heroine.intimacy >= 80 ||
                heroine.hCount >= 5 ||
                heroine.parameter.attribute.bitch && heroine.favor > 50 ||
                (heroine.isGirlfriend || heroine.favor >= 90) &&
                (!heroine.isVirgin || heroine.hCount >= 2 || heroine.intimacy >= 40))
            {
                var pregnancyWeek = heroine.GetPregnancyWeek();
                if (PregnancyPlugin.ShowPregnancyIconEarly.Value ? pregnancyWeek > 0 : pregnancyWeek > 1
                ) //todo show it later
                    return HeroineStatus.Pregnant;

                return HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.安全日
                    ? HeroineStatus.Safe
                    : HeroineStatus.Risky;
            }

            return HeroineStatus.Unknown;
        }
    }
}