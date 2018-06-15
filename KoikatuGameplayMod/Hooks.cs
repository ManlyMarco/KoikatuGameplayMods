using System;
using Harmony;
using UnityEngine;
using Random = System.Random;

namespace KoikatuGameplayMod
{
    static class Hooks
    {
        private static readonly Random RandomGen = new Random();

        public static void ApplyHooks()
        {
            var i = HarmonyInstance.Create("marco-gameplaymod");
            i.PatchAll(typeof(Hooks));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertNoVoiceClick), new Type[] { })]
        public static void IsNamaInsertOkPre(HSprite __instance)
        {
            if (!Input.GetMouseButtonUp(0) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isInsertOK)
                return;

            // Check if player can force raw 
            var girlOrgasms = __instance.flags.count.sonyuOrg;
            // OUT_A is resting after cumming outside
            if (__instance.flags.nowAnimStateName == "OUT_A" || __instance.flags.isDenialvoiceWait || girlOrgasms >= 3 - RandomGen.Next(0, 3))
            {
                // Make girl angry
                __instance.flags.lstHeroine[0].anger = Math.Min(100, __instance.flags.lstHeroine[0].anger + 34);
                __instance.flags.lstHeroine[0].isAnger = true;
                __instance.flags.lstHeroine[0].favor = Math.Max(0, __instance.flags.lstHeroine[0].favor - 10);

                __instance.flags.lstHeroine[0].chaCtrl.tearsLv = 1;
                __instance.flags.lstHeroine[0].chaCtrl.ChangeEyesShaking(true);
                __instance.flags.lstHeroine[0].chaCtrl.ChangeLookEyesTarget(2);
                __instance.flags.lstHeroine[0].chaCtrl.ChangeTongueState(0);

                // Trick game into allowing raw
                __instance.flags.isDebug = true;
                __instance.flags.isDenialvoiceWait = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertNoVoiceClick), new Type[] { })]
        public static void IsNamaInsertOkPost(HSprite __instance)
        {
            __instance.flags.isDebug = false;
        }
    }
}
