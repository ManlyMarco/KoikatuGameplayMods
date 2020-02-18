using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Harmony;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.MainGame;
using Manager;

namespace KK_Pregnancy
{
    public partial class PregnancyPlugin
    {
        private static class Hooks
        {
            public static void InitHooks(Harmony harmonyInstance)
            {
                HarmonyWrapper.PatchAll(typeof(Hooks), harmonyInstance);

                PatchNPCLoadAll(harmonyInstance, new HarmonyMethod(typeof(Hooks), nameof(NPCLoadAllTpl)));
            }

            #region Custom safe day schedule

            private static SaveData.Heroine _lastHeroine;
            private static byte[] _menstruationsBackup;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveData.Heroine), "get_" + nameof(SaveData.Heroine.MenstruationDay))]
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
                    var schedule = _lastHeroine.GetRelatedChaFiles().Select(control =>
                    {
                        var d = ExtendedSave.GetExtendedDataById(control, GUID);
                        if (d == null) return PregnancyDataUtils.MenstruationSchedule.Default;
                        PregnancyDataUtils.DeserializeData(d, out _, out _, out _, out var s);
                        return s;
                    }).FirstOrDefault(x => x != PregnancyDataUtils.MenstruationSchedule.Default);

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
                        var data = ExtendedSave.GetExtendedDataById(heroine.charFile, GUID);

                        if (data == null) return false;

                        PregnancyDataUtils.DeserializeData(data, out var week, out var gameplayEnabled, out _, out _);
                        if (gameplayEnabled && week >= PregnancyDataUtils.LeaveSchoolWeek)
                            return true;

                        return false;
                    });
                return !isOnLeave;
            }

            private static List<SaveData.Heroine> GetFilteredHeroines(List<SaveData.Heroine> originalList)
            {
                var filteredHeroines = originalList.Where(CanGetSpawned).ToList();
                return filteredHeroines;
            }

            #endregion
        }
    }
}
