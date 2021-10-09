using System.Collections.Generic;
using System.Reflection.Emit;
using ActionGame;
using BepInEx.Configuration;
using Config;
using HarmonyLib;
using KKAPI;
using KKAPI.Utilities;

namespace KoikatuGameplayMod
{
    internal class ClassCharaLimitUnlockHooks : IFeature
    {
        private const int UnlockedMaxCharacters = 99;

        public bool Install(Harmony instance, ConfigFile config)
        {
            if (KoikatuAPI.IsVR()) return false;
         
            instance.PatchAll(typeof(ClassCharaLimitUnlockHooks));
            instance.PatchMoveNext(AccessTools.Method(typeof(ActionScene), nameof(ActionScene.NPCLoadAll)),
                transpiler: new HarmonyMethod(typeof(ClassCharaLimitUnlockHooks), nameof(NPCLoadAllUnlock)));

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClassRoomList), nameof(ClassRoomList.Start))]
        private static void ClassRoomListUnlock(ClassRoomList __instance)
        {
            __instance.sldAttendanceNum.maxValue = UnlockedMaxCharacters;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EtceteraSetting), nameof(EtceteraSetting.Init))]
        private static void EtceteraSettingUnlock(EtceteraSetting __instance)
        {
            __instance.maxCharaNumSlider.maxValue = UnlockedMaxCharacters;
        }

        private static IEnumerable<CodeInstruction> NPCLoadAllUnlock(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_S)
                {
                    if (((sbyte)0x26).Equals(instruction.operand))
                        instruction.operand = UnlockedMaxCharacters;
                }
                yield return instruction;
            }
        }
    }
}
