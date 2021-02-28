using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ExtensibleSaveFormat;
using HarmonyLib;
#if KK
    using KKAPI.MainGame;
#endif
using Manager;
using UnityEngine;
#if AI
    using AIChara;
    using AIProject;
#endif

namespace KK_Pregnancy
{
    public partial class PregnancyPlugin
    {
        private static class Hooks
        {
            public static void InitHooks(Harmony harmonyInstance)
            {
                harmonyInstance.PatchAll(typeof(Hooks));

                #if KK
                    PatchNPCLoadAll(harmonyInstance, new HarmonyMethod(typeof(Hooks), nameof(NPCLoadAllTpl)));
                #endif
            }

            #region Custom safe day schedule

            #if KK
                private static SaveData.Heroine _lastHeroine;
                private static byte[] _menstruationsBackup;

            
                [HarmonyPostfix]
                [HarmonyPatch(typeof(SaveData.Heroine), nameof(SaveData.Heroine.MenstruationDay), MethodType.Getter)]
                private static void LastAccessedHeroinePatch(SaveData.Heroine __instance)
                {
                    _lastHeroine = __instance;
                }

                [HarmonyPrefix]
                [HarmonyPatch(typeof(HFlag), nameof(HFlag.GetMenstruation), typeof(byte))]
                private static void GetMenstruationOverridePrefix()
                {
                    if (_lastHeroine != null)
                    {
                        // Get a schedule directly this way since the controller is not spawned in class roster
                        var schedule = _lastHeroine.GetRelatedChaFiles()
                            .Select(c => PregnancyData.Load(ExtendedSave.GetExtendedDataById(c, GUID))?.MenstruationSchedule ?? MenstruationSchedule.Default)
                            .FirstOrDefault(x => x != MenstruationSchedule.Default);

                        _menstruationsBackup = HFlag.menstruations;
                        HFlag.menstruations = PregnancyCharaController.GetMenstruationsArr(schedule);
                    }
                }

                [HarmonyPostfix]
                [HarmonyPatch(typeof(HFlag), nameof(HFlag.GetMenstruation), typeof(byte))]
                private static void GetMenstruationOverridePostfix()
                {
                    if (_menstruationsBackup != null)
                    {
                        HFlag.menstruations = _menstruationsBackup;
                        _menstruationsBackup = null;
                    }
                }
                
            #endif


            #endregion

            #region Preg leave from school

            #if KK
                /// <summary>
                /// Needed for preventing characters from going to school when on leave after pregnancy
                /// </summary>
                private static void PatchNPCLoadAll(Harmony instance, HarmonyMethod transpiler)
                {
                    var t = typeof(ActionScene).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name.StartsWith("<NPCLoadAll>c__Iterator"));
                    var m = t.GetMethod("MoveNext");
                    instance.Patch(m, null, null, transpiler);
                }

                private static IEnumerable<CodeInstruction> NPCLoadAllTpl(IEnumerable<CodeInstruction> instructions)
                {
                    var target = AccessTools.Property(typeof(Game), nameof(Game.HeroineList)).GetGetMethod();
                    var customFilterM = AccessTools.Method(typeof(Hooks), nameof(GetFilteredHeroines));
                    foreach (var instruction in instructions)
                    {
                        yield return instruction;

                        if (instruction.operand == target)
                        {
                            // Grab the return of get_HeroineList and process it
                            yield return new CodeInstruction(OpCodes.Call, customFilterM);
                        }
                    }
                }
            #endif

            #endregion Preg leave from school

            #if KK
                private static bool CanGetSpawned(SaveData.Heroine heroine)
                {
                    var isOnLeave = heroine.GetRelatedChaFiles()
                        .Any(c =>
                        {
                            var pd = PregnancyData.Load(ExtendedSave.GetExtendedDataById(heroine.charFile, GUID));
                            if (pd == null) return false;
                            return pd.GameplayEnabled && pd.Week >= PregnancyData.LeaveSchoolWeek;
                        });
                    return !isOnLeave;
                }

                private static List<SaveData.Heroine> GetFilteredHeroines(List<SaveData.Heroine> originalList)
                {
                    var filteredHeroines = originalList.Where(CanGetSpawned).ToList();
                    return filteredHeroines;
                }

                #region Inflation

                // todo separate anal/vag?
                [HarmonyPrefix]
                [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuInside))]
                [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalInside))]
                [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiDrink))]
                public static void OnFinishInside(HFlag __instance)
                {
                    var heroine = GetLeadHeroine(__instance);
                    var controller = GetEffectController(heroine);
                    controller.AddInflation(1);
                }

                [HarmonyPrefix]
                [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuTare))]
                [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalTare))]
                public static void OnDrain(HFlag __instance)
                {
                    var heroine = GetLeadHeroine(__instance);
                    var controller = GetEffectController(heroine);
                    controller.DrainInflation(Mathf.Max(3, Mathf.CeilToInt(InflationMaxCount.Value / 2.2f)));
                }

                private static PregnancyCharaController GetEffectController(SaveData.Heroine heroine)
                {
                    return heroine?.chaCtrl != null ? heroine.chaCtrl.GetComponent<PregnancyCharaController>() : null;
                }

                private static SaveData.Heroine GetLeadHeroine(HFlag hflag)
                {
                    var id = hflag.mode == HFlag.EMode.houshi3P || hflag.mode == HFlag.EMode.sonyu3P ? hflag.nowAnimationInfo.id % 2 : 0;
                    return hflag.lstHeroine[id];
                }

                #endregion    

            #elif AI

                //TODO copied from KKAPI
                private static IEnumerable<ChaFileControl> GetRelatedChaFiles(AgentActor heroine)
                {
                    if (heroine == null) throw new ArgumentNullException(nameof(heroine));

                    var results = new List<ChaFileControl>();

                    if (heroine.ChaControl != null && heroine.ChaControl.chaFile != null)
                        results.Add(heroine.ChaControl.chaFile);

                    var npc = heroine.GetNPC();
                    if (npc != null && npc.ChaControl != null && npc.ChaControl.chaFile != null)
                        results.Add(npc.ChaControl.chaFile);

                    return results;
                }

                private static bool CanGetSpawned(AgentActor heroine)
                {
                    var isOnLeave = GetRelatedChaFiles(heroine)
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
                    //swallow
                    if (ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishInSide 
                        || ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishSame  
                        || ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishDrink ) 
                    {
                        PregnancyPlugin.Logger.LogDebug($"Preg - Proc {ctrlFlag.click}");
                        var heroine = GetLeadHeroine();
                        var controller = GetEffectController(heroine);
                        controller.AddInflation(1);                    
                    }
                    //spit
                    else if (ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishOutSide 
                        || ctrlFlag.click == HSceneFlagCtrl.ClickKind.FinishVomit) 
                    {
                        PregnancyPlugin.Logger.LogDebug($"Preg - Proc {ctrlFlag.click}");
                        var heroine = GetLeadHeroine();
                        var controller = GetEffectController(heroine);
                        controller.DrainInflation(Mathf.Max(3, Mathf.CeilToInt(InflationMaxCount.Value / 2.2f)));
                    }    
                }
                

                private static PregnancyCharaController GetEffectController(AgentActor heroine)
                {
                    return heroine?.ChaControl != null ? heroine.ChaControl.GetComponent<PregnancyCharaController>() : null;
                }

                private static AgentActor GetLeadHeroine()
                {
                    return Manager.HSceneManager.Instance?.Agent[0];
                }

                #endregion                              
            #endif


        }
    }
}
