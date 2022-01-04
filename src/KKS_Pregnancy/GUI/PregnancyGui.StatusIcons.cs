using System;
using ActionGame;
using HarmonyLib;
using KKAPI.Utilities;
using SaveData;
using UniRx;
using UniRx.Triggers;
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

            private static CharaData _currentHeroine;

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
            ///     Handle class roster
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Passport), nameof(Passport.Set))]
            private static void ClassroomPreviewUpdateHook(Passport __instance, CharaData charaData)
            {
                try
                {
                    SetHeart(__instance, charaData);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.Start))]
            private static void ClassRoomListStartHook(ClassRoomList __instance)
            {
                _currentHeroine = null;
                __instance.OnGUIAsObservable().Subscribe(_ => OnGUI());
            }

            private static void OnGUI()
            {
                if (_currentHeroine == null) return;

                var chara = _currentHeroine;
                var pregData = chara.GetPregnancyData();

                var status = chara.GetCharaStatus(pregData);
                var heroine = chara as Heroine;

                var windowHeight = status == HeroineStatus.Unknown ? 100 : status == HeroineStatus.Pregnant || status == HeroineStatus.OnLeave ? 180 : 370;
                var pos = new Vector2(Input.mousePosition.x, -(Input.mousePosition.y - Screen.height));
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

                                switch (pregData.MenstruationSchedule)
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
            private static void SetHeart(Passport passport, CharaData chara)
            {
                if (passport == null) throw new ArgumentNullException(nameof(passport));

                // This is needed because Atach has an image that covers our image, blocking pointer events. To fix this, change parent of our icon to the atach object
                var atachTr = passport.transform.parent.Find("Atach");

                var passpParentTr = passport._emptyImage.transform.parent;
                var pregIconTr = (atachTr ?? passpParentTr).Find(ICON_NAME);

                if (pregIconTr != null)
                    Destroy(pregIconTr.gameObject);

                if (chara == null) return;

                var status = chara.GetCharaStatus(chara.GetPregnancyData());
                if (chara is Heroine || status == HeroineStatus.Pregnant)
                {
                    pregIconTr = new GameObject(ICON_NAME, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                    pregIconTr.SetParent(passpParentTr, false);

                    var rt = pregIconTr.GetComponent<RectTransform>();
                    rt.anchorMax = rt.anchorMin = rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = new Vector2(48, 48);
                    rt.localScale = Vector3.one;
                    rt.localPosition = new Vector3(27f, -163f, 0);

                    var image = pregIconTr.GetComponent<Image>();

                    image.OnPointerEnterAsObservable().Subscribe(_ => _currentHeroine = chara);
                    image.OnPointerExitAsObservable().Subscribe(_ => _currentHeroine = null);

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

                    if (atachTr != null)
                        pregIconTr.SetParent(atachTr);
                }
            }
        }
    }
}