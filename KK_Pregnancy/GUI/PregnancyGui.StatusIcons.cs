using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using HarmonyLib;
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

            private static readonly List<KeyValuePair<SaveData.Heroine, RectTransform>> _currentHeroine = new List<KeyValuePair<SaveData.Heroine, RectTransform>>();

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

                    var entries = Traverse.Create(__instance).Property("charaPreviewList")
                        .GetValue<List<PreviewClassData>>();

                    foreach (var chaEntry in entries)
                    {
                        var baseImg = Traverse.Create(chaEntry).Field("_objHeart").GetValue<GameObject>();
                        // Need to call this every time in case characters get transferred/edited
                        SetHeart(baseImg, chaEntry.data?.charFile?.GetHeroine(), -70f);
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

                        foreach (var chaStatusComponent in chaStatusScene.transform
                            .GetComponentsInChildren<ChaStatusComponent>())
                        {
                            var heartObj = chaStatusComponent.objHeart;
                            if (heartObj != null) // not present on mc and teacher 
                                SetHeart(heartObj, chaStatusComponent.heroine, -91.1f);
                        }
                    }

                    _pluginInstance.StartCoroutine(CreatePregnancyIconCo());
                }
            }

            /// <summary>
            ///     Handle char icon for quick status popup
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ParamUI), "SetHeroine", typeof(SaveData.Heroine))]
            private static void ParamUI_SetHeroine(ParamUI __instance, SaveData.Heroine _heroine)
            {
                var objFemaleRoot = Traverse.Create(__instance).Field("objFemaleRoot").GetValue<GameObject>();
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
                var heroine = _currentHeroine.FirstOrDefault(x => GetOccupiedScreenRect(x).Contains(pos)).Key;
                if (heroine == null) return;

                var pregData = heroine.GetPregnancyData();
                var status = heroine.GetHeroineStatus(pregData);

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
                                GUILayout.Label("The character's body will slowly change, and at the end they will temporarily leave.");

                                GUILayout.FlexibleSpace();
                                var previousPregcount = Mathf.Max(0, pregData.PregnancyCount - 1);
                                GUILayout.Label($"This character was pregnant {previousPregcount} times before.");
                                break;

                            case HeroineStatus.Safe:
                            case HeroineStatus.Risky:
                                GUILayout.Label(status == HeroineStatus.Safe
                                    ? "This character is on a safe day, have fun!"
                                    : "This character is on a risky day, be careful!");
                                //GUILayout.Space(5);
                                GUILayout.FlexibleSpace();

                                var day = Singleton<Cycle>.Instance.nowWeek;

                                GUILayout.Label("Forecast for this week:");
                                GUILayout.Label($"Today ({day}): {status}");

                                for (var dayOffset = 1; dayOffset < 7; dayOffset++)
                                {
                                    var adjustedDay = (Cycle.Week)((int)(day + dayOffset) % Enum.GetValues(typeof(Cycle.Week)).Length);
                                    var adjustedSafe = HFlag.GetMenstruation((byte)((heroine.MenstruationDay + dayOffset) % HFlag.menstruations.Length)) == HFlag.MenstruationType.安全日;
                                    GUILayout.Label($"{adjustedDay}: {(adjustedSafe ? "Safe" : "Risky")}");
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
            /// <param name="heartObj">The lovers icon object</param>
            /// <param name="heroine">Is the preg icon shown</param>
            /// <param name="xOffset">Offset from the lovers icon</param>
            private static void SetHeart(GameObject heartObj, SaveData.Heroine heroine, float xOffset)
            {
                var owner = heartObj.transform.parent;
                var existing = owner.Find(ICON_NAME);

                if (heroine == null)
                {
                    if (existing != null)
                        Destroy(existing.gameObject);
                }
                else
                {
                    if (existing == null)
                    {
                        var copy = Instantiate(heartObj, owner);
                        copy.name = ICON_NAME;
                        copy.SetActive(true);

                        var rt = copy.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + xOffset, rt.anchoredPosition.y);
                        rt.sizeDelta = new Vector2(48, 48);

                        existing = copy.transform;
                    }

                    AddPregIcon(existing, heroine);
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

            private static void AddPregIcon(Transform pregIconTransform, SaveData.Heroine heroine)
            {
                var image = pregIconTransform.GetComponent<Image>();

                _currentHeroine.Add(new KeyValuePair<SaveData.Heroine, RectTransform>(heroine, image.GetComponent<RectTransform>()));

                switch (heroine.GetHeroineStatus(heroine.GetPregnancyData()))
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
            }

            private static readonly Vector3[] _worldCornersBuffer = new Vector3[4];
            private static Rect GetOccupiedScreenRect(KeyValuePair<SaveData.Heroine, RectTransform> x)
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