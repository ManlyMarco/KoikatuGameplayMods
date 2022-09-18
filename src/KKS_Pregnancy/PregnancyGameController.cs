using System;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;
using ActionGame;
using SaveData;

namespace KK_Pregnancy
{
    public class PregnancyGameController : GameCustomFunctionController
    {
        internal static bool InsideHScene { get; private set; }

        // silent means the heroine doesn't know about it and will not trigger an afterpill event
        public static void StartPregnancyDelayed(Heroine heroine, bool silent)
        {
            PregnancyPlugin.Logger.LogDebug($"StartPregnancyDelayed: heroine={heroine.parameter.fullname} silent={silent}");
            // Delay preg start/stop effects to the next period change so that everything done in a single
            // time period is averaged out, and that there is no sudden change to the belly size.
            heroine.talkEvent.Add(Constants.StartPregEventID);
            if (!silent)
            {
                ApplyToAllDatas((chara, data) =>
                {
                    if (chara == heroine && !data.CanAskForAfterpill)
                    {
                        data.CanAskForAfterpill = true;
                        return true;
                    }
                    return false;
                });
            }
        }

        // Overrules any pregnancies started at the same time
        public static void ForceStopPregnancyDelayed(Heroine heroine)
        {
            PregnancyPlugin.Logger.LogDebug($"ForceStopPregnancyDelayed: heroine={heroine.parameter.fullname}");

            heroine.talkEvent.Add(Constants.CancelPregEventID);
            ApplyToAllDatas((chara, data) =>
            {
                if (chara == heroine && (data.CanAskForAfterpill || data.CanTellAboutPregnancy))
                {
                    data.CanAskForAfterpill = false;
                    data.CanTellAboutPregnancy = false;
                    return true;
                }
                return false;
            });
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            // Use Sunday for weekly stuff because it is always triggered (all other days can get skipped)
            if (day == Cycle.Week.Holiday)
            {
                // At start of each week increase pregnancy week counters of all pregnant characters
                ApplyToAllDatas(AddPregnancyWeek);
            }
        }

        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            InsideHScene = true;
            proc.gameObject.AddComponent<LactationController>();
        }

        protected override void OnEndH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            InsideHScene = false;
            Destroy(proc.GetComponent<LactationController>());

            // Figure out if conception happened at end of h scene
            // bug Don't know which character is which
            if (hFlag.mode == HFlag.EMode.houshi3P || hFlag.mode == HFlag.EMode.sonyu3P) return;

            var heroine = hFlag.lstHeroine.First(x => x != null);
            var isDangerousDay = HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日;
            if (!isDangerousDay) return;

            var cameInside = PregnancyPlugin.ConceptionEnabled.Value && hFlag.count.sonyuInside > 0;
            var cameInsideAnal = PregnancyPlugin.AnalConceptionEnabled.Value && hFlag.count.sonyuAnalInside > 0;
            if (cameInside || cameInsideAnal)
            {
                var fertility = PregnancyDataUtils.GetFertility(heroine);

                var winThreshold = Mathf.RoundToInt(fertility * 100);
                var childLottery = Random.Range(1, 100);

                var wonAChild = winThreshold >= childLottery;
                PregnancyPlugin.Logger.LogDebug($"OnEndH -> lottery: heroine={heroine.parameter.fullname} winThreshold={winThreshold} won={wonAChild}");
                if (wonAChild)
                {
                    StartPregnancyDelayed(heroine, true);
                }

                ApplyToAllDatas((chara, data) =>
                {
                    if (chara == heroine && !data.IsPregnant && !data.CanAskForAfterpill)
                    {
                        data.CanAskForAfterpill = true;
                        return true;
                    }
                    return false;
                });
            }
        }

        protected override void OnPeriodChange(Cycle.Type period)
        {
            ProcessPendingChanges();
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            ProcessPendingChanges();
        }

        private static void ProcessPendingChanges()
        {
            ApplyToAllDatas((chara, data) =>
            {
                if (chara is Heroine heroine)
                {
                    if (heroine.talkEvent.Contains(Constants.CancelPregEventID))
                    {
                        PregnancyPlugin.Logger.LogDebug($"ProcessPendingChanges -> cancel: heroine={heroine.parameter.fullname}");

                        data.StopPregnancy();
                        return true;
                    }

                    if (data.GameplayEnabled && !data.IsPregnant)
                    {
                        if (heroine.talkEvent.Contains(Constants.StartPregEventID))
                        {
                            PregnancyPlugin.Logger.LogDebug($"ProcessPendingChanges -> start: heroine={heroine.parameter.fullname}");

                            data.StartPregnancy();
                            return true;
                        }
                    }
                }
                return false;
            });

            // Need to remove the tags after and not in lambda above because the lambda can run multiple times for each heroine
            foreach (var heroine in Game.HeroineList)
            {
                heroine.talkEvent.Remove(Constants.StartPregEventID);
                heroine.talkEvent.Remove(Constants.CancelPregEventID);
            }
        }

        internal static void ApplyToAllDatas(Func<CharaData, PregnancyData, bool> action)
        {
            void ApplyToDatas(CharaData character)
            {
                var chafiles = character.GetRelatedChaFiles();
                if (chafiles == null) return;
                var data = character.GetPregnancyData();
                if (!action(character, data)) return;
                foreach (var chaFile in chafiles)
                {
                    ExtendedSave.SetExtendedDataById(chaFile, PregnancyPlugin.GUID, data.Save());
                }
            }

            foreach (var heroine in Game.HeroineList) ApplyToDatas(heroine);
            ApplyToDatas(Game.Player);

            // If controller exists then update its state so it gets any pregnancy week updates
            foreach (var controller in FindObjectsOfType<PregnancyCharaController>())
                controller.ReadData();
        }

        private static bool AddPregnancyWeek(CharaData charaData, PregnancyData pd)
        {
            if (pd == null || !pd.GameplayEnabled) return false;

            if (pd.IsPregnant)
            {
                if (pd.Week < PregnancyData.LeaveSchoolWeek)
                {
                    // Advance through in-school at full configured speed
                    var weekChange = PregnancyDataUtils.GetPregnancyProgressionSpeed(charaData);
                    pd.Week = Mathf.Min(PregnancyData.LeaveSchoolWeek, pd.Week + weekChange);
                }
                else if (pd.Week < PregnancyData.ReturnToSchoolWeek)
                {
                    // Make sure at least one week is spent out of school
                    var weekChange = Mathf.Min(PregnancyData.ReturnToSchoolWeek - PregnancyData.LeaveSchoolWeek - 1, PregnancyDataUtils.GetPregnancyProgressionSpeed(charaData));
                    pd.Week += weekChange;
                }

                if (pd.Week >= PregnancyData.ReturnToSchoolWeek)
                    pd.Week = 0;
                // PregnancyPlugin.Logger.LogDebug($"Preg - pregnancy week is now {pd.Week}");
            }
            else if (pd.PregnancyCount > 0)
            {
                pd.WeeksSinceLastPregnancy++;
            }

            if (pd.Week > 1)
                pd.CanAskForAfterpill = false;

            return true;
        }
    }
}
