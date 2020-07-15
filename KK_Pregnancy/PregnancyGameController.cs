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

        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            base.OnStartH(proc, freeH);

            proc.gameObject.AddComponent<LactationController>();
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            Destroy(proc.GetComponent<LactationController>());

            // Figure out if conception happened at end of h scene
            // bug Don't know which character is which
            if (proc.flags.mode == HFlag.EMode.houshi3P || proc.flags.mode == HFlag.EMode.sonyu3P) return;

            var heroine = proc.flags.lstHeroine.First(x => x != null);
            var isDangerousDay = HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日;
            if (!isDangerousDay) return;

            var cameInside = PregnancyPlugin.ConceptionEnabled.Value && proc.flags.count.sonyuInside > 0;
            var cameInsideAnal = PregnancyPlugin.AnalConceptionEnabled.Value && proc.flags.count.sonyuAnalInside > 0;
            if (cameInside || cameInsideAnal)
            {
                var controller = heroine.chaCtrl.GetComponent<PregnancyCharaController>();
                if (controller == null) throw new ArgumentNullException(nameof(controller));

                if (!controller.Data.GameplayEnabled || controller.Data.IsPregnant) return;

                var winThreshold = Mathf.RoundToInt(controller.Data.Fertility * 100);
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
            var pd = PregnancyData.Load(data);

            // If week is 0 the character is not pregnant
            if (pd.GameplayEnabled && !pd.IsPregnant)
            {
                pd.StartPregnancy();
                ExtendedSave.SetExtendedDataById(chaFile, PregnancyPlugin.GUID, pd.Save());
            }
        }

        private static void AddPregnancyWeek(ChaFileControl chaFile)
        {
            var data = ExtendedSave.GetExtendedDataById(chaFile, PregnancyPlugin.GUID);
            if (data == null) return;

            var pd = PregnancyData.Load(data);

            if (pd.GameplayEnabled)
            {
                if (pd.IsPregnant)
                {
                    if (pd.Week < PregnancyData.LeaveSchoolWeek)
                    {
                        // Advance through in-school at full configured speed
                        var weekChange = PregnancyPlugin.PregnancyProgressionSpeed.Value;
                        pd.Week = Mathf.Min(PregnancyData.LeaveSchoolWeek, pd.Week + weekChange);
                    }
                    else if (pd.Week < PregnancyData.ReturnToSchoolWeek)
                    {
                        // Make sure at least one week is spent out of school
                        var weekChange = Mathf.Min(PregnancyData.ReturnToSchoolWeek - PregnancyData.LeaveSchoolWeek - 1, PregnancyPlugin.PregnancyProgressionSpeed.Value);
                        pd.Week = pd.Week + weekChange;
                    }

                    if (pd.Week >= PregnancyData.ReturnToSchoolWeek)
                        pd.Week = 0;

                    //Logger.Log(LogLevel.Debug, $"Preg - pregnancy week for {chaFile.parameter.fullname} is now {week}");
                }
                else
                {
                    pd.WeeksSinceLastPregnancy++;
                }

                ExtendedSave.SetExtendedDataById(chaFile, PregnancyPlugin.GUID, pd.Save());
            }
        }
    }
}
