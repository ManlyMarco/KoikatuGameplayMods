using System;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;

namespace KK_LewdCrestX
{
    internal static class CharacterHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.notBra), MethodType.Setter)]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.notShorts), MethodType.Setter)]
        private static void NotBraShortsOverride(ChaControl __instance, ref bool value)
        {
            if (__instance.GetCurrentCrest() == CrestType.liberated)
            {
                if (KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame || GameAPI.InsideHScene)
                {
                    // Force underwear to be off
                    value = true;
                }
            }
        }

        private static readonly ChaFileParameter.Denial _noDenial = new ChaFileParameter.Denial { aibu = true, anal = true, kiss = true, massage = true, notCondom = true };
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveData.CharaData), nameof(SaveData.CharaData.denial), MethodType.Getter)]
        public static void DenialOverride(SaveData.CharaData __instance, ref ChaFileParameter.Denial __result)
        {
            var currentCrest = (__instance as SaveData.Heroine)?.GetCurrentCrest();
            if (currentCrest == CrestType.command)
            {
                __result = _noDenial;
            }
            else if (currentCrest == CrestType.suffer)
            {
                var newResult = new ChaFileParameter.Denial();
                newResult.Copy(__result);
                newResult.aibu = true;
                newResult.anal = true;
                newResult.massage = true;
                __result = newResult;
            }
        }
    }
}