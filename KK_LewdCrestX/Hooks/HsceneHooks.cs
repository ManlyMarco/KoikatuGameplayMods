using HarmonyLib;
using KKAPI.Utilities;
using StrayTech;
using UnityEngine;

namespace KK_LewdCrestX
{
    internal static class HsceneHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddAibuOrg))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
        public static void OnOrg(HFlag __instance)
        {
            var h = __instance.GetLeadingHeroine();
            var crestType = LewdCrestXPlugin.GetCurrentCrest(h);
            switch (crestType)
            {
                case CrestType.mindmelt:
                    // This effect makes character slowly forget things on every org
                    h.favor = Mathf.Clamp(h.favor - 10, 0, 100);
                    h.intimacy = Mathf.Clamp(h.intimacy - 8, 0, 100);

                    h.anger = Mathf.Clamp(h.anger - 10, 0, 100);
                    if (h.anger == 0) h.isAnger = false;

                    if (Random.value < 0.2f) h.isDate = false;

                    // In exchange they get lewder
                    h.lewdness = Mathf.Clamp(h.lewdness + 30, 0, 100);

                    var orgCount = __instance.GetOrgCount();
                    if (orgCount >= 2)
                    {
                        if (h.favor == 0 && h.intimacy == 0)
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
    }
}