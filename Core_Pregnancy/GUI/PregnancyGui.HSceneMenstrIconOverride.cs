using System;
using HarmonyLib;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Pregnancy
{
    public static partial class PregnancyGui
    {
        private static class HSceneMenstrIconOverride
        {
            private static Sprite _unknownSprite;
            private static Sprite _pregSprite;
            private static Sprite _safeSprite;
            private static Sprite _riskySprite;
            private static Sprite _leaveSprite;

            public static void Init(Harmony hi, Sprite unknownSprite, Sprite pregSprite, Sprite safeSprite, Sprite riskySprite, Sprite leaveSprite)
            {
                if (PregnancyPlugin.HSceneMenstrIconOverride.Value)
                {
                    _unknownSprite = unknownSprite ? unknownSprite : throw new ArgumentNullException(nameof(unknownSprite));
                    _pregSprite = pregSprite ? pregSprite : throw new ArgumentNullException(nameof(pregSprite));
                    _riskySprite = riskySprite ? riskySprite : throw new ArgumentNullException(nameof(riskySprite));
                    _safeSprite = safeSprite ? safeSprite : throw new ArgumentNullException(nameof(safeSprite));
                    _leaveSprite = leaveSprite ? leaveSprite : throw new ArgumentNullException(nameof(leaveSprite));
                    hi.PatchAll(typeof(HSceneMenstrIconOverride));
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
                    // Add the custom icons if necessary
                    if (__instance.categoryMenstruation.lstObj.Count == 2)
                    {
                        var original = __instance.categoryMenstruation.lstObj[0];
                        void AddNewState(Sprite sprite)
                        {
                            var copy = UnityEngine.Object.Instantiate(original, original.transform.parent, false);
                            copy.GetComponent<Image>().sprite = sprite;
                            __instance.categoryMenstruation.lstObj.Add(copy);
                        }
                        AddNewState(_unknownSprite); // index 2
                        AddNewState(_pregSprite);
                        AddNewState(_safeSprite);
                        AddNewState(_riskySprite);
                        AddNewState(_leaveSprite);
                    }

                    var heroineStatus = __instance.GetLeadingHeroine().GetHeroineStatus();
                    switch (heroineStatus)
                    {
                        case HeroineStatus.Unknown:
                            __instance.categoryMenstruation.SetActiveToggle(2);
                            break;
                        case HeroineStatus.Pregnant:
                            __instance.categoryMenstruation.SetActiveToggle(3);
                            break;
                        case HeroineStatus.Safe:
                            __instance.categoryMenstruation.SetActiveToggle(4);
                            break;
                        case HeroineStatus.Risky:
                            __instance.categoryMenstruation.SetActiveToggle(5);
                            break;
                        case HeroineStatus.OnLeave:
                            __instance.categoryMenstruation.SetActiveToggle(6);
                            break;
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