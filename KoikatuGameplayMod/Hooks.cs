using System;
using Harmony;
using UnityEngine;
using Random = System.Random;

// ReSharper disable InconsistentNaming

namespace KoikatuGameplayMod
{
    internal static class Hooks
    {
        private static readonly Random RandomGen = new Random();

        public static void ApplyHooks()
        {
            var i = HarmonyInstance.Create("marco-gameplaymod");
            i.PatchAll(typeof(Hooks));
        }

        #region ForceRaw

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertNoVoiceClick), new Type[] { })]
        public static void OnInsertNoVoiceClickPre(HSprite __instance)
        {
            if (!Input.GetMouseButtonUp(0) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isInsertOK)
                return;

            var heroine = __instance.flags.lstHeroine[0];
            var girlOrgasms = __instance.flags.count.sonyuOrg;

            // Check if player can force raw 
            // OUT_A is resting after popping the cork outdoors
            if (__instance.flags.nowAnimStateName == "OUT_A" ||
                __instance.flags.isDenialvoiceWait ||
                girlOrgasms >= 3 + RandomGen.Next(0, 3) - heroine.lewdness / 66)
            {
                MakeGirlAngry(heroine);

                ForceAllowRaw(__instance);
                __instance.flags.isDenialvoiceWait = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertClick), new Type[] { })]
        public static void OnInsertClickPre(HSprite __instance)
        {
            if (!Input.GetMouseButtonUp(0) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isInsertOK)
                return;

            var heroine = __instance.flags.lstHeroine[0];
            var girlOrgasms = __instance.flags.count.sonyuOrg;

            // Check if girl allows raw
            if (girlOrgasms >= 4 + RandomGen.Next(0, 3) - heroine.lewdness / 45)
                ForceAllowRaw(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertNoVoiceClick), new Type[] { })]
        public static void OnInsertNoVoiceClickPost(HSprite __instance)
        {
            ResetForceAllowRaw(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertClick), new Type[] { })]
        public static void OnInsertClickPost(HSprite __instance)
        {
            ResetForceAllowRaw(__instance);
        }

        private static void ForceAllowRaw(HSprite instance)
        {
            instance.flags.isDebug = true;
        }

        private static void ResetForceAllowRaw(HSprite __instance)
        {
            __instance.flags.isDebug = false;
        }

        private static void MakeGirlAngry(SaveData.Heroine heroine)
        {
            heroine.anger = Math.Min(100, heroine.anger + 34);
            heroine.isAnger = true;
            heroine.favor = Math.Max(0, heroine.favor - 10);

            heroine.chaCtrl.tearsLv = 1;
            heroine.chaCtrl.ChangeEyesShaking(true);
            heroine.chaCtrl.ChangeLookEyesTarget(2);
            heroine.chaCtrl.ChangeTongueState(0);
        }

        #endregion
    }
}