using System;
using HarmonyLib;

namespace KK_LewdCrestX
{
    internal static class CharacterHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.notBra), MethodType.Setter)]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.notShorts), MethodType.Setter)]
        private static void notBraOverride(ChaControl __instance, ref bool value)
        {
            Console.WriteLine("notBraOverride");
            if (LewdCrestXPlugin.GetCurrentCrest(__instance) == CrestType.liberated)
            {
                // Force underwear to be off
                value = true;
            }
        }

        private static readonly ChaFileParameter.Denial _noDenial = new ChaFileParameter.Denial { aibu = true, anal = true, kiss = true, massage = true, notCondom = true };
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveData.CharaData), nameof(SaveData.CharaData.denial), MethodType.Getter)]
        public static void DenialOverride(SaveData.CharaData __instance, ref ChaFileParameter.Denial __result)
        {
            // todo is this fast enough? cache upstream?
            var commandcrest = LewdCrestXPlugin.GetCurrentCrest(__instance as SaveData.Heroine) == CrestType.command;
            if (commandcrest) __result = _noDenial;
        }
    }
}