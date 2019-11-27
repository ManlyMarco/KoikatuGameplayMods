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

        private Camera _mainCamera;

        private void Update()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                    return;
            }

            if (_mainCamera.orthographic && Input.mouseScrollDelta.y != 0)
            {
                _mainCamera.orthographicSize = Mathf.Max(0.1f, _mainCamera.orthographicSize + _mainCamera.orthographicSize * Input.mouseScrollDelta.y * 0.1f);
            }
            else if (ToggleOrthoCamera.IsDown())
            {
                _mainCamera.orthographic = !_mainCamera.orthographic;
            }
        }
    }
}
