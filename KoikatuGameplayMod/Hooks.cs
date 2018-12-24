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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), "Start", new Type[] { })]
        public static void HookToEndHButton(HSprite __instance)
        {
            var f = typeof(HSprite).GetField("btnEnd",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var b = f.GetValue(__instance) as UnityEngine.UI.Button;

            // Modify girl's lewdness after exiting h scene based on scene stats
            b.OnClickAsObservable().Subscribe(unit =>
            {
                UpdateGirlLewdness(__instance);
                ApplyGirlAnger(__instance);
            });
        }

        #region GirlEroLevel

        private static void UpdateGirlLewdness(HSprite __instance)
        {
            if(!KoikatuGameplayMod.DecreaseLewd.Value) return;
            
            var flags = __instance.flags;
            var count = flags.count;
            var heroine = GetTargetHeroine(__instance);
            if (heroine == null) return;

            if (flags.GetOrgCount() == 0)
            {
                var massageTotal = (int)(count.selectAreas.Sum() / 4 + (+count.kiss + count.houshiOutside + count.houshiInside) * 10);
                if (massageTotal <= 5)
                    heroine.lewdness = Math.Max(0, heroine.lewdness - 30);
                else
                    heroine.lewdness = Math.Min(100, heroine.lewdness + massageTotal);
            }
            else if (count.aibuOrg > 0 && count.sonyuOrg + count.sonyuAnalOrg == 0)
                heroine.lewdness = Math.Min(100, heroine.lewdness - (count.aibuOrg - 1) * 20);
            else
            {
                int cumCount = count.sonyuCondomInside + count.sonyuInside + count.sonyuOutside + count.sonyuAnalCondomInside + count.sonyuAnalInside + count.sonyuAnalOutside;
                if (cumCount > 0)
                    heroine.lewdness = Math.Max(0, heroine.lewdness - cumCount * 20);

                heroine.lewdness = Math.Max(0, heroine.lewdness - count.aibuOrg * 20);
            }
        }

        #endregion

        #region FastTravelCost

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapSelectMenuScene), "Start", new Type[] { })]
        public static void MapSelectMenuSceneRegisterCallback(MapSelectMenuScene __instance)
        {
            var f = typeof(MapSelectMenuScene).GetField("enterButton",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var b = f.GetValue(__instance) as UnityEngine.UI.Button;

            // Add a time penalty for using F3 fast travel
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
                var heroine = GetTargetHeroine(__instance);

                MakeGirlAngry(heroine, 20, 10);

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

            var heroine = GetTargetHeroine(__instance);
            if (heroine == null) return;

            // Check if player can circumvent the anal deny
            if (!KoikatuGameplayMod.ForceInsert.Value) return;
            if (CanCircumventDeny(__instance) || __instance.flags.count.sonyuAnalOrg >= 1)
            {
                MakeGirlAngry(heroine, 30, 15);

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

            if (__instance.flags.isInsertOK[GetTargetHeroineId(__instance)])
                return;

            var heroine = GetTargetHeroine(__instance);
            var girlOrgasms = __instance.flags.count.sonyuOrg;

            // Check if player can circumvent the raw deny
            if (!KoikatuGameplayMod.ForceInsert.Value) return;
            if (CanCircumventDeny(__instance) ||
                girlOrgasms >= 3 + RandomGen.Next(0, 3) - heroine.lewdness / 66)
            {
                MakeGirlAngry(heroine, 20, 10);

                ForceAllowInsert(__instance);
                __instance.flags.isDenialvoiceWait = false;
            }
        }

        private static SaveData.Heroine GetTargetHeroine(HSprite __instance)
        {
            return __instance.flags.lstHeroine[GetTargetHeroineId(__instance)];
        }

        private static int GetTargetHeroineId(HSprite __instance)
        {
            return (__instance.flags.mode >= HFlag.EMode.houshi3P) ? (__instance.flags.nowAnimationInfo.id % 2) : 0;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnInsertClick), new Type[] { })]
        public static void OnInsertClickPre(HSprite __instance)
        {
            if (!Input.GetMouseButtonUp(0) || !__instance.IsSpriteAciotn())
                return;

            if (__instance.flags.isInsertOK[GetTargetHeroineId(__instance)])
                return;

            var heroine = GetTargetHeroine(__instance);
            if (heroine == null) return;
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
            if(!KoikatuGameplayMod.ForceInsertAnger.Value) return;

            var heroine = GetTargetHeroine(__instance);
            if (heroine == null) return;

            if (!__instance.flags.isInsertOK[GetTargetHeroineId(__instance)])
            {
                if (__instance.flags.count.sonyuInside > 0)
                {
                    if(HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日)
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
                else if(__instance.flags.count.sonyuOutside > 0)
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