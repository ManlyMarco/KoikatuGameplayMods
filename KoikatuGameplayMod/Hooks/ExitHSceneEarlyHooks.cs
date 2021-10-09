using System;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using UnityEngine;

namespace KoikatuGameplayMod
{
    /// <summary>
    /// Allow to exit first time hscene early
    /// </summary>
    internal class ExitHSceneEarlyHooks : IFeature
    {
        public bool Install(Harmony instance, ConfigFile config)
        {
            if (KoikatuAPI.IsVR()) return false;

            instance.PatchAll(typeof(ExitHSceneEarlyHooks));

            GameAPI.EndH += OnHEnd;

            return true;
        }

        //These hooks are for H in story mode. In free H they still run but have no effect. 
        //Since free H is the only mode available in official VR, we don't need to patch the VRHScene class (the VR equivalent of HSceneProc).
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSceneProc), "Start")]
        private static void HSpriteUpdatePre(HSceneProc __instance)
        {
            // Adjust help sprite location so it doesn't cover the back button
            var rt = __instance.sprite.objFirstHHelpBase.transform.parent.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.04f, 0f);
            rt.offsetMax = Vector2.zero;
            rt.offsetMin = Vector2.zero;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSprite), "Update")]
        private static void HSpriteUpdatePre(HSprite __instance, out bool __state)
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
        private static void HSpriteUpdatePost(HSprite __instance, bool __state)
        {
            // Restore original hcount
            if (__state)
                __instance.flags.lstHeroine[0].hCount = 0;
        }
        
        /// <summary>
        /// Keep first time status if chara is still a virgin after H scene ends (only possible with these patches, normally you need to do penetration)
        /// </summary>
        private static void OnHEnd(object sender, EventArgs eventArgs)
        {
            // If girl is stil a virgin, keep her first time status
            foreach (var heroine in GameObject.FindObjectOfType<HFlag>().lstHeroine)
            {
                if (heroine.isVirgin && heroine.isAnalVirgin)
                    heroine.hCount = 0;
            }
        }
    }
}