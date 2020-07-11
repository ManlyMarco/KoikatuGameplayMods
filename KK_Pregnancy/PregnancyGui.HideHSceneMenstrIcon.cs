using System;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Pregnancy
{
    public static partial class PregnancyGui
    {
        private static class HideHSceneMenstrIcon
        {
            private static Sprite _unknownSprite;

            public static void Init(Harmony hi, Sprite unknownSprite)
            {
                if (PregnancyPlugin.HideHSceneMenstrIcon.Value)
                {
                    _unknownSprite = unknownSprite ?? throw new ArgumentNullException(nameof(unknownSprite));
                    hi.PatchAll(typeof(HideHSceneMenstrIcon));
                }
            }

            /// <summary>
            /// Turn off the safe/risky indicator if the character didn't tell you their schedule yet
            /// You have to listen for the cues instead
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.Init))]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.InitHeroine))]
            private static void HideMenstrIconIfNeeded(HSprite __instance)
            {
                try
                {
                    if (StatusIcons.GetHeroineStatus(__instance.GetLeadingHeroine()) ==
                        StatusIcons.HeroineStatus.Unknown)
                    {
                        if (__instance.categoryMenstruation.lstObj.Count == 2)
                        {
                            var orig = __instance.categoryMenstruation.lstObj[0];
                            var copy = UnityEngine.Object.Instantiate(orig, orig.transform.parent, false);
                            copy.GetComponent<Image>().sprite = _unknownSprite;
                            __instance.categoryMenstruation.lstObj.Add(copy);
                        }

                        // -1 disables everything
                        __instance.categoryMenstruation.SetActiveToggle(2);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }
}