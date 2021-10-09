using System.Linq;
using ActionGame;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;

namespace KoikatuGameplayMod
{
    internal class FastTravelCostHooks : IFeature
    {
        public bool Install(Harmony instance, ConfigFile config)
        {
            if (KoikatuAPI.IsVR()) return false;

            _fastTravelTimePenalty = config.Bind(KoikatuGameplayMod.ConfCatMainGame, "Fast travel (F3) time cost", 50,
                new ConfigDescription("Value is in seconds. One period has 500 seconds.", new AcceptableValueRange<int>(0, 100)));

            instance.PatchAll(typeof(FastTravelCostHooks));

            return true;
        }

        private static ConfigEntry<int> _fastTravelTimePenalty;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionMap), nameof(ActionMap.PlayerMapWarp))]
        private static void MapSelectMenuSceneRegisterCallback()
        {
            if (_fastTravelTimePenalty.Value > 0)
            {
                var cycle = UnityEngine.Object.FindObjectsOfType<Cycle>().FirstOrDefault();
                if (cycle != null) cycle.AddTimer(_fastTravelTimePenalty.Value / 500f);
            }
        }
    }
}