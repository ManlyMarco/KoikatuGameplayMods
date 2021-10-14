using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using KKAPI;
using Manager;

namespace KoikatuGameplayMod
{
    [BepInPlugin(GUID, "Koikatu Gameplay Tweaks and Improvements", Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInProcess(KoikatuAPI.VRProcessName)]
#if KK
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
    [BepInProcess(KoikatuAPI.VRProcessNameSteam)]
    [BepInIncompatibility("fulmene.experiencelogic")]
#endif
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class KoikatuGameplayMod : BaseUnityPlugin
    {
        public const string GUID = "marco-gameplaymod";
        public const string Version = "2.2";

        internal const string ConfCatMainGame = "Main game";
        internal const string ConfCatHScene = "H Scene tweaks";

        private void Start()
        {
            var i = new Harmony(GUID);

            var featureT = typeof(IFeature);
            var types = typeof(KoikatuGameplayMod).Assembly.GetTypes().Where(x => featureT.IsAssignableFrom(x) && x.IsClass);

            var successful = new List<string>();
            foreach (var type in types)
            {
                var feature = (IFeature)Activator.CreateInstance(type);
                if (feature.Install(i, Config))
                    successful.Add(type.Name);
            }
            Logger.LogInfo("Loaded features: " + string.Join(", ", successful.ToArray()));
        }

        internal static List<SaveData.Heroine> GetHeroineList()
        {
#if KK
            return Game.Instance.HeroineList;
#else
            return Game.HeroineList;
#endif
        }
    }
}
