using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_NightDarkener
{
    [BepInPlugin(GUID, "Night Darkener", Version)]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    public class NightDarkener : BaseUnityPlugin
    {
        public const string GUID = "Marco.NightDarkener";
        internal const string Version = "1.2";

        private static readonly int[] _allowedMaps =
        {
#if KK
            0,  // MyRoom
            3,  // 2F
            4,  // 3F
            8,  // 2-1
            9,  // 2-2
            11, // 3-1
            15, // 2F Toilet
            16, // 3F Toilet
            17, // Staff room
            18, // Male Toilet
            20, // Nurse room
            21, // Library
            22, // Club room
            26, // Comic club room
            28, // Tea club room
            31, // Gym warehouse
            32, // Gym
            33, // Courtyard
            34, // Sport ground
            36, // Rooftom
            37, // Pool
            38, // Dining
            45, // Showers
            46, // Lockers
            47  // Backyard
#elif KKS
            0,  // Hotel
            1,  // Guest House
            2,  // Training Center
            3,  // Harbor
            4,  // Beach
            5,  // Nature Park
            6,  // Lighthouse
            7,  // Aquarium
            8,  // Stone Wall Pathway
            9,  // Secret Beach
            28, // N. Park Public Bathroom
            33, // Beach Changing Room
            36, // Suite
#endif
        };

        public static ConfigEntry<bool> BeSmart { get; private set; }
        public static ConfigEntry<bool> UseFog { get; private set; }
        public static ConfigEntry<float> Exposure { get; private set; }

        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            var proc = FindObjectOfType<HSceneProc>();

            if (proc == null || !proc.dataH.isFreeH) return;

            var time = (SunLightInfo.Info.Type)proc.dataH.timezoneFreeH;
            if (time != SunLightInfo.Info.Type.Night) return;

            if (BeSmart.Value && !_allowedMaps.Contains(proc.dataH.mapNoFreeH)) return;

            var amplifyColorEffect = Camera.main?.gameObject.GetComponent<AmplifyColorEffect>();
            if (amplifyColorEffect != null)
                amplifyColorEffect.Exposure = Exposure.Value;

            var lightInfo = FindObjectOfType<SunLightInfo>();

            foreach (var info in lightInfo.infos)
            {
                if (info.type != SunLightInfo.Info.Type.Night) continue;

                if (UseFog.Value)
                {
                    info.fogColor = SubtractColor(info.fogColor, 0.1f);
                    info.fogStart = 1;
                    info.fogEnd = 50;
                }

                //bug does this do anything?
                info.color = SubtractColor(info.color, 0.2f);
                info.intensity = info.intensity / 2f;
            }
        }

        private void Start()
        {
            UseFog = Config.Bind("General", "Enable dark fog at night", false, "Horror game effect.\nChanges take effect next time you load a night map.");
            Exposure = Config.Bind("General", "Exposure at night", 0.3f, new ConfigDescription("The lower the exposure, the darker the game will be.\nChanges take effect next time you load a night map.", new AcceptableValueRange<float>(0, 1)));
            BeSmart = Config.Bind("General", "Only on specific maps", true, "Only darken maps that are unlikely to have lights turned on at night (likely to be vacant). Turn off to make all maps dark at night.");

            Harmony.CreateAndPatchAll(typeof(NightDarkener));
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static Color SubtractColor(Color color, float amount)
        {
            return new Color(color.r - amount, color.g - amount, color.g - amount);
        }
    }
}
