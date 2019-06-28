using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ExtensibleSaveFormat;
using Harmony;
using Manager;
using UniRx;

namespace KK_Pregnancy
{
    public partial class PregnancyPlugin
    {
        private static class Hooks
        {
            public static void InitHooks()
            {
                var hi = HarmonyInstance.Create(GUID);
                hi.PatchAll(typeof(Hooks));

                PatchNPCLoadAll(hi, new HarmonyMethod(typeof(Hooks), nameof(NPCLoadAllTpl)));
            }

            public static IEnumerable<CodeInstruction> NPCLoadAllTpl(IEnumerable<CodeInstruction> instructions)
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
                var data = ExtendedSave.GetExtendedDataById(heroine.charFile, GUID);
                if (data == null)
                    return true;

                ParseData(data, out var week, out var gameplayEnabled, out var _);
                if (gameplayEnabled && week >= LeaveSchoolWeek)
                    return false;

                return true;
            }

            private static List<SaveData.Heroine> GetFilteredHeroines(List<SaveData.Heroine> originalList)
            {
                var filteredHeroines = originalList.Where(CanGetSpawned).ToList();
                return filteredHeroines;
            }

            private static void PatchNPCLoadAll(HarmonyInstance instance, HarmonyMethod transpiler)
            {
                var t = typeof(ActionScene).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name.StartsWith("<NPCLoadAll>c__Iterator"));
                var m = t.GetMethod("MoveNext");
                instance.Patch(m, null, null, transpiler);
            }
        }
    }
}
