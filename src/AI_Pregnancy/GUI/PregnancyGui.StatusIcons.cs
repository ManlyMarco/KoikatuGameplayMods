using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIProject;
using AIProject.UI;
using AIProject.SaveData;
using HarmonyLib;
using KKAPI.Utilities;
using UnityEngine;
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

            private static readonly List<KeyValuePair<AgentData, RectTransform>> _currentHeroine = new List<KeyValuePair<AgentData, RectTransform>>();

            internal static void Init(Harmony hi, Sprite unknownSprite, Sprite pregSprite, Sprite safeSprite, Sprite riskySprite, Sprite leaveSprite)
            {
                _unknownSprite = unknownSprite ? unknownSprite : throw new ArgumentNullException(nameof(unknownSprite));
                _pregSprite = pregSprite ? pregSprite : throw new ArgumentNullException(nameof(pregSprite));
                _riskySprite = riskySprite ? riskySprite : throw new ArgumentNullException(nameof(riskySprite));
                _safeSprite = safeSprite ? safeSprite : throw new ArgumentNullException(nameof(safeSprite));
                _leaveSprite = leaveSprite ? leaveSprite : throw new ArgumentNullException(nameof(leaveSprite));

                hi.PatchAll(typeof(StatusIcons));
            }

            /// <summary>
            ///     Handle char icon for status menu
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StatusUI), "RefreshAgentContent", typeof(int))]
            private static void StatusUI_RefreshAgentContent(StatusUI __instance, int id)
            {

                // PregnancyPlugin.Logger.LogDebug("Preg - StatusUI_RefreshAgentContent");
                var objImageRoot = Traverse.Create(__instance).Field("_cardRawImage").GetValue<RawImage>();
                if (objImageRoot == null) return;

                //Ignore player status tab, only want actors    
                if (id == 0)
                {
                    var existingIcon = objImageRoot.transform.Find(ICON_NAME);
                    if (existingIcon) Destroy(existingIcon.gameObject);
                    return;
                }

                Singleton<Manager.Map>.Instance.AgentTable.TryGetValue((id - 1), out AgentActor _heroine);
                if (_heroine == null) return;

                SpawnGUI();

                IEnumerator HeroineCanvasPreviewUpdateCo()
                {
                    yield return new WaitForEndOfFrame();

                    _currentHeroine.Clear();
                    //                                                             :right :up
                    SetQuickStatusIcon(objImageRoot.gameObject, _heroine.AgentData, 95f, -80f);
                }

                _pluginInstance.StartCoroutine(HeroineCanvasPreviewUpdateCo());
            }

            /// <summary>
            ///     Clear icon if on player status menu
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StatusUI), "RefreshPlayerContent")]
            private static void StatusUI_RefreshAgentContent(StatusUI __instance)
            {
                var objImageRoot = Traverse.Create(__instance).Field("_cardRawImage").GetValue<RawImage>();
                if (objImageRoot == null) return;

                var existingIcon = objImageRoot.transform.Find(ICON_NAME);
                if (existingIcon) Destroy(existingIcon.gameObject);
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

                var status = chara.GetHeroineStatus(pregData);
                var heroine = chara as AgentData;

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
            ///     Enable/disable pregnancy icon on quick status container
            /// </summary>
            /// <param name="characterImageObj">The heroine image object</param>
            /// <param name="heroine">Is the preg icon shown</param>
            /// <param name="xOffset">Offset from the character image</param>
            /// <param name="yOffset">Offset from the character image</param>
            private static void SetQuickStatusIcon(GameObject characterImageObj, AgentData heroine, float xOffset, float yOffset)
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

            private static void AddPregIcon(Transform pregIconTransform, AgentData heroine)
            {
                var image = pregIconTransform.GetComponent<Image>();

                _currentHeroine.Add(new KeyValuePair<AgentData, RectTransform>(heroine, image.GetComponent<RectTransform>()));

                var status = heroine.GetHeroineStatus(heroine.GetPregnancyData());
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
            }

            private static Rect GetOccupiedScreenRect(KeyValuePair<AgentData, RectTransform> x)
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