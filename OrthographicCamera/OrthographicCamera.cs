using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace KK_OrthographicCamera
{
    [BepInPlugin(GUID, GUID, Version)]
    [DefaultExecutionOrder(1000)]
    public class OrthographicCamera : BaseUnityPlugin
    {
        public const string GUID = "KK_OrthographicCamera";
        internal const string Version = "1.1.1";

        public ConfigEntry<KeyboardShortcut> ToggleOrthoCamera { get; private set; }
        public bool ForceOrthographicSize;

        private void Start()
        {
            ToggleOrthoCamera = Config.Bind("", "Toggle orthographic mode", new KeyboardShortcut(KeyCode.I));
            // No need to force it in koi studio
            ForceOrthographicSize = Application.productName != "CharaStudio";
        }

        private Camera _mainCamera;
        private float _orthoSize;

        private void LateUpdate()
        {
            if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                    return;
                _orthoSize = _mainCamera.orthographicSize;
            }

            if (_mainCamera.orthographic)
            {
                if (!ForceOrthographicSize)
                    _orthoSize = _mainCamera.orthographicSize;

                if (Input.mouseScrollDelta.y != 0)
                    _orthoSize = Mathf.Max(0.1f, _orthoSize + _orthoSize * Input.mouseScrollDelta.y * 0.1f);

                _mainCamera.orthographicSize = _orthoSize;
            }

            if (ToggleOrthoCamera.Value.IsDown())
            {
                _mainCamera.orthographic = !_mainCamera.orthographic;
                if (_mainCamera.orthographic)
                    _orthoSize = _mainCamera.orthographicSize;
            }
        }
    }
}
