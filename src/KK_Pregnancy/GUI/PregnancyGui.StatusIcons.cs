using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using HarmonyLib;
using Illusion.Extensions;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_Pregnancy
{
    public static partial class PregnancyGui
    {
        private class StatusIcons : MonoBehaviour
        {
            private static Sprite _pregSprite;
            private static Sprite _riskySprite;
            private static Sprite _safeSprite;
            private static Sprite _unknownSprite;
            private static Sprite _leaveSprite;

            private const string ICON_NAME = "Pregnancy_Icon";

            private static readonly List<KeyValuePair<SaveData.CharaData, RectTransform>> _currentHeroine = new List<KeyValuePair<SaveData.CharaData, RectTransform>>();

            internal static void Init(Harmony hi, Sprite unknownSprite, Sprite pregSprite, Sprite safeSprite, Sprite riskySprite, Sprite leaveSprite)
            {
                _unknownSprite = unknownSprite ? unknownSprite : throw new ArgumentNullException(nameof(unknownSprite));
                _pregSprite = pregSprite ? pregSprite : throw new ArgumentNullException(nameof(pregSprite));
                _riskySprite = riskySprite ? riskySprite : throw new ArgumentNullException(nameof(riskySprite));
                _safeSprite = safeSprite ? safeSprite : throw new ArgumentNullException(nameof(safeSprite));
                _leaveSprite = leaveSprite ? leaveSprite : throw new ArgumentNullException(nameof(leaveSprite));

                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                SceneManager.sceneUnloaded += s =>
                {
                    if (_currentHeroine.Count > 0)
                        SceneManager_sceneLoaded(s, LoadSceneMode.Additive);
                };

                hi.PatchAll(typeof(StatusIcons));
            }

            /// <summary>
            ///     Handle class roster
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ClassRoomList), "PreviewUpdate")]
            public static void ClassroomPreviewUpdateHook(ClassRoomList __instance)
            {
                IEnumerator ClassroomPreviewUpdateCo()
                {
                    yield return new WaitForEndOfFrame();

                    _currentHeroine.Clear();
                    SpawnGUI();

                    var entries = __instance.charaPreviewList;

                    foreach (var chaEntry in entries)
                    {
                        // Need to call this every time in case characters get transferred/edited
                        SetHeart(chaEntry.rootObj, chaEntry.data, true);
                    }
                }

                _pluginInstance.StartCoroutine(ClassroomPreviewUpdateCo());
            }

            /// <summary>
            ///     Handle character list in roaming mode
            /// </summary>
            private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (mode == LoadSceneMode.Single || _pluginInstance == null)
                    return;

                _currentHeroine.Clear();

                var chaStatusScene = FindObjectOfType<ChaStatusScene>();
                if (chaStatusScene != null)
                {
                    SpawnGUI();

                    IEnumerator CreatePregnancyIconCo()
                    {
                        yield return new WaitForEndOfFrame();

                        foreach (var chaStatusComponent in chaStatusScene.transform.GetComponentsInChildren<ChaStatusComponent>())
                        {
                            var chara = (SaveData.CharaData)chaStatusComponent.heroine ??
                                        (chaStatusComponent.name == "status_m" ? Manager.Game.Instance.Player : null);
                            SetHeart(chaStatusComponent.gameObject, chara, false);
                        }
                    }

                    _pluginInstance.StartCoroutine(CreatePregnancyIconCo());
                }
            }

            /// <summary>
            ///     Handle char icon for top left quick status popup during roaming mode
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ParamUI), "SetHeroine", typeof(SaveData.Heroine))]
            private static void ParamUI_SetHeroine(ParamUI __instance, SaveData.Heroine _heroine)
            {
                var objFemaleRoot = __instance.objFemaleRoot;
                if (objFemaleRoot == null) return;

                SpawnGUI();

                IEnumerator HeroineCanvasPreviewUpdateCo()
                {
                    yield return new WaitForEndOfFrame();

                    _currentHeroine.Clear();
                    SetQuickStatusIcon(objFemaleRoot, _heroine, -214f, -26f);
                }

                _pluginInstance.StartCoroutine(HeroineCanvasPreviewUpdateCo());
            }

            private static void SpawnGUI()
            {
                if (!GameObject.Find("PregnancyGUI"))
                    new GameObject("PregnancyGUI").AddComponent<StatusIcons>();
            }

            private void OnGUI()
            {
                if (_currentHeroine.Count == 0) return;

                var pos = new Vector2(Input.mousePosition.x, -(Input.mousePosition.y - Screen.height));
                var heroineRect = _currentHeroine.FirstOrDefault(x =>
                {
                    if (x.Value == null) return false;
                    return GetOccupiedScreenRect(x).Contains(pos);
                });
                var chara = heroineRect.Key;
                if (chara == null) return;

                var pregData = chara.GetPregnancyData();

                var status = chara.GetCharaStatus(pregData);
                var heroine = chara as SaveData.Heroine;

                var windowHeight = status == HeroineStatus.Unknown ? 100 : status == HeroineStatus.Pregnant || status == HeroineStatus.OnLeave ? 180 : 370;
                var screenRect = new Rect((int)pos.x + 30, (int)pos.y - windowHeight / 2, 180, windowHeight);
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.BeginArea(screenRect, GUI.skin.box);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.FlexibleSpace();

                        switch (status)
                        {
                            case HeroineStatus.Unknown:
                                GUILayout.Label("This character didn't tell you their risky day schedule yet.");
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("Become closer to learn it!");
                                break;

                            case HeroineStatus.OnLeave:
                                GUILayout.Label("This character is on a maternal leave and will not appear until it is over.");
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("Consider using a rubber next time!");
                                break;

                            case HeroineStatus.Pregnant:
                                GUILayout.Label($"This character is pregnant (on week {pregData.Week} / 40).");
                                GUILayout.FlexibleSpace();
                                if (pregData.GameplayEnabled)
                                    GUILayout.Label(heroine != null ? "The character's body will slowly change, and at the end they will temporarily leave." : "The character's body will slowly change.");

                                GUILayout.FlexibleSpace();
                                var previousPregcount = Mathf.Max(0, pregData.PregnancyCount - 1);
                                GUILayout.Label($"This character was pregnant {previousPregcount} times before.");
                                break;

                            case HeroineStatus.Safe:
                            case HeroineStatus.Risky:
                                if (heroine == null) break;

                                GUILayout.Label(status == HeroineStatus.Safe
                                    ? "This character is on a safe day, have fun!"
                                    : "This character is on a risky day, be careful!");
                                //GUILayout.Space(5);
                                GUILayout.FlexibleSpace();

                                var day = Singleton<Cycle>.Instance.nowWeek;

                                GUILayout.Label("Forecast for this week:");

                                switch (PregnancyDataUtils.GetMenstruation(heroine))
                                {
                                    case MenstruationSchedule.AlwaysSafe:
                                        GUILayout.Label("It's always safe!");
                                        break;
                                    case MenstruationSchedule.AlwaysRisky:
                                        GUILayout.Label("It's always risky!");
                                        break;
                                    default:
                                        GUILayout.Label($"Today ({day}): {status}");

                                        for (var dayOffset = 1; dayOffset < 7; dayOffset++)
                                        {
                                            var adjustedDay = (Cycle.Week)((int)(day + dayOffset) % Enum.GetValues(typeof(Cycle.Week)).Length);
                                            var adjustedSafe = HFlag.GetMenstruation((byte)((heroine.MenstruationDay + dayOffset) % HFlag.menstruations.Length)) == HFlag.MenstruationType.安全日;
                                            GUILayout.Label($"{adjustedDay}: {(adjustedSafe ? "Safe" : "Risky")}");
                                        }
                                        break;
                                }

                                var pregnancyCount = pregData.IsPregnant ? pregData.PregnancyCount - 1 : pregData.PregnancyCount;
                                if (pregnancyCount > 0)
                                {
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Label($"This character was pregnant {pregnancyCount} times.");
                                }

                                if (pregData.WeeksSinceLastPregnancy > 0)
                                {
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Label($"Last pregnancy was {pregData.WeeksSinceLastPregnancy} weeks ago.");
                                }
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndArea();
            }

            /// <summary>
            ///     Enable/disable pregnancy icon
            /// </summary>
            private static void SetHeart(GameObject rootObj, SaveData.CharaData chara, bool classRoster)
            {
                var pregIconTr = rootObj.transform.Find(ICON_NAME);

                if (chara == null)
                {
                    if (pregIconTr != null)
                        Destroy(pregIconTr.gameObject);
                }
                else
                {
                    if (pregIconTr == null)
                    {
                        pregIconTr = new GameObject(ICON_NAME, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                        pregIconTr.SetParent(rootObj.transform, false);
                        var rt = pregIconTr.GetComponent<RectTransform>();
                        if (classRoster)
                        {
                            rt.anchorMax = rt.anchorMin = rt.pivot = new Vector2(0, 1);
                            rt.offsetMin = Vector2.zero;
                            rt.offsetMax = new Vector2(48, 48);
                            rt.localScale = Vector3.one;
                            rt.localPosition = new Vector3(4, -115, 0);
                        }
                        else // status screen during roaming mode
                        {
                            rt.anchorMax = rt.anchorMin = rt.pivot = new Vector2(0.5f, 0.5f);
                            rt.offsetMin = Vector2.zero;
                            rt.offsetMax = new Vector2(48, 48);
                            rt.localScale = Vector3.one;
                            rt.localPosition = new Vector3(-273, -85, 0);
                        }
                    }

                    AddPregIcon(pregIconTr, chara);
                }
            }

            /// <summary>
            ///     Enable/disable pregnancy icon on quick status container
            /// </summary>
            /// <param name="characterImageObj">The heroine image object</param>
            /// <param name="heroine">Is the preg icon shown</param>
            /// <param name="xOffset">Offset from the character image</param>
            /// <param name="yOffset">Offset from the character image</param>
            private static void SetQuickStatusIcon(GameObject characterImageObj, SaveData.Heroine heroine, float xOffset, float yOffset)
            {
                var existing = characterImageObj.transform.Find(ICON_NAME);

                if (heroine == null)
                {
                    if (existing != null)
                        Destroy(existing.gameObject);
                }
                else
                {
                    if (existing == null)
                    {
                        var newChildIcon = new GameObject();
                        newChildIcon.AddComponent<RectTransform>();
                        newChildIcon.AddComponent<Image>();

                        var copy = Instantiate(newChildIcon, characterImageObj.transform);
                        copy.name = ICON_NAME;
                        copy.SetActive(true);

                        var charRt = characterImageObj.GetComponent<RectTransform>();
                        var rt = copy.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(charRt.anchoredPosition.x + xOffset, charRt.anchoredPosition.y + yOffset);
                        rt.sizeDelta = new Vector2(48, 48);

                        existing = copy.transform;
                    }

                    AddPregIcon(existing, heroine);
                }
            }

            private static readonly Vector3[] _worldCornersBuffer = new Vector3[4];

            private static void AddPregIcon(Transform pregIconTransform, SaveData.CharaData chara)
            {
                var image = pregIconTransform.GetComponent<Image>();

                _currentHeroine.Add(new KeyValuePair<SaveData.CharaData, RectTransform>(chara, image.GetComponent<RectTransform>()));

                var status = chara.GetCharaStatus(chara.GetPregnancyData());
                switch (status)
                {
                    case HeroineStatus.Unknown:
                        image.sprite = _unknownSprite;
                        break;
                    case HeroineStatus.OnLeave:
                        image.sprite = _leaveSprite;
                        break;
                    case HeroineStatus.Safe:
                        image.sprite = _safeSprite;
                        break;
                    case HeroineStatus.Risky:
                        image.sprite = _riskySprite;
                        break;
                    case HeroineStatus.Pregnant:
                        image.sprite = _pregSprite;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                pregIconTransform.gameObject.SetActiveIfDifferent(chara is SaveData.Heroine || status == HeroineStatus.Pregnant);
            }

            private static Rect GetOccupiedScreenRect(KeyValuePair<SaveData.CharaData, RectTransform> x)
            {
                x.Value.GetWorldCorners(_worldCornersBuffer);
                var screenPos = new Rect(
                    _worldCornersBuffer[0].x,
                    Screen.height - _worldCornersBuffer[2].y,
                    _worldCornersBuffer[2].x - _worldCornersBuffer[0].x,
                    _worldCornersBuffer[2].y - _worldCornersBuffer[0].y);
                return screenPos;
            }
        }
    }
}