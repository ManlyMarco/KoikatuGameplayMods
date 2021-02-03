using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using Illusion.Component;
using KKAPI.Utilities;
using Manager;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KK_LewdCrestX
{
    internal static class ActionIconHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionMap), "Reserve")]
        private static void OnMapChangedHook(ActionMap __instance)
        {
            if (__instance.mapRoot == null || __instance.isMapLoading) return;

            if (__instance.no == 22)
            {
                try
                {
                    SpawnCrestActionPoint();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                }
            }
        }

        private static void SpawnCrestActionPoint()
        {
            LewdCrestXPlugin.Logger.LogDebug("Spawning crest action point");

            if (_iconOff == null)
            {
                _iconOff = (LewdCrestXPlugin.Bundle.LoadAsset<Texture2D>("action_icon_crest_off") ??
                            throw new Exception("asset not found - action_icon_crest_off")).ToSprite();
                Object.DontDestroyOnLoad(_iconOff);
            }

            if (_iconOn == null)
            {
                _iconOn = (LewdCrestXPlugin.Bundle.LoadAsset<Texture2D>("action_icon_crest_on") ??
                           throw new Exception("asset not found - action_icon_crest_on")).ToSprite();
                Object.DontDestroyOnLoad(_iconOn);
            }

            var inst = CommonLib.LoadAsset<GameObject>("map/playeractionpoint/00.unity3d", "PlayerActionPoint_05", true);
            var parent = GameObject.Find("Map/ActionPoints");
            inst.transform.SetParent(parent.transform, true);

            var pap = inst.GetComponentInChildren<PlayerActionPoint>();
            var iconRootObject = pap.gameObject;
            var iconRootTransform = pap.transform;
            Object.DestroyImmediate(pap, false);

            // position above the small table
            iconRootTransform.position = new Vector3(-3.1f, -0.4f, 1.85f);

            if (iconRootObject.GetComponent<ObservableUpdateTrigger>())
                Console.WriteLine("was spawned -=--------------");

            var evt = iconRootObject.AddComponent<TriggerEnterExitEvent>();
            var animator = iconRootObject.GetComponentInChildren<Animator>();
            var rendererIcon = iconRootObject.GetComponentInChildren<SpriteRenderer>();
            rendererIcon.sprite = _iconOff;
            var playerInRange = false;
            evt.onTriggerEnter += _c =>
            {
                if (!_c.CompareTag("Player")) return;
                playerInRange = true;
                animator.Play("icon_action");
                rendererIcon.sprite = _iconOn;
                _c.GetComponent<Player>().actionPointList.Add(evt);
            };
            evt.onTriggerExit += _c =>
            {
                if (!_c.CompareTag("Player")) return;
                playerInRange = false;
                animator.Play("icon_stop");
                rendererIcon.sprite = _iconOff;
                _c.GetComponent<Player>().actionPointList.Remove(evt);
            };

            var player = Singleton<Game>.Instance.actScene.Player;
            evt.UpdateAsObservable()
                .Where(_ => playerInRange && ActionInput.isAction && !player.isActionNow)
                .Subscribe(_ => ShowWindow = true)
                .AddTo(evt);

            evt.OnGUIAsObservable()
                .Where(_ => ShowWindow)
                .Subscribe(_ =>
                {
                    if (GUI.Button(_screenRect, string.Empty, GUI.skin.box) && !_windowRect.Contains(Input.mousePosition))
                        ShowWindow = false;
                    IMGUIUtils.DrawSolidBox(_windowRect);
                    GUILayout.Window(47238, _windowRect, CrestWindow, "Assign lewd crests to club members");
                    Input.ResetInputAxes();
                })
                .AddTo(evt);
        }

        private static Sprite _iconOff, _iconOn;
        private static List<SaveData.Heroine> _crestableHeroines;
        private static Rect _screenRect;
        private static Rect _windowRect;
        private static Vector2 _scrollPos1, _scrollPos2;
        private static int _selHeroine, _selCrest;
        private static bool _showWindow;
        private static bool _showOnlyImplemented;
        private static CrestInterfaceList _crestlist;

        private static bool ShowWindow
        {
            get => _showWindow;
            set
            {
                if (_showWindow != value)
                {
                    var actScene = Singleton<Game>.Instance.actScene;
                    var lockField = Traverse.Create(actScene).Field<bool>("_isCursorLock");
                    lockField.Value = !value;

                    if (value)
                    {
                        ShowOnlyImplemented = true;

                        _screenRect = new Rect(0, 0, Screen.width, Screen.height);
                        const int windowSize = 450;
                        _windowRect = new Rect((_screenRect.width - windowSize) / 2,
                            (_screenRect.height - windowSize) / 2, windowSize, windowSize);

                        _crestableHeroines = Singleton<Game>.Instance.HeroineList.Where(x => x != null && x.isStaff)
                            .ToList();

                        _selCrest = _selHeroine = 0;
                        _scrollPos1 = _scrollPos2 = Vector2.zero;
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
                    _crestlist = CrestInterfaceList.Create(value, true);
                    _showOnlyImplemented = value;
                }
            }
        }

        private static void CrestWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
                {
                    GUILayout.Label("As a part of the club's research activities, you can give other club members lewd (womb) crests.\nCrests can have mental and/or physical effects, but members don't know about this to avoid bias in the research.\nAll effects are reverted after a crest is removed, but memories remain.");
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                //todo handle 0 heroines
                _scrollPos1 = GUILayout.BeginScrollView(_scrollPos1, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                {
                    _selHeroine = GUILayout.SelectionGrid(_selHeroine, _crestableHeroines.Select(x => x.charFile.parameter.fullname).ToArray(), 1, GUILayout.ExpandWidth(true)); //todo TL name
                    if (GUI.changed)
                    {
                        _selCrest = _crestlist.GetIndex(_crestableHeroines[_selHeroine].GetCrestController().CurrentCrest);
                        GUI.changed = false;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUILayout.BeginHorizontal(GUILayout.Height(150), GUILayout.ExpandWidth(true));
                {
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(150), GUILayout.ExpandHeight(true));
                    _scrollPos2 = GUILayout.BeginScrollView(_scrollPos2, false, true, GUILayout.ExpandWidth(true));
                    {
                        _selCrest = GUILayout.SelectionGrid(_selCrest, _crestlist.GetInterfaceNames(), 1, GUILayout.ExpandWidth(true));
                        if (GUI.changed)
                        {
                            _crestableHeroines[_selHeroine].GetCrestController().CurrentCrest = _crestlist.GetType(_selCrest);
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
                        GUILayout.Label(currentCrest != null ? "Description: " + currentCrest.Description : "No crest selected. Choose a crest on the right see its description and give it to the selected character.");
                        GUILayout.FlexibleSpace();
                        if (currentCrest != null)
                            GUILayout.Label(currentCrest.Implemented ? "Gameplay will be changed as described." : "Only for looks and lore, it won't change gameplay.");
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Apply and Close", GUILayout.ExpandWidth(true)))
                    ShowWindow = false;
            }
            GUILayout.EndVertical();
        }
    }
}