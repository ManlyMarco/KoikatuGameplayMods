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

        private sealed class HeroineData
        {
            public readonly SaveData.Heroine Heroine;
            public readonly LewdCrestXController Controller;
            private string _heroineName;
            private Texture2D _faceTex;

            public HeroineData(SaveData.Heroine heroine)
            {
                Heroine = heroine;
                Controller = heroine.GetCrestController();

                _heroineName = Heroine.parameter.fullname;
                TranslationHelper.TranslateAsync(_heroineName, s => _heroineName = s);
            }

            public string GetCrestName() => LewdCrestXPlugin.CrestInfos.TryGetValue(Controller.CurrentCrest, out var ci) ? ci.Name : "No crest";

            public string HeroineName => _heroineName;

            public Texture GetFaceTex()
            {
                if (_faceTex == null)
                {
                    var origTex = Heroine.charFile.facePngData.LoadTexture();
                    var scale = 84f / origTex.width;
                    _faceTex = origTex.ResizeTexture(TextureUtils.ImageFilterMode.Average, scale);
                    GameObject.Destroy(origTex);
                }

                return _faceTex;

            }

            public void Destroy()
            {
                GameObject.Destroy(_faceTex);
            }
        }

        private static Sprite _iconOff, _iconOn;
        private static List<HeroineData> _crestableHeroines;
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