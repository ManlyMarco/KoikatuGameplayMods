using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.MainGame;
using UnityEngine;
using AIProject;

namespace KK_Pregnancy
{
    public partial class PregnancyPlugin
    {
        private static class Hooks
        {
            public static void InitHooks(Harmony harmonyInstance)
            {
                harmonyInstance.PatchAll(typeof(Hooks));
            }

            private static bool _lastPullProc;

            private static bool CanGetSpawned(AgentActor heroine)
            {
                var isOnLeave = heroine.GetRelatedChaFiles()
                    .Any(c =>
                    {
                        var pd = PregnancyData.Load(ExtendedSave.GetExtendedDataById(heroine.ChaControl.chaFile, GUID));
                        if (pd == null) return false;
                        return pd.GameplayEnabled && pd.Week >= PregnancyData.LeaveSchoolWeek;
                    });
                return !isOnLeave;
            }

            private static List<AgentActor> GetFilteredHeroines(List<AgentActor> originalList)
            {
                var filteredHeroines = originalList.Where(CanGetSpawned).ToList();
                return filteredHeroines;
            }

            #region InflationAI                

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Sonyu), "Proc", typeof(int), typeof(HScene.AnimationListInfo))]
            public static void Sonyu_Proc(Sonyu __instance)
            {
                //Get current user button click type
                var ctrlFlag = Traverse.Create(__instance).Field("ctrlFlag").GetValue<HSceneFlagCtrl>();
                DetermineInflationState(ctrlFlag);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Houshi), "Proc", typeof(int), typeof(HScene.AnimationListInfo))]
            public static void Houshi_Proc(Houshi __instance)
            {
                //Get current user button click type
                var ctrlFlag = Traverse.Create(__instance).Field("ctrlFlag").GetValue<HSceneFlagCtrl>();
                DetermineInflationState(ctrlFlag);
            }

            //When user clicks finish button, set the inflation based on the button clicked
            private static void DetermineInflationState(HSceneFlagCtrl ctrlFlag)
            {
                //swallow clicked
                if (ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishInSide
                    || ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishSame
                    || ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishDrink)
                {
                    // PregnancyPlugin.Logger.LogDebug($"Preg - Proc {ctrlFlag.click}");
                    var heroine = GetLeadHeroine();
                    var controller = GetEffectController(heroine);
                    controller.AddInflation(1);
                }
                //spit clicked
                else if (ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishOutSide
                    || ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishVomit)
                {
                    // PregnancyPlugin.Logger.LogDebug($"Preg - Proc {ctrlFlag.click}");
                    var heroine = GetLeadHeroine();
                    var controller = GetEffectController(heroine);
                    controller.DrainInflation(Mathf.Max(3, Mathf.CeilToInt(InflationMaxCount.Value / 2.2f)));
                }
            }

            //pulling out
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Sonyu), "PullProc", typeof(float), typeof(int))]
            public static void Sonyu_PullProc(Sonyu __instance)
            {
                //Get current inserted state
                var ctrlFlag = Traverse.Create(__instance).Field("ctrlFlag").GetValue<HSceneFlagCtrl>();
                // PregnancyPlugin.Logger.LogDebug($"Preg - PullProc {ctrlFlag.isInsert}");

                if (ctrlFlag.isInsert && _lastPullProc != ctrlFlag.isInsert)
                {
                    var heroine = GetLeadHeroine();
                    var controller = GetEffectController(heroine);
                    controller.DrainInflation(Mathf.Max(3, Mathf.CeilToInt(InflationMaxCount.Value / 2.2f)));
                }

                _lastPullProc = ctrlFlag.isInsert;
            }

            private static PregnancyCharaController GetEffectController(Actor heroine)
            {
                return heroine?.ChaControl != null ? heroine.ChaControl.GetComponent<PregnancyCharaController>() : null;
            }

            private static Actor GetLeadHeroine()
            {
                return Manager.HSceneManager.Instance?.females[0];
            }

            #endregion
        }
    }
}
