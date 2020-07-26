using System.Linq;
using ActionGame;
using HarmonyLib;

namespace KoikatuGameplayMod
{
    internal static class FastTravelCostHooks
    {
        public static void ApplyHooks(Harmony instance)
        {
            instance.PatchAll(typeof(FastTravelCostHooks));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionMap), nameof(ActionMap.PlayerMapWarp))]
        public static void MapSelectMenuSceneRegisterCallback(MapSelectMenuScene __instance)
        {
            if (KoikatuGameplayMod.FastTravelTimePenalty.Value > 0)
            {
                var cycle = UnityEngine.Object.FindObjectsOfType<Cycle>().FirstOrDefault();
                if (cycle != null) cycle.AddTimer(KoikatuGameplayMod.FastTravelTimePenalty.Value / 500f);
            }
        }
    }
}