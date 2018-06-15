using System;
using System.Linq;
using System.Reflection;
using Harmony;
using UniRx;
using UnityEngine;
using Random = System.Random;

namespace KoikatuGameplayMod
{
    internal static class Hooks
    {
        public static readonly Random RandomGen = new Random();

        public static void ApplyHooks()
        {
            var i = HarmonyInstance.Create("marco-gameplaymod");
            i.PatchAll(typeof(Hooks));
        }

        #region FastTravelCost

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapSelectMenuScene), "Start", new Type[] { })]
        public static void MapSelectMenuSceneRegisterCallback(MapSelectMenuScene __instance)
        {
            var f = typeof(MapSelectMenuScene).GetField("enterButton",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var b = f.GetValue(__instance) as UnityEngine.UI.Button;
            b.OnClickAsObservable().Subscribe(unit =>
            {
                if (__instance.result == MapSelectMenuScene.ResultType.EnterMapMove)
                {
                    var cycle = UnityEngine.Object.FindObjectsOfType<ActionGame.Cycle>().FirstOrDefault();
                    if (cycle != null)
                    {
                        var newVal = Math.Min(cycle.timer + KoikatuGameplayMod.FastTravelTimePenalty.Value, ActionGame.Cycle.TIME_LIMIT - 10);
                        typeof(ActionGame.Cycle)
                            .GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)
                            .SetValue(cycle, newVal);
                    }
                }
            });
        }

        #endregion

        #region ForceAnal

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertAnalClick), new Type[] { })]
        public static void OnInsertAnalClickPre(HSprite __instance)
        {
            if (!Input.GetMouseButtonUp(0) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isAnalInsertOK)
                return;
            
            // Check if player can circumvent the anal deny
            if (__instance.flags.count.sonyuAnalOrg >= 1)
            {
                ForceAllowInsert(__instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertAnalNoVoiceClick), new Type[] { })]
        public static void OnInsertAnalNoVoiceClickPre(HSprite __instance)
        {
            if (!Input.GetMouseButtonUp(0) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isAnalInsertOK)
                return;

            var heroine = __instance.flags.lstHeroine[0];

            // Check if player can circumvent the anal deny
            if (CanCircumventDeny(__instance) || __instance.flags.count.sonyuAnalOrg >= 1)
            {
                MakeGirlAngry(heroine);

                ForceAllowInsert(__instance);
                __instance.flags.isDenialvoiceWait = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertAnalClick), new Type[] { })]
        public static void OnInsertAnalClickPost(HSprite __instance)
        {
            ResetForceAllowInsert(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertAnalNoVoiceClick), new Type[] { })]
        public static void OnInsertAnalNoVoiceClickPost(HSprite __instance)
        {
            ResetForceAllowInsert(__instance);
        }

        #endregion

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

            // Check if player can circumvent the raw deny 
            // OUT_A is resting after popping the cork outdoors
            if (CanCircumventDeny(__instance) ||
                girlOrgasms >= 3 + RandomGen.Next(0, 3) - heroine.lewdness / 66)
            {
                MakeGirlAngry(heroine);

                ForceAllowInsert(__instance);
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
                ForceAllowInsert(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertNoVoiceClick), new Type[] { })]
        public static void OnInsertNoVoiceClickPost(HSprite __instance)
        {
            ResetForceAllowInsert(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertClick), new Type[] { })]
        public static void OnInsertClickPost(HSprite __instance)
        {
            ResetForceAllowInsert(__instance);
        }

        private static void ForceAllowInsert(HSprite instance)
        {
            instance.flags.isDebug = true;
        }

        private static void ResetForceAllowInsert(HSprite __instance)
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

        private static bool CanCircumventDeny(HSprite __instance)
        {
            // OUT_A is resting after popping the cork outdoors
            return string.Equals(__instance.flags.nowAnimStateName, "OUT_A", StringComparison.Ordinal) ||
                   string.Equals(__instance.flags.nowAnimStateName, "A_OUT_A", StringComparison.Ordinal) ||
                   __instance.flags.isDenialvoiceWait;
        }

        #endregion
    }
}