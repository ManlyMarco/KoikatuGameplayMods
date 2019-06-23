using BepInEx;
using UnityEngine;

namespace KK_OrthographicCamera
{
    [BepInPlugin(GUID, GUID, Version)]
    public class OrthographicCamera : BaseUnityPlugin
    {
        public const string GUID = "KK_OrthographicCamera";
        internal const string Version = "1.0";

        public SavedKeyboardShortcut ToggleOrthoCamera { get; private set; }

        private void Start()
        {
            ToggleOrthoCamera = new SavedKeyboardShortcut(nameof(ToggleOrthoCamera), this, new KeyboardShortcut(KeyCode.I));
        }

        private void Update()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (ToggleOrthoCamera.IsDown())
            {
                cam.orthographic = !cam.orthographic;
            }
            else if (cam.orthographic && !Mathf.Approximately(Input.mouseScrollDelta.y, 0))
            {
                cam.orthographicSize = Mathf.Max(0.1f, cam.orthographicSize + cam.orthographicSize * Input.mouseScrollDelta.y * 0.1f);
            }
        }
    }
}
