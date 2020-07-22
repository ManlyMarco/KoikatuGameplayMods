using HarmonyLib;
using UnityEngine;

namespace KoikatuGameplayMod
{
    internal static class ExitFirstHHooks
    {
        private static HSceneProc _hproc;

        public static void ApplyHooks(Harmony instance)
        {
            instance.PatchAll(typeof(ExitFirstHHooks));
        }

        #region Keep first time status if chara is still a virgin

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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileStatus), nameof(ChaFileStatus.visibleBodyAlways), MethodType.Setter)]
        public static void MaleVisibleOverride(ChaFileStatus __instance, ref bool value)
        {
            // Make sure to only work in h scenes
            // Prevent expensive FindObjectOfType with extra checks beforehand
            if (value || !KoikatuGameplayMod.DontHidePlayerWhenTouching.Value) return;
            if (Manager.Scene.Instance.AddSceneName != "HProc") return;
            if (_hproc == null) _hproc = Object.FindObjectOfType<HSceneProc>();
            if (_hproc != null)
            {
                var m = Traverse.Create(_hproc).Field<ChaControl>("male").Value;
                if (m.fileStatus == __instance)
                    value = true;
            }
        }
    }
}