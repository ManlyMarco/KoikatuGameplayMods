using System.Collections.Generic;
using System.ComponentModel;
using BepInEx;
using Harmony;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_NightDarkener
{
    [BepInProcess("Koikatu")]
    [BepInPlugin(GUID, "KK Night Darkener", Version)]
    public class NightDarkener : BaseUnityPlugin
    {
        public const string GUID = "Marco.NightDarkener";
        internal const string Version = "1.0";

        private static readonly HashSet<SunLightInfo> AdjustedList = new HashSet<SunLightInfo>();
        
        [DisplayName("Enable dark fog at night")]
        [Description("Horror game effect.\nChanges take effect next time you load a night map.")]
        public static ConfigWrapper<bool> UseFog { get; private set; }

        [DisplayName("Exposure at night")]
        [Description("The lower the exposure, the darker the game will be.\nChanges take effect next time you load a night map.")]
        [AcceptableValueRange(0f, 1f)]
        public static ConfigWrapper<float> Exposure { get; private set; }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SunLightInfo), nameof(SunLightInfo.Set))]
        public static void SunLightInfoHook(SunLightInfo __instance, SunLightInfo.Info.Type type)
        {
            if (type == SunLightInfo.Info.Type.Night)
            {
                var amplifyColorEffect = Camera.main?.gameObject.GetComponent<AmplifyColorEffect>();
                if (amplifyColorEffect != null)
                    amplifyColorEffect.Exposure = Exposure.Value;
            }

            if (AdjustedList.Add(__instance))
            {
                foreach (var info in __instance.infos)
                {
                    if (info.type == SunLightInfo.Info.Type.Night)
                    {
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
            }
        }

        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg1 == LoadSceneMode.Single)
                AdjustedList.Clear();
        }

        private void Start()
        {
            UseFog = new ConfigWrapper<bool>("UseFog", this);
            Exposure = new ConfigWrapper<float>("NightExposure", this, 0.3f);
            
            HarmonyInstance.Create(GUID).PatchAll(typeof(NightDarkener));
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static Color SubtractColor(Color color, float amount)
        {
            return new Color(color.r - amount, color.g - amount, color.g - amount);
        }
    }
}
