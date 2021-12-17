using System;
using System.Collections.Generic;
using System.Linq;
using KKAPI.Utilities;
using UniRx;
using UnityEngine;

namespace KK_LewdCrestX
{
    internal static partial class ClubInterface
    {
        private static GUIStyle _btnStyleDeselected, _btnStyleSelected;

        private static Rect _screenRect, _windowRect;
        private static Vector2 _scrollPos1, _scrollPos2;
        private static int _rowCount = 3;
        private static int _charaListWidth = 380;
        private static float _singleItemSize = (_charaListWidth - 6 * 2 - 17) / _rowCount - 6;

        private static List<HeroineData> _crestableHeroines;
        private static CrestInterfaceList _crestlist;

        private static int _selHeroine;
        private static int _selCrest;

        private static bool _mouseDown;

        private static bool _showWindow;
        private static bool _showOnlyImplemented;

        public static bool ShowWindow
        {
            get => _showWindow;
            set
            {
                if (_showWindow != value)
                {
                    var actScene = LewdCrestXGameController.GetActionScene();

                    actScene._isCursorLock = !value;

                    Time.timeScale = value ? 0 : 1;

                    if (value)
                    {
                        ShowOnlyImplemented = true;

                        _screenRect = new Rect(0, 0, Screen.width, Screen.height);
                        const int windowWidth = 750;
                        const int windowHeight = 530;
                        _windowRect = new Rect((_screenRect.width - windowWidth) / 2,
                            (_screenRect.height - windowHeight) / 2, windowWidth, windowHeight);

                        if (_crestableHeroines != null)
                        {
                            foreach (var heroine in _crestableHeroines)
                                heroine.Destroy();
                        }

                        _crestableHeroines = LewdCrestXGameController.GetHeroineList()
                            .Where(x => x != null)
                            .Where(x => x.isStaff)
                            .Select(x => new HeroineData(x))
                            .Where(x => x.Controller != null)
                            .ToList();

                        _selCrest = _selHeroine = 0;
                        _scrollPos1 = _scrollPos2 = Vector2.zero;
                    }
                    else
                    {
                        foreach (var heroine in _crestableHeroines)
                        {
                            if (heroine.Controller != null)
                                heroine.Controller.SaveData();
                        }
                    }

                    _showWindow = value;
                }
            }
        }

        public static bool ShowOnlyImplemented
        {
            get => _showOnlyImplemented;
            set
            {
                if (_showOnlyImplemented != value || _crestlist == null)
                {
                    _crestlist = CrestInterfaceList.Create(value, false);
                    _showOnlyImplemented = value;
                }
            }
        }

        private static bool ClickedInsideLastElement()
        {
            var current = Event.current;
            // Has to be during repaint for GetLastRect, can't use Input to check mouse down here
            return current.type == EventType.Repaint && _mouseDown && GUILayoutUtility.GetLastRect().Contains(current.mousePosition);
        }

        public static void OnGui(Unit _)
        {
            if (!ShowWindow) return;

            if (GUI.Button(_screenRect, string.Empty, GUI.skin.box) && !_windowRect.Contains(Input.mousePosition)) ShowWindow = false;

            if (_btnStyleDeselected == null)
            {
                _btnStyleDeselected = new GUIStyle(GUI.skin.button);
                _btnStyleDeselected.normal.background = GUI.skin.button.active.background;
                _btnStyleSelected = new GUIStyle(GUI.skin.button);
                _btnStyleSelected.normal.background = GUI.skin.button.onNormal.background;
                _btnStyleSelected.hover.background = GUI.skin.button.onHover.background;
            }

            IMGUIUtils.DrawSolidBox(_windowRect);
            GUILayout.Window(47238, _windowRect, CrestWindow, "Assign Lewd Crests to club members");

            Input.ResetInputAxes();
        }

        private static void CrestWindow(int id)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0) _mouseDown = true;

            try
            {
                DrawColumns();
            }
            catch (ArgumentException e)
            {
                if (!e.Message.Contains("position in a group with only"))
                    throw;
            }

