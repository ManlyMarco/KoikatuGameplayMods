using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using UniRx;
using UnityEngine;

namespace KK_LewdCrestX
{
    internal static partial class ClubInterface
    {
        private static List<HeroineData> _crestableHeroines;
        private static Rect _screenRect;
        private static Rect _windowRect;
        private static Vector2 _scrollPos1;
        private static Vector2 _scrollPos2;
        private static int _selHeroine;
        private static int _selCrest;
        private static CrestInterfaceList _crestlist;
        private static bool _showWindow;
        private static bool _showOnlyImplemented;

        public static bool ShowWindow
        {
            get => _showWindow;
            set
            {
                if (_showWindow != value)
                {
                    var actScene = Singleton<Game>.Instance.actScene;
                    var lockField = Traverse.Create(actScene).Field<bool>("_isCursorLock");
                    lockField.Value = !value;

                    Time.timeScale = value ? 0 : 1;

                    if (value)
                    {
                        ShowOnlyImplemented = true;

                        _screenRect = new Rect(0, 0, Screen.width, Screen.height);
                        const int windowSize = 540;
                        _windowRect = new Rect((_screenRect.width - windowSize) / 2,
                            (_screenRect.height - windowSize) / 2, windowSize, windowSize);

                        if (_crestableHeroines != null)
                        {
                            foreach (var heroine in _crestableHeroines)
                                heroine.Destroy();
                        }

                        _crestableHeroines = Singleton<Game>.Instance.HeroineList
                            .Where(x => x != null && x.isStaff) // staff is club member
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

        public static void ClubInterfaceOnGui(Unit _)
        {
            if (!ShowWindow) return;
            if (GUI.Button(_screenRect, string.Empty, GUI.skin.box) && !_windowRect.Contains(Input.mousePosition)) ShowWindow = false;
            IMGUIUtils.DrawSolidBox(_windowRect);
            GUILayout.Window(47238, _windowRect, CrestWindow, "Assign lewd crests to club members");
            Input.ResetInputAxes();
        }

        private static void CrestWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
                {
                    GUILayout.Label("Your club's love research includes giving other club members lewd crests. Crests can have many different effects, but members don't know about them to avoid bias.\nThe effects are reverted after a crest is removed, but memories remain.");
                }
                GUILayout.EndHorizontal();

                if (_crestableHeroines.Count == 0)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
                    {
                        var alig = GUI.skin.label.alignment;
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUILayout.Label("Only present club memebers can be given a crest.\n\nInvite some new members and try again!");
                        GUI.skin.label.alignment = alig;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    {
                        _scrollPos1 = GUILayout.BeginScrollView(_scrollPos1, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        {
                            //_selHeroine = GUILayout.SelectionGrid(_selHeroine, _crestableHeroines.Select(x => x.HeroineNameAndCrest).ToArray(), 1, GUILayout.ExpandWidth(true));
                            _selHeroine = GUILayout.SelectionGrid(_selHeroine, _crestableHeroines.Select(x => x.GetFaceTex()).ToArray(), 5, GUILayout.ExpandWidth(true));
                            if (GUI.changed)
                            {
                                _selCrest = _crestlist.GetIndex(_crestableHeroines[_selHeroine].Controller.CurrentCrest);
                                GUI.changed = false;
                            }
                        }
                        GUILayout.EndScrollView();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(_crestableHeroines[_selHeroine].HeroineName, GUILayout.ExpandWidth(false));
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginHorizontal(GUILayout.Height(165), GUILayout.ExpandWidth(true));
                    {
                        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(150), GUILayout.ExpandHeight(true));
                        _scrollPos2 = GUILayout.BeginScrollView(_scrollPos2, false, true, GUILayout.ExpandWidth(true));
                        {
                            _selCrest = GUILayout.SelectionGrid(_selCrest, _crestlist.GetInterfaceNames(), 1, GUILayout.ExpandWidth(true));
                            if (GUI.changed)
                            {
                                _crestableHeroines[_selHeroine].Controller.CurrentCrest = _crestlist.GetType(_selCrest);
                                GUI.changed = false;
                            }
                        }
                        GUILayout.EndScrollView();
                        ShowOnlyImplemented = GUILayout.Toggle(ShowOnlyImplemented, "Only with effects");
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
                        {
                            var currentCrest = _crestlist.GetInfo(_selCrest);
                            GUILayout.Label("Currently selected crest: " + (currentCrest?.Name ?? "None"));
                            GUILayout.Label(currentCrest != null ? "Description: " + currentCrest.Description : "No crest selected. Choose a crest on the left see its description and give it to the selected character.");
                            GUILayout.FlexibleSpace();
                            if (currentCrest != null)
                                GUILayout.Label(currentCrest.Implemented ? "Gameplay will be changed roughly as described." : "Only for looks and lore, it won't change gameplay.");
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Close this window", GUILayout.ExpandWidth(true)))
                    ShowWindow = false;
            }
            GUILayout.EndVertical();
        }
    }
}