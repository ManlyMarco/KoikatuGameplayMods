using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KK_Pregnancy
{
    public class PregnancyGameController : GameCustomFunctionController
    {
        private static readonly HashSet<SaveData.Heroine> _startedPregnancies = new HashSet<SaveData.Heroine>();

        protected override void OnDayChange(Cycle.Week day)
        {
            // Use Sunday for weekly stuff because it is always triggered (all other days can get skipped)
            if (day == Cycle.Week.Holiday)
            {
                ProcessNewWeekStart();
                RefreshControllers();
            }
        }

        // Figure out if conception happened at end of h scene
        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            if (!PregnancyPlugin.ConceptionEnabled.Value) return;

            // Don't know which girl player came inside
            if (proc.flags.mode == HFlag.EMode.houshi3P || proc.flags.mode == HFlag.EMode.sonyu3P) return;

            var heroine = proc.flags.lstHeroine.First(x => x != null);
            var isDangerousDay = HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日;
            var cameInside = proc.flags.count.sonyuInside > 0;
            if (isDangerousDay && cameInside)
            {
                var controller = heroine.chaCtrl.GetComponent<PregnancyCharaController>();
                if (controller == null) throw new ArgumentNullException(nameof(controller));

                if (!controller.GameplayEnabled || controller.IsDuringPregnancy()) return;

                var winThreshold = Mathf.RoundToInt(controller.Fertility * 100);
                var childLottery = Random.Range(1, 100);
                //Logger.Log(LogLevel.Debug, $"Preg - OnEndH calc pregnancy chance {childLottery} to {winThreshold}");
                var wonAChild = winThreshold >= childLottery;
                if (wonAChild)
                {
                    //Logger.Log(LogLevel.Debug, "Preg - child lottery won, pregnancy will start");
                    _startedPregnancies.Add(heroine);
                }
            }
        }

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            _startedPregnancies.Clear();
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            ProcessNewlyStartedPregnancies();
            RefreshControllers();
        }

        protected override void OnPeriodChange(Cycle.Type period)
        {
            if (ProcessNewlyStartedPregnancies())
                RefreshControllers();
        }

        private static bool ProcessNewlyStartedPregnancies()
        {
            if (_startedPregnancies.Count == 0) return false;
            foreach (var startedPregnancy in _startedPregnancies.Where(x => x != null))
            {
                foreach (var chaFile in startedPregnancy.GetRelatedChaFiles())
                    StartPregnancy(chaFile);
            }
            _startedPregnancies.Clear();
            return true;
        }

        private static void ProcessNewWeekStart()
        {
            // At start of each week increase pregnancy week counters of all pregnant characters
            foreach (var heroine in Game.Instance.HeroineList)
            {
                foreach (var chaFile in heroine.GetRelatedChaFiles())
                    AddPregnancyWeek(chaFile);
            }
        }

        private static void RefreshControllers()
        {
            // If controller exists then update its state so it gets any pregnancy week updates
            foreach (var controller in FindObjectsOfType<PregnancyCharaController>())
                controller.ReadData();
        }

        private static void StartPregnancy(ChaFileControl chaFile)
        {
            var data = ExtendedSave.GetExtendedDataById(chaFile, PregnancyPlugin.GUID);
            PregnancyDataUtils.DeserializeData(data, out var week, out var gameplayEnabled, out var fertility);

            // If week is 0 the character is not pregnant
            if (gameplayEnabled && week <= 0)
            {
                //Logger.Log(LogLevel.Debug, "Preg - starting pregnancy on " + chaFile.parameter.fullname + ", new week is " + 1);

                ExtendedSave.SetExtendedDataById(chaFile, PregnancyPlugin.GUID, PregnancyDataUtils.SerializeData(1, true, fertility));
            }
        }

        private static void AddPregnancyWeek(ChaFileControl chaFile)
        {
            var data = ExtendedSave.GetExtendedDataById(chaFile, PregnancyPlugin.GUID);
            if (data == null) return;

            PregnancyDataUtils.DeserializeData(data, out var week, out var gameplayEnabled, out var fertility);
            // Advance the week of pregnancy. If week is 0 the character is not pregnant
            if (gameplayEnabled && week > 0)
            {
                if (week < PregnancyDataUtils.LeaveSchoolWeek)
                {
                    // Advance through in-school at full configured speed
                    var weekChange = PregnancyPlugin.PregnancyProgressionSpeed.Value;
                    week = Mathf.Min(PregnancyDataUtils.LeaveSchoolWeek, week + weekChange);
                }
                else if (week < PregnancyDataUtils.ReturnToSchoolWeek)
                {
                    // Make sure at least one week is spent out of school
                    var weekChange = Mathf.Min(PregnancyDataUtils.ReturnToSchoolWeek - PregnancyDataUtils.LeaveSchoolWeek - 1, PregnancyPlugin.PregnancyProgressionSpeed.Value);
                    week = week + weekChange;
                }

                if (week >= PregnancyDataUtils.ReturnToSchoolWeek)
                    week = 0;

                //Logger.Log(LogLevel.Debug, $"Preg - pregnancy week for {chaFile.parameter.fullname} is now {week}");
                ExtendedSave.SetExtendedDataById(chaFile, PregnancyPlugin.GUID, PregnancyDataUtils.SerializeData(week, true, fertility));
            }
        }
    }
}
