using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace KoikatuGameplayMod
{
    internal static class HSceneHooks
    {
        public static void ApplyHooks(Harmony instance)
        {
            instance.PatchAll(typeof(HSceneHooks));
        }

        #region Keep first time status if chara is still a virgin
        //Not needed and has no effect in free H or official VR

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), "NewHeroineEndProc")]
        public static void NewHeroineEndProcPost(HSceneProc __instance)
        {
            OnHEnd(__instance);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), "EndProc")]
        public static void EndProcPost(HSceneProc __instance)
        {
            OnHEnd(__instance);
        }

        private static void OnHEnd(HSceneProc proc)
        {
            // If girl is stil a virgin, keep her first time status
            foreach (var heroine in proc.flags.lstHeroine)
            {
                if (heroine.isVirgin && heroine.isAnalVirgin)
                    heroine.hCount = 0;
            }
        }

        #endregion

        #region Allow to exit first hscene early
        //Not needed and has no effect in free H or official VR

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSceneProc), "Start")]
        public static void HSpriteUpdatePre(HSceneProc __instance)
        {
            // Adjust help sprite location so it doesn't cover the back button
            var rt = __instance.sprite.objFirstHHelpBase.transform.parent.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.04f, 0f);
            rt.offsetMax = Vector2.zero;
            rt.offsetMin = Vector2.zero;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), "Update")]
        public static void HSpriteUpdatePre(HSprite __instance, out bool __state)
        {
            // Skip code that hides the back button
            __state = false;
            if (__instance.flags.lstHeroine.Count != 0 && __instance.flags.lstHeroine[0].hCount == 0)
            {
                __state = true;
                __instance.flags.lstHeroine[0].hCount = 1;
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), "Update")]
        public static void HSpriteUpdatePost(HSprite __instance, bool __state)
        {
            // Restore original hcount
            if (__state)
                __instance.flags.lstHeroine[0].hCount = 0;
        }

        #endregion

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(HHoushi), nameof(HHoushi.Proc))]
        [HarmonyPatch(typeof(HSonyu), nameof(HSonyu.Proc))]
        public static IEnumerable<CodeInstruction> MaleVisibleOverrideTpl(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(HActionBase), "IsBodyTouch") ?? throw new ArgumentNullException("HActionBase.IsBodyTouch)");
            var replacement = AccessTools.Method(typeof(HSceneHooks), nameof(HSceneHooks.IsBodyTouchOverride)) ?? throw new ArgumentNullException("HSceneHooks.CanHide");
            
            foreach (var codeInstruction in instructions)
            {
                yield return codeInstruction;

                if (codeInstruction.operand == target)
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                }
            }
        }

        public static bool IsBodyTouchOverride(bool isTouch)
        {
            return isTouch && !KoikatuGameplayMod.DontHidePlayerWhenTouching.Value;
        }
    }
}