using System.Collections.Generic;
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

        private static readonly HashSet<SunLightInfo> _adjustedList = new HashSet<SunLightInfo>();

        private static NightDarkener instance;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SunLightInfo), nameof(SunLightInfo.Set))]
        public static void SunLightInfoHook(SunLightInfo __instance, SunLightInfo.Info.Type type)
        {
            if (instance == null) return;

            if (type == SunLightInfo.Info.Type.Night)
            {
                //todo different lut for night?
                var amplifyColorEffect = Camera.main?.gameObject.GetComponent<AmplifyColorEffect>();
                if (amplifyColorEffect != null)
                    amplifyColorEffect.Exposure = 0.5f;
            }

            if (_adjustedList.Add(__instance))
            {
                foreach (var info in __instance.infos)
                {
                    if (info.type == SunLightInfo.Info.Type.Night)
                    {
                        info.fogColor = SubtractColor(info.fogColor, 0.1f);
                        info.fogStart = 1;
                        info.fogEnd = 50;

                        //bug does this do anything?
                        info.color = SubtractColor(info.color, 0.2f);
                        info.intensity = info.intensity / 2f;
                    }
                }
            }
        }

        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (instance == null) return;
            if (arg1 == LoadSceneMode.Single)
                _adjustedList.Clear();
        }

        private void Start()
        {
            instance = this;
            HarmonyInstance.Create(GUID).PatchAll(typeof(NightDarkener));
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static Color SubtractColor(Color color, float amount)
        {
            return new Color(color.r - amount, color.g - amount, color.g - amount);
        }
    }
}
