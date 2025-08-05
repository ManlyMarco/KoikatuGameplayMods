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

            _addHeroins = config.Bind(KoikatuGameplayMod.ConfCatMainGame, "Increase max number of character slots", 0, new ConfigDescription(
                                          "Maximum number of heroines in each location will be increased by this amount. You have to reopen the roster window after changing this setting.\n" +
                                          "WARNING: Before lowering this setting make sure to remove any characters in slots that will no longer exist! If you don't do this you may encounter random bugs.",
                                          new AcceptableValueRange<int>(0, 1500)));

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
                for (int i = 0; i < _addHeroins.Value; ++i)
                {
                    var seat = GameObject.Instantiate(_seat24, parent);

                    int index = i + BaseSeats;
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
            scrollRT.anchorMin = new Vector2(0.095f, 0.08f);
            scrollRT.anchorMax = new Vector2(0.82f, 0.89f);
            scrollRT.sizeDelta = new Vector2(25, 0);
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;   // 横スクロール不要なら false / No horizontal scroll if not needed
            scrollRect.vertical = true;    // 縦スクロール有効 / Enable vertical scroll

            // 3. Viewport の作成 / Creating Viewport
            var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGO.transform.SetParent(scrollGO.transform, false);
            RectTransform vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            vpRT.sizeDelta = Vector2.zero;


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
            grid.padding = new RectOffset(10, 10, 0, 0);

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            var originalScrollBarGObj = GameObject.Find("ClassRoomSelectScene/previewcharacanvas/charaFileControl/charaFileWindow/WinRect/ListArea/Scroll View/Scrollbar Vertical");

            if(originalScrollBarGObj != null)
            {
                var scrollbar = originalScrollBarGObj.GetComponent<Scrollbar>();

                // コピー元のScrollbarを複製 / Duplicate the original Scrollbar
                Scrollbar newScrollbar = GameObject.Instantiate(scrollbar, scrollRect.transform);

                // ScrollRectと連携 / Link the ScrollRect with the new scrollbar
                scrollRect.verticalScrollbar = newScrollbar;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

                RectTransform scrollbarRT = newScrollbar.GetComponent<RectTransform>();

                // Viewportのサイズ調整（必要に応じて）/ Adjust the size of the Viewport (if necessary)
                if (scrollRect.viewport != null)
                {
                    // Scrollbarの位置を右に配置（仮に幅20）/ Position the scrollbar to the right (assuming width 20)
                    scrollbarRT.anchorMin = new Vector2(1, 0);
                    scrollbarRT.anchorMax = new Vector2(1, 1);
                    scrollbarRT.pivot = new Vector2(1, 1);
                    scrollbarRT.sizeDelta = new Vector2(20, 0);
                    scrollbarRT.anchoredPosition = new Vector2(+2, 0);
                }

                scrollbarRT.SetParent(scrollbarRT.parent.parent, true);
            }
        }
    }
}
