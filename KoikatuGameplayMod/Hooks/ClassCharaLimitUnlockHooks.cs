using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using Config;
using Harmony;
using UnityEngine.UI;

namespace KoikatuGameplayMod
{
    internal static class ClassCharaLimitUnlockHooks
    {
        private const int UnlockedMaxCharacters = 99;

        public static void ApplyHooks(HarmonyInstance instance)
        {
            instance.PatchAll(typeof(ClassCharaLimitUnlockHooks));

            var t = typeof(ActionScene).GetNestedType("<NPCLoadAll>c__IteratorD", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var m = t.GetMethod("MoveNext");
            instance.Patch(m, null, null, new HarmonyMethod(typeof(ClassCharaLimitUnlockHooks), nameof(NPCLoadAllUnlock)));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClassRoomList), "Start")]
        public static void ClassRoomListUnlock(ClassRoomList __instance)
        {
            var f = typeof(ClassRoomList).GetField("sldAttendanceNum", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var sld = (Slider) f.GetValue(__instance);
            sld.maxValue = UnlockedMaxCharacters;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EtceteraSetting), "Init")]
        public static void EtceteraSettingUnlock(EtceteraSetting __instance)
        {
            var f = typeof(EtceteraSetting).GetField("maxCharaNumSlider", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var sld = (Slider) f.GetValue(__instance);
            sld.maxValue = UnlockedMaxCharacters;
        }

        public static IEnumerable<CodeInstruction> NPCLoadAllUnlock(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_S)
                {
                    if (((sbyte) 0x26).Equals(instruction.operand))
                        instruction.operand = UnlockedMaxCharacters;
                }
                yield return instruction;
            }
        }
    }
}
