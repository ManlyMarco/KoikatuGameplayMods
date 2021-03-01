using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
#if AI
    using AIChara;
    using AIProject;
    using AIProject.SaveData;
#endif

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
        #if KK

            [Obsolete]
            public static PregnancyData GetPregnancyData(SaveData.Heroine heroine) => GetPregnancyData((SaveData.CharaData)heroine);
            public static PregnancyData GetPregnancyData(this SaveData.CharaData chara)
            {
                if (chara == null) return new PregnancyData();

                // Figure out which data to take if there are multiple
                // probably not necessary? null check should be enough? 
                return chara.GetRelatedChaFiles()
                    .Select(GetPregnancyData)
                    .Where(x => x != null)
                    .OrderByDescending(x => x.PregnancyCount)
                    .ThenByDescending(x => x.WeeksSinceLastPregnancy)
                    .ThenByDescending(x => x.Week)
                    .ThenByDescending(x => x.MenstruationSchedule)
                    .ThenByDescending(x => x.GameplayEnabled)
                    .FirstOrDefault() ?? new PregnancyData();
            }

            [Obsolete]
            public static HeroineStatus GetHeroineStatus(this SaveData.CharaData chara, PregnancyData pregData = null) => GetCharaStatus(chara, pregData);
            public static HeroineStatus GetCharaStatus(this SaveData.CharaData chara, PregnancyData pregData = null)
            {
                if (chara is SaveData.Heroine heroine)
                {
                    if (pregData == null) pregData = heroine.GetPregnancyData();

                    // Check if she wants to tell
                    if (heroine.intimacy >= 80 ||
                        heroine.hCount >= 5 ||
                        heroine.parameter.attribute.bitch && heroine.favor > 50 ||
                        (heroine.isGirlfriend || heroine.favor >= 90) &&
                        (!heroine.isVirgin || heroine.hCount >= 2 || heroine.intimacy >= 40))
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

            internal static IEnumerable<ChaFileControl> GetRelatedChaFiles(this SaveData.CharaData character)
            {
                var chafiles =
                    character is SaveData.Heroine h ? KKAPI.MainGame.GameExtensions.GetRelatedChaFiles(h) :
                    character is SaveData.Player p ? KKAPI.MainGame.GameExtensions.GetRelatedChaFiles(p) :
                    null;
                return chafiles;
            }

        #elif AI    

            public static PregnancyData GetPregnancyData(this AgentData heroine)
            {
                if (heroine == null) return new PregnancyData();

                // Figure out which data to take if there are multiple
                // probably not necessary? null check should be enough? 
                return heroine.GetRelatedChaFiles()
                    .Select(GetPregnancyData)
                    .Where(x => x != null)
                    .OrderByDescending(x => x.PregnancyCount)
                    .ThenByDescending(x => x.WeeksSinceLastPregnancy)
                    .ThenByDescending(x => x.Week)
                    // .ThenByDescending(x => x.MenstruationSchedule) //TODO
                    .ThenByDescending(x => x.GameplayEnabled)
                    .FirstOrDefault() ?? new PregnancyData();
            }

            public static HeroineStatus GetHeroineStatus(this AgentData heroine, PregnancyData pregData = null)
            {
                if (heroine == null) return HeroineStatus.Unknown;

                if (pregData == null) pregData = heroine.GetPregnancyData();

                //TODO
                // Check if she wants to tell
                // if (heroine.intimacy >= 80 ||
                //     heroine.hCount >= 5 ||
                //     heroine.parameter.attribute.bitch && heroine.favor > 50 ||
                //     (heroine.isGirlfriend || heroine.favor >= 90) &&
                //     (!heroine.isVirgin || heroine.hCount >= 2 || heroine.intimacy >= 40))
                // {
                    var pregnancyWeek = pregData.Week;
                    if (pregnancyWeek > 0)
                    {
                        if (pregnancyWeek >= PregnancyData.LeaveSchoolWeek) return HeroineStatus.OnLeave;
                        if (PregnancyPlugin.ShowPregnancyIconEarly.Value) return HeroineStatus.Pregnant;
                        // Different personalities notice at different times
                        if (_earlyDetectPersonalities.Contains(heroine.GetNPC().ChaControl.fileParam.personality))
                        {
                            if (pregnancyWeek > 1) return HeroineStatus.Pregnant;
                        }
                        else if (_lateDetectPersonalities.Contains(heroine.GetNPC().ChaControl.fileParam.personality))
                        {
                            if (pregnancyWeek > 11) return HeroineStatus.Pregnant;
                        }
                        else
                        {
                            if (pregnancyWeek > 5) return HeroineStatus.Pregnant;
                        }
                    }

                    //TODO  use random chance?
                    return HeroineStatus.Risky;

                // }

                // return HeroineStatus.Unknown;
            }
        #endif
        
    }
}