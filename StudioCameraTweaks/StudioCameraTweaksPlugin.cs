using BepInEx;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace StudioCameraTweaks
{
    [BepInPlugin(GUID, "Studio Camera Tweaks", Version)]
    public class StudioCameraTweaksPlugin : BaseUnityPlugin
    {
        public const string GUID = "StudioCameraTweaks";
        public const string Version = "1.0";

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(StudioCameraTweaksPlugin));
        }
        
        private static OCICamera _lastCamera;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AddObjectCamera), nameof(AddObjectCamera.Add))]
        public static void AddCameraHook1(OCICamera __result)
        {
            _lastCamera = __result;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickCamera")]
        public static void AddCameraHook2()
        {
            var changeAmount = _lastCamera.objectInfo.changeAmount;
            var camera = Camera.main.transform;
            changeAmount.pos = camera.position;
            changeAmount.rot = camera.rotation.eulerAngles;
        }
    }
}
