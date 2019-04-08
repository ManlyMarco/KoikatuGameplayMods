using System;
using System.Linq;
using System.Reflection;
using Harmony;
using UniRx;

namespace KoikatuGameplayMod
{
    internal static class FastTravelCostHooks
    {
        public static void ApplyHooks(HarmonyInstance instance)
        {
            instance.PatchAll(typeof(FastTravelCostHooks));
        }

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
    }
}