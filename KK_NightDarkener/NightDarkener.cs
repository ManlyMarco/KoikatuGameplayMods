using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_NightDarkener
{
    [BepInPlugin(GUID, "Night Darkener", Version)]
    [BepInProcess(GameProcessName)]
    [BepInProcess(GameProcessNameSteam)]
    public class NightDarkener : BaseUnityPlugin
    {
        public const string GUID = "Marco.NightDarkener";
        internal const string Version = "1.2";

        private const string GameProcessName = "Koikatu";
        private const string GameProcessNameSteam = "Koikatsu Party";

        private static readonly int[] _allowedMaps = { 0, 3, 4, 8, 9, 11, 15, 16, 17, 18, 20, 21, 22, 26, 28, 31, 32, 33, 34, 36, 37, 38, 45, 46, 47 };

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
