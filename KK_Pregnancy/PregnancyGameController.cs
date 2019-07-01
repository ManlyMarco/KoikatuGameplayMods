using System;
using System.Linq;
using ActionGame;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KK_Pregnancy
{
    public class PregnancyGameController : GameCustomFunctionController
    {
        protected override void OnDayChange(Cycle.Week day)
        {
            BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnDayChange " + day);
            // Use Sunday for weekly stuff because it is always triggered (all other days can get skipped)
            if (day == Cycle.Week.Holiday)
            {
                // At start of each week increase pregnancy week counters of all pregnant characters
                foreach (var heroine in Game.Instance.HeroineList)
                {
                    BepInEx.Logger.Log(LogLevel.Debug, "Preg - process heroine " + heroine.charFile.parameter.fullname);

                    foreach (var chaFile in heroine.GetRelatedChaFiles())
                        AddPregnancyWeek(chaFile);
                }
            }

            // If controller exists then update its state so it gets any pregnancy week updates
            foreach (var controller in FindObjectsOfType<PregnancyCharaController>())
            {
                controller.ReadData();
            }
        }

        private static void AddPregnancyWeek(ChaFileControl chaFile)
        {
            var data = ExtendedSave.GetExtendedDataById(chaFile, PregnancyPlugin.GUID);
            if (data == null) return;

            BepInEx.Logger.Log(LogLevel.Debug, "Preg - data found");
            PregnancyDataUtils.ParseData(data, out var week, out var gameplayEnabled, out var lowFertility);
            // Advance the week of pregnancy. If week is 0 the character is not pregnant
            if (gameplayEnabled && week > 0)
            {
                BepInEx.Logger.Log(LogLevel.Debug, "Preg - update preg week start " + chaFile.parameter.fullname);
                if (week < PregnancyDataUtils.LeaveSchoolWeek)
                {
                    BepInEx.Logger.Log(LogLevel.Debug, "Preg - week < PregnancyPlugin.LeaveSchoolWeek");
                    // Advance through in-school at full configured speed
                    var weekChange = PregnancyPlugin.PregnancyProgressionSpeed.Value;
                    week = Mathf.Min(PregnancyDataUtils.LeaveSchoolWeek, week + weekChange);

                    BepInEx.Logger.Log(LogLevel.Debug, "Preg - update preg week start " + chaFile.parameter.fullname);
                }
                else if (week < PregnancyDataUtils.ReturnToSchoolWeek)
                {
                    BepInEx.Logger.Log(LogLevel.Debug, "Preg - week < PregnancyPlugin.ReturnToSchoolWeek");
                    // Make sure at least one week is spent out of school
                    var weekChange = Mathf.Min(PregnancyDataUtils.ReturnToSchoolWeek - PregnancyDataUtils.LeaveSchoolWeek - 1, PregnancyPlugin.PregnancyProgressionSpeed.Value);
                    week = week + weekChange;
                }

                if (week >= PregnancyDataUtils.ReturnToSchoolWeek)
                    week = 0;

                BepInEx.Logger.Log(LogLevel.Debug, "Preg - end week " + week);
                ExtendedSave.SetExtendedDataById(chaFile, PregnancyPlugin.GUID, PregnancyDataUtils.WriteData(week, true, lowFertility));
            }
        }

        // Figure out if conception happened at end of h scene
        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnEndH with " + proc.flags.lstHeroine.First(x => x != null).charFile.parameter.fullname);
            if (!PregnancyPlugin.ConceptionEnabled.Value) return;

            // Don't know which girl player came inside
            if (proc.flags.mode == HFlag.EMode.houshi3P || proc.flags.mode == HFlag.EMode.sonyu3P) return;
            BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnEndH 2");

            var heroine = proc.flags.lstHeroine.First(x => x != null);
            var isDangerousDay = HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日;
            var cameInside = proc.flags.count.sonyuInside > 0;
            if (isDangerousDay && cameInside)
            {
                BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnEndH came inside on danger day");
                var controller = heroine.chaCtrl.GetComponent<PregnancyCharaController>();
                if (controller == null) throw new ArgumentNullException(nameof(controller));

                if (!controller.GameplayEnabled || controller.IsDuringPregnancy()) return;

                var winThreshold = Mathf.RoundToInt(controller.Fertility * 100);
                var childLottery = Random.Range(1, 100);
                BepInEx.Logger.Log(LogLevel.Debug, $"Preg - OnEndH calc pregnancy chance {childLottery} to {winThreshold}");
                var wonAChild = winThreshold >= childLottery;
                if (wonAChild)
                {
                    BepInEx.Logger.Log(LogLevel.Debug, "Preg - child lottery won");
                    controller.StartPregnancy();
                }
                else
                {
                    BepInEx.Logger.Log(LogLevel.Debug, "Preg - child lottery lost");

                }
            }
        }
    }
}