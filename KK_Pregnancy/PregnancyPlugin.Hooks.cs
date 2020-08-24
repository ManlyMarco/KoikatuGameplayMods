using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.MainGame;
using Manager;
using UnityEngine;

namespace KK_Pregnancy
{
    public partial class PregnancyPlugin
    {
        private static class Hooks
        {
            public static void InitHooks(Harmony harmonyInstance)
            {
                harmonyInstance.PatchAll(typeof(Hooks));

                PatchNPCLoadAll(harmonyInstance, new HarmonyMethod(typeof(Hooks), nameof(NPCLoadAllTpl)));
            }

            #region Custom safe day schedule

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

            #endregion

            #region Preg leave from school

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

            #endregion

            #region Inflation

            // todo separate anal/vag?
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuInside))]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalInside))]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddKuwaeFinish))]
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
        }
    }
}