            // This has to be specifically reset in repaint at the end of this method because ongui
            // when mouse click is handled there's an extra layout added after the repaint and then the
            // mouse event is processed, so the mouseDown has to survive the entire frame before being seein in repaint
            if (Event.current.type == EventType.Repaint)
                _mouseDown = false;
        }

        private static void DrawColumns()
        {
            var selectedHeroine = _crestableHeroines[_selHeroine];
            var selectedController = selectedHeroine.Controller;

            GUILayout.BeginHorizontal();
            {
                // Left column ----------------------------------------------------------------------------------------
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(_charaListWidth), GUILayout.ExpandHeight(true));
                if (_crestableHeroines.Count == 0)
                {
                    GUILayout.FlexibleSpace();
                    var alig = GUI.skin.label.alignment;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label("Only present club memebers can be given a crest.\n\nInvite some new members and try again!");
                    GUI.skin.label.alignment = alig;
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    _scrollPos1 = GUILayout.BeginScrollView(_scrollPos1, false, true, GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true));
                    {
                        if (_singleItemSize > 0)
                        {
                            for (int i = 0; i < _crestableHeroines.Count;)
                            {
                                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                                for (int r = 0; r < _rowCount && i < _crestableHeroines.Count; i++, r++)
                                {
                                    var heroine = _crestableHeroines[i];
                                    var style = _selHeroine == i ? _btnStyleSelected : _btnStyleDeselected;
                                    GUILayout.BeginVertical(style, GUILayout.Width(_singleItemSize), GUILayout.ExpandHeight(false));
                                    {
                                        GUILayout.Label(heroine.GetFaceTex());
                                        GUILayout.Label(heroine.HeroineName);
                                        GUILayout.Label(heroine.GetCrestName());
                                    }
                                    GUILayout.EndVertical();

                                    // bug This will pick up elements even if they are scrolled outside of the scrollview.
                                    // This is mitigated by making the scrollview take whole left side, but beware when reordering.
                                    // Getting scrollview rect and checking if mouse is inside of it didn't work properly because it was offset for some reason.
                                    if (ClickedInsideLastElement())
                                    {
                                        _selHeroine = i;
                                        _selCrest = _crestlist.GetIndex(heroine.Controller.CurrentCrest);
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    GUILayout.EndScrollView();

                    //if (Event.current.type == EventType.repaint)
                    //{
                    //    _scrollRect = GUILayoutUtility.GetLastRect();
                    //    //Console.WriteLine("scrl " + _scrollRect.ToString());
                    //}
                }

                GUILayout.EndVertical();

                // Right column ----------------------------------------------------------------------------------------
                GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                {
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                    {
                        GUILayout.Label("Your club's love research includes giving other club members lewd crests. " +
                                        "Crests can have many different effects, but members don't know about them to avoid bias.");
                        GUILayout.Label("If you don't see a character, make sure they are club members. " +
                                        "Effects are applied immediately or on next period. " +
                                        "After a crest is removed effects are reverted but memories remain.");
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(160), GUILayout.ExpandWidth(true));
                    {
                        _scrollPos2 = GUILayout.BeginScrollView(_scrollPos2, false, true, GUILayout.ExpandWidth(true));
                        {
                            _selCrest = GUILayout.SelectionGrid(_selCrest, _crestlist.GetInterfaceNames(), 3, _btnStyleDeselected, GUILayout.ExpandWidth(true));
                            if (GUI.changed)
                            {
                                selectedController.CurrentCrest = _crestlist.GetType(_selCrest);
                                GUI.changed = false;
                            }
                        }
                        GUILayout.EndScrollView();
                        ShowOnlyImplemented = GUILayout.Toggle(ShowOnlyImplemented, "Show only crests with gameplay effects");
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
                    {
                        GUILayout.Label("Currently selected heroine: " + selectedHeroine.HeroineName);
                        var currentCrest = _crestlist.GetInfo(_selCrest);
                        GUILayout.Label("Currently selected crest: " + (currentCrest?.Name ?? "None"));
                        if (currentCrest != null)
                        {
                            GUILayout.Label("Description: " + currentCrest.Description);
                            selectedController.HideCrestGraphic = GUILayout.Toggle(selectedController.HideCrestGraphic, "Hide crest graphic (effect is still applied)");
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(currentCrest.Implemented
                                ? "Gameplay will be changed roughly as described."
                                : "Only for looks and lore, it won't change gameplay.");
                        }
                        else
                        {
                            GUILayout.Label("No crest selected. Choose a crest on the left to see its description and give it to the selected character.");
                        }
                    }
                    GUILayout.EndVertical();

                    if (GUILayout.Button("Close this window", GUILayout.ExpandWidth(true)))
                        ShowWindow = false;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
    }
}