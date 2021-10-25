using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
using AIChara;
using AIProject;
using AIProject.SaveData;
using AIProject.Definitions;

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
                .ThenByDescending(x => x.GameplayEnabled)
                .FirstOrDefault() ?? new PregnancyData();
        }

        public static HeroineStatus GetHeroineStatus(this AgentData heroine, PregnancyData pregData = null)
        {
            if (heroine == null) return HeroineStatus.Unknown;
            if (pregData == null) pregData = heroine.GetPregnancyData();

            var chaControl = heroine.GetNPC()?.ChaControl;
            if (chaControl == null) return HeroineStatus.Unknown;

            // Check if she wants to tell
            if (heroine.SickState.ID == AIProject.Definitions.Sickness.GoodHealthID && !heroine.IsWet &&
                (chaControl.fileGameInfo.phase > 2
                  || heroine.StatsTable[(int)Status.Type.Mood] > 95
                  || heroine.StatsTable[(int)Status.Type.Immoral] > 95
                  || heroine.StatsTable[(int)Status.Type.Motivation] > 140))
            {

                var pregnancyWeek = pregData.Week;
                if (pregnancyWeek > 0)
                {
                    if (PregnancyPlugin.ShowPregnancyIconEarly.Value) return HeroineStatus.Pregnant;
                    // Different personalities notice at different times
                    if (_earlyDetectPersonalities.Contains(chaControl.fileParam.personality))
                    {
                        if (pregnancyWeek > 1) return HeroineStatus.Pregnant;
                    }
                    else if (_lateDetectPersonalities.Contains(chaControl.fileParam.personality))
                    {
                        if (pregnancyWeek > 11) return HeroineStatus.Pregnant;
                    }
                    else
                    {
                        if (pregnancyWeek > 5) return HeroineStatus.Pregnant;
                    }
                }

                var pregCharCtrl = chaControl.GetComponent<PregnancyCharaController>();
                return !pregCharCtrl.isDangerousDay
                    ? HeroineStatus.Safe
                    : HeroineStatus.Risky;
            }

            return HeroineStatus.Unknown;

        }
    }
}