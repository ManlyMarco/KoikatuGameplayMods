using System;
using HarmonyLib;
using UnityEngine;

namespace KoikatuGameplayMod
{
    internal static class ForceInsertHooks
    {
        public static void ApplyHooks(Harmony instance)
        {
            instance.PatchAll(typeof(ForceInsertHooks));

            Utilities.HSceneEndClicked += ApplyGirlAnger;
        }

        #region ForceAnal

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertAnalClick), new Type[] { })]
        public static void OnInsertAnalClickPre(HSprite __instance)
        {
            if ((Application.productName != KoikatuGameplayMod.GameProcessNameVR && !Input.GetMouseButtonUp(0)) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isAnalInsertOK)
                return;

            // Check if player can circumvent the anal deny
            if (__instance.flags.count.sonyuAnalOrg >= 1)
            {
                var heroine = Utilities.GetTargetHeroine(__instance);

                MakeGirlAngry(heroine, 20, 10);

                Utilities.ForceAllowInsert(__instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertAnalNoVoiceClick), new Type[] { })]
        public static void OnInsertAnalNoVoiceClickPre(HSprite __instance)
        {
            if ((Application.productName != KoikatuGameplayMod.GameProcessNameVR && !Input.GetMouseButtonUp(0)) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isAnalInsertOK)
                return;

            var heroine = Utilities.GetTargetHeroine(__instance);
            if (heroine == null) return;

            // Check if player can circumvent the anal deny
            if (!KoikatuGameplayMod.ForceInsert.Value) return;
            if (CanCircumventDeny(__instance) || __instance.flags.count.sonyuAnalOrg >= 1)
            {
                MakeGirlAngry(heroine, 30, 15);

                Utilities.ForceAllowInsert(__instance);
                __instance.flags.isDenialvoiceWait = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertAnalClick), new Type[] { })]
        public static void OnInsertAnalClickPost(HSprite __instance)
        {
            Utilities.ResetForceAllowInsert(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertAnalNoVoiceClick), new Type[] { })]
        public static void OnInsertAnalNoVoiceClickPost(HSprite __instance)
        {
            Utilities.ResetForceAllowInsert(__instance);
        }

        #endregion

        #region ForceRaw

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertNoVoiceClick), new Type[] { })]
        public static void OnInsertNoVoiceClickPre(HSprite __instance)
        {
            if ((Application.productName != KoikatuGameplayMod.GameProcessNameVR && !Input.GetMouseButtonUp(0)) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isInsertOK[Utilities.GetTargetHeroineId(__instance)])
                return;

            var heroine = Utilities.GetTargetHeroine(__instance);
            var girlOrgasms = __instance.flags.count.sonyuOrg;

            // Check if player can circumvent the raw deny
            if (!KoikatuGameplayMod.ForceInsert.Value) return;
            if (CanCircumventDeny(__instance) ||
                girlOrgasms >= 3 + UnityEngine.Random.Range(0, 3) - heroine.lewdness / 66)
            {
                MakeGirlAngry(heroine, 20, 10);

                Utilities.ForceAllowInsert(__instance);
                __instance.flags.isDenialvoiceWait = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertClick), new Type[] { })]
        public static void OnInsertClickPre(HSprite __instance)
        {
            if ((Application.productName != KoikatuGameplayMod.GameProcessNameVR && !Input.GetMouseButtonUp(0)) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isInsertOK[Utilities.GetTargetHeroineId(__instance)])
                return;

            var heroine = Utilities.GetTargetHeroine(__instance);
            if (heroine == null) return;
            var girlOrgasms = __instance.flags.count.sonyuOrg;

            // Check if girl allows raw
            if (girlOrgasms >= 4 + UnityEngine.Random.Range(0, 3) - heroine.lewdness / 45)
                Utilities.ForceAllowInsert(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertNoVoiceClick), new Type[] { })]
        public static void OnInsertNoVoiceClickPost(HSprite __instance)
        {
            Utilities.ResetForceAllowInsert(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertClick), new Type[] { })]
        public static void OnInsertClickPost(HSprite __instance)
        {
            Utilities.ResetForceAllowInsert(__instance);
        }

        /// <summary>
        /// ang 15 fav 10
        /// </summary>
        /// <param name="heroine"></param>
        /// <param name="angerAmount"></param>
        /// <param name="favorAmount"></param>
        private static void MakeGirlAngry(SaveData.Heroine heroine, int angerAmount, int favorAmount)
        {
            if (!KoikatuGameplayMod.ForceInsertAnger.Value) return;

            heroine.anger = Math.Min(100, heroine.anger + angerAmount);
            heroine.favor = Math.Max(0, heroine.favor - favorAmount);

            heroine.chaCtrl.tearsLv = 2;
            heroine.chaCtrl.ChangeEyesShaking(true);
            heroine.chaCtrl.ChangeLookEyesTarget(2);
            heroine.chaCtrl.ChangeTongueState(0);
            heroine.chaCtrl.ChangeEyesOpenMax(1f);
        }

        private static bool CanCircumventDeny(HSprite __instance)
        {
            // OUT_A is resting after popping the cork outdoors
            return string.Equals(__instance.flags.nowAnimStateName, "OUT_A", StringComparison.Ordinal) ||
                   string.Equals(__instance.flags.nowAnimStateName, "A_OUT_A", StringComparison.Ordinal) ||
                   __instance.flags.isDenialvoiceWait;
        }

        private static void ApplyGirlAnger(HSprite __instance)
        {
            if (!KoikatuGameplayMod.ForceInsertAnger.Value) return;

            var heroine = Utilities.GetTargetHeroine(__instance);
            if (heroine == null) return;

            if (!__instance.flags.isInsertOK[Utilities.GetTargetHeroineId(__instance)])
            {
                if (__instance.flags.count.sonyuInside > 0)
                {
                    if (HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日)
                    {
                        // If it's dangerous always make her angry
                        heroine.anger = Math.Min(100, heroine.anger + __instance.flags.count.sonyuInside * 45);
                        heroine.isAnger = true;
                    }
                    else
                    {
                        heroine.anger = Math.Min(100, heroine.anger + __instance.flags.count.sonyuInside * 25);
                    }
                }
                else if (__instance.flags.count.sonyuOutside > 0)
                {
                    heroine.anger = Math.Max(0, heroine.anger - __instance.flags.count.sonyuOutside * 10);
                }
            }

            if (heroine.anger >= 100)
                heroine.isAnger = true;
        }

        #endregion

    }
}