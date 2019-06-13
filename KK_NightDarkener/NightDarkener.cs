using System.ComponentModel;
using System.Linq;
using BepInEx;
using Harmony;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_NightDarkener
{
    [BepInPlugin(GUID, "Night Darkener", Version)]
    public class NightDarkener : BaseUnityPlugin
    {
        public const string GUID = "Marco.NightDarkener";
        internal const string Version = "1.1.1";
        
        [DisplayName("Enable dark fog at night")]
        [Description("Horror game effect.\nChanges take effect next time you load a night map.")]
        public static ConfigWrapper<bool> UseFog { get; private set; }

        [DisplayName("Exposure at night")]
        [Description("The lower the exposure, the darker the game will be.\nChanges take effect next time you load a night map.")]
        [AcceptableValueRange(0f, 1f)]
        public static ConfigWrapper<float> Exposure { get; private set; }
        
        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            var proc = FindObjectOfType<HSceneProc>();

            if (proc == null || !proc.dataH.isFreeH) return;

            var time = (SunLightInfo.Info.Type)proc.dataH.timezoneFreeH;
            if (time != SunLightInfo.Info.Type.Night) return;

            var allowedMaps = new[] { 0, 3, 4, 8, 9, 11, 15, 16, 17, 18, 20, 21, 22, 26, 28, 31, 32, 33, 34, 36, 37, 38, 45, 46, 47 };
            if (!allowedMaps.Contains(proc.dataH.mapNoFreeH)) return;

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
            if (Application.productName == "CharaStudio")
            {
                BepInEx.Bootstrap.Chainloader.Plugins.Remove(this);
                Destroy(this);
                return;
            }

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
