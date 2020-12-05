using BepInEx;
using BepInEx.Configuration;
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

        private static ConfigEntry<bool> _spawnAtMaincam;
        private static OCICamera _lastCamera;

        private void Awake()
        {
            _spawnAtMaincam = Config.Bind("Camera Object", "Spawn at current camera position", true,
                "Should clicking the Camera button in Workspace window spawn the new camera at the current viewport camera position?\n" +
                "The deafault is to spawn the new camera object at either origin point or current cursor position.");

            Harmony.CreateAndPatchAll(typeof(StudioCameraTweaksPlugin));
        }

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
            if (_spawnAtMaincam.Value)
            {
                var changeAmount = _lastCamera.objectInfo.changeAmount;
                var camera = Camera.main.transform;
                changeAmount.pos = camera.position;
                changeAmount.rot = camera.rotation.eulerAngles;
            }
        }
    }
}
