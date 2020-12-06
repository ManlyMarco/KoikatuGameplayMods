using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace StudioCameraTweaks
{
    [BepInPlugin(GUID, "Studio Camera Tweaks", Version)]
    [BepInProcess("CharaStudio")]
    [BepInProcess("StudioNEOV2")]
    public class StudioCameraTweaksPlugin : BaseUnityPlugin
    {
        public const string GUID = "StudioCameraTweaks";
        public const string Version = "1.0";

        private static ConfigEntry<bool> _spawnAtMaincam;
        private static ConfigEntry<bool> _turnOffByDefault;
        private static OCICamera _lastCamera;

        private void Awake()
        {
            _spawnAtMaincam = Config.Bind("Camera Object", "Spawn at current camera position", true,
                "Should clicking the Camera button in Workspace window spawn the new camera at the current viewport camera position?\n" +
                "The deafault is to spawn the new camera object at either origin point or current cursor position.");
            _turnOffByDefault = Config.Bind("Camera Object", "Hide newly created camera objects", true,
                "Automatically disable newly spawned camera objects in the workspace.\n" +
                "This will cause their gizmo to not appear, but they will still function normally.\n" +
                "Useful when spawning at current camera position to not obscure the view.");

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
                if (_turnOffByDefault.Value)
                    _lastCamera.treeNodeObject.SetVisible(false);
            }
        }
    }
}
