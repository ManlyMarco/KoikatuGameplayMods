using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using UnityEngine;
using UnityEngine.UI;

namespace KoikatuGameplayMod
{
    internal class HeroinesLimitUnlockHooks : IFeature
    {
        private static ConfigEntry<int> _addHeroins;

        public bool Install(Harmony instance, ConfigFile config)
        {
            if (KoikatuAPI.IsVR())
                return false;

            _addHeroins = config.Bind(KoikatuGameplayMod.ConfCatMainGame, "Increase max number of character slots", 0, new ConfigDescription("Maximum number of heroines in each location will be increased by this amount. You have to reopen the roster window after changing this setting.", new AcceptableValueRange<int>(0, 500)));

            instance.PatchAll(typeof(HeroinesLimitUnlockHooks));
            return true;
        }

        private static GameObject _seat24;
        private const int BaseSeats = 25;
        
        /// <summary>
        /// This patch is based on code provided by BitMagnet under GPL-3.0 license.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(ActionGame.ClassRoomList), nameof(ActionGame.ClassRoomList.charaPreviewList), MethodType.Getter)]
        private static void GetClassRegisterMaxPrefix(ActionGame.ClassRoomList __instance)
        {
            if (_addHeroins.Value <= 0) return;

            if (!_seat24) _seat24 = GameObject.Find("/ClassRoomSelectScene/classroomcanvas/Seat/Seat_24");

            if (!_seat24) return;

            var parent = _seat24.transform.parent;

            if (parent.childCount <= BaseSeats)
            {
                //System.Console.WriteLine($"@@@@ {__instance.PreviewRoot.transform.GetFullPath()}");

                for (int i = 0; i < _addHeroins.Value; ++i)
                {
                    var seat = GameObject.Instantiate(_seat24, parent);

                    int index = i + (BaseSeats + 1);
                    seat.name = "Seat_" + index;

                    var transform = (RectTransform)seat.transform;
                    transform.localPosition = new Vector2(index % 5 * 272 + 4, index / 5 * -176 - 4);
                }
            }

            // Check if the list is already initialized
            if (parent.GetComponent<GridLayoutGroup>()) return;

            var scrollGO = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent.parent, false);
            scrollGO.transform.SetSiblingIndex(1);
            RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();

            // 全画面サイズにフィットさせる例 / Example of fitting to full screen size
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(200, 0);
            scrollRT.offsetMax = new Vector2(200, -20);
            scrollRT.sizeDelta = Vector2.zero;

            // 背景 Image（透けさせたい場合は alpha を下げる）
            // Background Image (lower alpha if you want it to be transparent)
            //var bgImage = scrollGO.GetComponent<Image>();
            //bgImage.color = new Color(1, 1, 0, 0.5f);

            // Mask で子を切り抜き
            // Mask to clip children
            //var mask = scrollGO.GetComponent<Mask>();
            //mask.showMaskGraphic = false;

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;   // 横スクロール不要なら false / No horizontal scroll if not needed
            scrollRect.vertical = true;    // 縦スクロール有効 / Enable vertical scroll

            // 3. Viewport の作成 / Creating Viewport
            var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGO.transform.SetParent(scrollGO.transform, false);
            RectTransform vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(0, 100);
            vpRT.offsetMax = new Vector2(-550, -120);
            vpRT.sizeDelta = new Vector2(-550, -200);


            var bgImage = vpGO.GetComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 2f / 255f);

            scrollRect.scrollSensitivity = 10f;
            scrollRect.viewport = vpRT;

            var contentGO = parent.gameObject;

            contentGO.transform.SetParent(vpGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();

            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(0, 1);
            contentRT.pivot = new Vector2(0, 1);
            contentRT.anchoredPosition = Vector2.zero;
            // サイズは最初ゼロ、フィッターで自動調整 / Size starts at zero, automatically adjusted by fitter
            contentRT.sizeDelta = Vector2.zero;

            scrollRect.content = contentRT;

            var grid = contentGO.GetOrAddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(262, 166);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            //System.Console.WriteLine("@@@ ddd");
        }
    }
}
