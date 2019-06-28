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
    public class PregnancyManager : GameCustomFunctionController
    {
        protected override void OnDayChange(Cycle.Week day)
        {
            // Use monday for weekly stuff because it is always triggered (alternative would be sunday/saturday)
            if (day == Cycle.Week.Monday)
            {
                BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnDayChange Monday");
                // At start of each week increase pregnancy week counters of all pregnant characters
                foreach (var heroine in Game.Instance.HeroineList)
                {
                    var data = ExtendedSave.GetExtendedDataById(heroine.charFile, PregnancyPlugin.GUID);
                    if (data != null)
                    {
                        PregnancyPlugin.ParseData(data, out var week, out var gameplayEnabled, out var lowFertility);
                        // Advance the week of pregnancy. If week is 0 the character is not pregnant
                        if (gameplayEnabled && week > 0)
                        {
                            BepInEx.Logger.Log(LogLevel.Debug, "Preg - update preg week start " + heroine.charFile.parameter.fullname);
                            if (week < PregnancyPlugin.LeaveSchoolWeek)
                            {
                                BepInEx.Logger.Log(LogLevel.Debug, "Preg - week < PregnancyPlugin.LeaveSchoolWeek");
                                // Advance through in-school at full configured speed
                                var weekChange = PregnancyPlugin.PregnancyProgressionSpeed.Value;
                                week = Mathf.Min(PregnancyPlugin.LeaveSchoolWeek, week + weekChange);

                                // Force the girl to always be on the safe day, happens every day after day of conception
                                HFlag.SetMenstruation(heroine, HFlag.MenstruationType.安全日);
                                BepInEx.Logger.Log(LogLevel.Debug, "Preg - update preg week start " + heroine.charFile.parameter.fullname);
                            }
                            else if (week < PregnancyPlugin.ReturnToSchoolWeek)
                            {
                                BepInEx.Logger.Log(LogLevel.Debug, "Preg - week < PregnancyPlugin.ReturnToSchoolWeek");
                                // Make sure at least one week is spent out of school
                                var weekChange = Mathf.Min(PregnancyPlugin.ReturnToSchoolWeek - PregnancyPlugin.LeaveSchoolWeek - 1, PregnancyPlugin.PregnancyProgressionSpeed.Value);
                                week = week + weekChange;
                            }

                            if (week >= PregnancyPlugin.ReturnToSchoolWeek)
                                week = 0;

                            BepInEx.Logger.Log(LogLevel.Debug, "Preg - end week " + week);
                            ExtendedSave.SetExtendedDataById(heroine.charFile, PregnancyPlugin.GUID, PregnancyPlugin.WriteData(week, true, lowFertility));
                            // If controller exists then update its values so it doesn't overwrite them when saving
                            heroine.chaCtrl?.GetComponent<PregnancyController>()?.ReadSavedData();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Figure out if conception happened
        /// </summary>
        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnEndH 1 " + proc.flags.lstHeroine.First(x => x != null).charFile.parameter.fullname);
            if (!PregnancyPlugin.ConceptionEnabled.Value) return;

            // Don't know which girl player came inside
            if (proc.flags.mode == HFlag.EMode.houshi3P || proc.flags.mode == HFlag.EMode.sonyu3P) return;
            BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnEndH 2");

            var heroine = proc.flags.lstHeroine.First(x => x != null);
            var isDangerousDay = HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日;
            var cameInside = proc.flags.count.sonyuInside > 0;
            if (isDangerousDay && cameInside)
            {
                BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnEndH 3");
                var controller = heroine.chaCtrl.GetComponent<PregnancyController>();
                if (controller == null) throw new ArgumentNullException(nameof(controller));

                if (!controller.GameplayEnabled || controller.IsDuringPregnancy()) return;

                BepInEx.Logger.Log(LogLevel.Debug, "Preg - OnEndH 4");
                var winThreshold = Mathf.RoundToInt(controller.Fertility * 100);
                var childLottery = Random.Range(1, 100);
                var wonAChild = winThreshold >= childLottery;
                if (wonAChild)
                    controller.StartPregnancy();
            }
        }
    }
}