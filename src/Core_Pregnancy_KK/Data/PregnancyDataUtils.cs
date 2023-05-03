using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
using UnityEngine;
using static ActionGame.Cycle;

namespace KK_Pregnancy
{
    public static class PregnancyDataUtils
    {
        private static readonly int[] _earlyDetectPersonalities = { 00, 11, 12, 13, 19, 24, 31, 33 };
        private static readonly int[] _lateDetectPersonalities = { 03, 05, 08, 20, 25, 26, 37 };

        /// <param name="c">ChaFile to test</param>
        ///// <param name="afterWasDiscovered">The girl knows about it / tested it</param>
        public static PregnancyData GetPregnancyData(this ChaFileControl c)
        {
            if (c == null) return null;

            var d = ExtendedSave.GetExtendedDataById(c, PregnancyPlugin.GUID);
            if (d == null) return null;

            return PregnancyData.Load(d);
        }

        /// <param name="heroine">Heroine to test</param>
        ///// <param name="afterWasDiscovered">The girl knows about it / tested it</param>
        [Obsolete]
        public static PregnancyData GetPregnancyData(SaveData.Heroine heroine) => GetPregnancyData((SaveData.CharaData)heroine);
        public static PregnancyData GetPregnancyData(this SaveData.CharaData chara)
        {
            if (chara == null) return new PregnancyData();

            // Figure out which data to take if there are multiple
            // probably not necessary? null check should be enough? 
            //return chara.GetRelatedChaFiles()
            //    .Select(GetPregnancyData)
            //    .Where(x => x != null)
            //    .OrderByDescending(x => x.PregnancyCount)
            //    .ThenByDescending(x => x.WeeksSinceLastPregnancy)
            //    .ThenByDescending(x => x.Week)
            //    .ThenByDescending(x => x.MenstruationSchedule)
            //    .ThenByDescending(x => x.GameplayEnabled)
            //    .FirstOrDefault() ?? new PregnancyData();
            return GetPregnancyData(chara.charFile) ?? new PregnancyData();
        }

        [Obsolete]
        public static HeroineStatus GetHeroineStatus(this SaveData.CharaData chara, PregnancyData pregData = null) => GetCharaStatus(chara, pregData);
        public static HeroineStatus GetCharaStatus(this SaveData.CharaData chara, PregnancyData pregData = null)
        {
            if (chara is SaveData.Heroine heroine)
            {
                if (pregData == null) pregData = heroine.GetPregnancyData();

                if (CanShowStatus(heroine))
                {
                    var pregnancyWeek = pregData.Week;
                    if (pregnancyWeek > 0)
                    {
                        if (pregnancyWeek >= PregnancyData.LeaveSchoolWeek) return HeroineStatus.OnLeave;
                        if (PregnancyPlugin.ShowPregnancyIconEarly.Value) return HeroineStatus.Pregnant;
                        // Different personalities notice at different times
                        if (_earlyDetectPersonalities.Contains(heroine.personality))
                        {
                            if (pregnancyWeek > 1) return HeroineStatus.Pregnant;
                        }
                        else if (_lateDetectPersonalities.Contains(heroine.personality))
                        {
                            if (pregnancyWeek > 11) return HeroineStatus.Pregnant;
                        }
                        else
                        {
                            if (pregnancyWeek > 5) return HeroineStatus.Pregnant;
                        }
                    }

                    return HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.安全日
                        ? HeroineStatus.Safe
                        : HeroineStatus.Risky;
                }
            }
            else if (chara is SaveData.Player player)
            {
                if (pregData == null) pregData = player.GetPregnancyData();
                return pregData.IsPregnant ? HeroineStatus.Pregnant : HeroineStatus.Safe;
            }
            return HeroineStatus.Unknown;
        }

        private static bool CanShowStatus(SaveData.Heroine heroine)
        {
            // todo add option to ask in talk scene?
            switch (PregnancyPlugin.StatusDisplay.Value)
            {
                case PregnancyPlugin.StatusDisplayCondition.Always:
                    return true;
                case PregnancyPlugin.StatusDisplayCondition.Normal:
#if KK
                    return heroine.intimacy >= 80 ||
                           heroine.hCount >= 5 ||
                           heroine.parameter.attribute.bitch && heroine.favor > 50 ||
                           (heroine.isGirlfriend || heroine.favor >= 90) &&
                           (!heroine.isVirgin || heroine.hCount >= 2 || heroine.intimacy >= 40);
#else
                    return heroine.relation >= 3 ||
                           heroine.hCount >= 5 ||
                           heroine.parameter.attribute.bitch && heroine.favor > 50 ||
                           heroine.isGirlfriend && (!heroine.isVirgin || heroine.hCount >= 2 || heroine.favor >= 120);
#endif
                case PregnancyPlugin.StatusDisplayCondition.OnlyGirlfriend:
#if KK
                    return heroine.isGirlfriend && (!heroine.isVirgin || heroine.hCount >= 2 || heroine.intimacy >= 40);
#else
                    return heroine.isGirlfriend && (!heroine.isVirgin || heroine.hCount >= 2 || heroine.favor >= 120);
#endif
                case PregnancyPlugin.StatusDisplayCondition.Never:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static IEnumerable<ChaFileControl> GetRelatedChaFiles(this SaveData.CharaData character)
        {
            var chafiles =
                character is SaveData.Heroine h ? KKAPI.MainGame.GameExtensions.GetRelatedChaFiles(h) :
                character is SaveData.Player p ? KKAPI.MainGame.GameExtensions.GetRelatedChaFiles(p) :
                null;
            return chafiles;
        }

        /// <summary>
        /// Get character's current fertility. Can be affected by plugin config and other plugins.
        /// 0-1 range
        /// </summary>
        public static float GetFertility(SaveData.CharaData character)
        {
            return Mathf.Max(PregnancyPlugin.FertilityOverride.Value, character != null ? character.GetPregnancyData().Fertility : 0.3f);
        }

        /// <summary>
        /// Get character's current MenstruationSchedule. Can be affected by plugin config and other plugins.
        /// </summary>
        public static MenstruationSchedule GetMenstruation(SaveData.CharaData character)
        {
            return character != null ? character.GetPregnancyData().MenstruationSchedule : MenstruationSchedule.Default;

            // old version
            //return heroine.GetRelatedChaFiles()
            //            .Select(c => PregnancyData.Load(ExtendedSave.GetExtendedDataById(c, GUID))?.MenstruationSchedule ?? MenstruationSchedule.Default)
            //            .FirstOrDefault(x => x != MenstruationSchedule.Default);
        }

        /// <summary>
        /// Get speed modifier of character's pergenency. Can be affected by plugin config and other plugins.
        /// </summary>
        public static int GetPregnancyProgressionSpeed(SaveData.CharaData character)
        {
            // todo add setting for individual chara speed (multiplier? slow normal fast for 1.5, 1, 0.5? only toggle for faster? slower won't work if config is set to 1)
            return PregnancyPlugin.PregnancyProgressionSpeed.Value;
        }
    }
}