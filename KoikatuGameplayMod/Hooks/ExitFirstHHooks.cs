using HarmonyLib;
using UnityEngine;

namespace KoikatuGameplayMod
{
    internal static class ExitFirstHHooks
    {
        public static void ApplyHooks(Harmony instance)
        {
            instance.PatchAll(typeof(ExitFirstHHooks));
        }

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
    }
}