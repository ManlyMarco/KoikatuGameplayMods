using System;
using HarmonyLib;
using KKAPI.MainGame;
using KKAPI.Utilities;
using Manager;
using StrayTech;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KK_LewdCrestX
{
    internal static class HsceneHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiInside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuInside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuCondomInside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalInside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalCondomInside))]
        public static void OnInsideFinish(HFlag __instance)
        {
            try
            {
                if (__instance.player != null)
                {
                    var heroine = __instance.GetLeadingHeroine();
                    var currentCrest = heroine.GetCurrentCrest();
                    if (currentCrest == CrestType.siphoning)
                    {
                        __instance.player.physical = Mathf.Min(100, __instance.player.physical + 15);
                        __instance.player.intellect = Mathf.Min(100, __instance.player.intellect + 10);
                        __instance.player.hentai = Mathf.Min(100, __instance.player.hentai + 5);
                        GameAPI.GetActionControl()?.AddDesire(22, heroine, 35);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuInside))]
        public static void OnInsideFinish2(HFlag __instance)
        {
            try
            {
                if (__instance.player != null)
                {
                    var heroine = __instance.GetLeadingHeroine();
                    var currentCrest = heroine.GetCurrentCrest();
                    if (currentCrest == CrestType.breedgasm)
                        PreggersHooks.ApplyTempPreggers(heroine);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddAibuOrg))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
        public static void OnOrg(HFlag __instance)
        {
            try
            {
                var h = __instance.GetLeadingHeroine();
                var crestType = h.GetCurrentCrest();
                switch (crestType)
                {
                    case CrestType.mindmelt:
                        // This effect makes character slowly forget things on every org
                        h.favor = Mathf.Clamp(h.favor - 10, 0, 100);
#if KK
                        h.intimacy = Mathf.Clamp(h.intimacy - 8, 0, 100);

                        h.anger = Mathf.Clamp(h.anger - 10, 0, 100);
                        if (h.anger == 0) h.isAnger = false;
                        
                        if (Random.value < 0.15f) h.isDate = false;
#else
                        if (Random.value < 0.15f) h.isDayH = false;
                        if (Random.value < 0.15f) h.isDresses = false;
#endif
                        if (Random.value < 0.15f) h.isLunch = false;

                        // In exchange they get lewder
                        h.lewdness = Mathf.Clamp(h.lewdness + 30, 0, 100);

                        var orgCount = __instance.GetOrgCount();
                        if (orgCount >= 2)
                        {
#if KK
                            if (h.favor == 0 && h.intimacy == 0)
#else
                            if (h.favor == 0)
#endif
                            {
                                h.isGirlfriend = false;
                                if (Random.value < 0.2f) h.confessed = false;
                            }

                            if (h.isKiss && Random.value < 0.1f) h.isKiss = false;
                            else if (!h.isAnalVirgin && Random.value < 0.1f) h.isAnalVirgin = true;
                            else if (Random.value < 0.3f + orgCount / 10f)
                            {
                                // Remove a random seen event so she acts like it never happened
                                var randomEvent = h.talkEvent.GetRandomElement();
                                var isMeetingEvent = randomEvent == 0 || randomEvent == 1;
                                if (isMeetingEvent)
                                {
                                    if (h.talkEvent.Count <= 2)
                                        h.talkEvent.Clear();
                                }
                                else
                                {
                                    h.talkEvent.Remove(randomEvent);
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
        }
    }
}