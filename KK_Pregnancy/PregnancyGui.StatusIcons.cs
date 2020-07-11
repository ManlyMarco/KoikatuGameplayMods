using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using BepInEx.Harmony;
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
            internal static Sprite _unknownSprite;

            private static readonly List<KeyValuePair<SaveData.Heroine, Rect>> _currentHeroine = new List<KeyValuePair<SaveData.Heroine, Rect>>();

            internal static void Init(Harmony hi)
            {
                _pregSprite = LoadIcon("pregnant.png");
                _riskySprite = LoadIcon("risky.png");
                _safeSprite = LoadIcon("safe.png");
                _unknownSprite = LoadIcon("unknown.png");

                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                SceneManager.sceneUnloaded += s =>
                {
                    if (_currentHeroine.Count > 0)
                        SceneManager_sceneLoaded(s, LoadSceneMode.Additive);
                };

                hi.PatchAll(typeof(StatusIcons));
            }

            private static Sprite LoadIcon(string resourceFileName)
            {
                var iconTex = new Texture2D(2, 2, TextureFormat.DXT5, false);
                DontDestroyOnLoad(iconTex);
                iconTex.LoadImage(ResourceUtils.GetEmbeddedResource(resourceFileName));
                var sprite = Sprite.Create(iconTex, new Rect(0f, 0f, iconTex.width, iconTex.height),
                    new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
                DontDestroyOnLoad(sprite);
                return sprite;
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

            private static void SpawnGUI()
            {
                if (!GameObject.Find("PregnancyGUI"))
                    new GameObject("PregnancyGUI").AddComponent<StatusIcons>();
            }

            private void OnGUI()
            {
                if (_currentHeroine.Count == 0) return;

                var pos = new Vector2(Input.mousePosition.x, -(Input.mousePosition.y - Screen.height));
                var heroine = _currentHeroine.FirstOrDefault(x => x.Value.Contains(pos)).Key;
                if (heroine == null) return;

                var status = GetHeroineStatus(heroine);

                var windowHeight = status == HeroineStatus.Unknown || status == HeroineStatus.Pregnant ? 110 : 270;
                var screenRect = new Rect(pos.x + 30, pos.y - windowHeight / 2, 180, windowHeight);
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.BeginArea(screenRect, GUI.skin.box);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.FlexibleSpace();

                        switch (status)
                        {
                            case HeroineStatus.Unknown:
                                GUILayout.Label("This character didn't tell you their risky day schedule yet.\n\nBecome closer to learn it!");
                                break;

                            case HeroineStatus.Pregnant:
                                GUILayout.Label("This character is pregnant.\n\nOver time the character's belly will grow, and at the end they will leave school temporarily.");
                                break;

                            case HeroineStatus.Safe:
                            case HeroineStatus.Risky:
                                GUILayout.Label(status == HeroineStatus.Safe
                                    ? "This character is on a safe day, have fun!"
                                    : "This character is on a risky day, be careful!");
                                GUILayout.Space(5);

                                var day = Singleton<Cycle>.Instance.nowWeek;

                                GUILayout.Label("Forecast for this week:");
                                GUILayout.Label($"Today ({day}): {status}");

                                for (var dayOffset = 1; dayOffset < 7; dayOffset++)
                                {
                                    var adjustedDay =
                                        (Cycle.Week)((int)(day + dayOffset) % Enum.GetValues(typeof(Cycle.Week)).Length);
                                    var adjustedSafe =
                                        HFlag.GetMenstruation(
                                            (byte)((heroine.MenstruationDay + dayOffset) % HFlag.menstruations.Length)) ==
                                        HFlag.MenstruationType.安全日;
                                    GUILayout.Label($"{adjustedDay}: {(adjustedSafe ? "Safe" : "Risky")}");
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
                const string name = "Pregnancy_Icon";
                var owner = heartObj.transform.parent;
                var existing = owner.Find(name);

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
                        copy.name = name;
                        copy.SetActive(true);

                        var rt = copy.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + xOffset, rt.anchoredPosition.y);
                        rt.sizeDelta = new Vector2(48, 48);

                        existing = copy.transform;
                    }

                    var image = existing.GetComponent<Image>();

                    var worldCorners = new Vector3[4];
                    image.GetComponent<RectTransform>().GetWorldCorners(worldCorners);
                    _currentHeroine.Add(new KeyValuePair<SaveData.Heroine, Rect>(heroine, new Rect(
                        worldCorners[0].x,
                        Screen.height - worldCorners[2].y,
                        worldCorners[2].x - worldCorners[0].x,
                        worldCorners[2].y - worldCorners[0].y)));

                    switch (GetHeroineStatus(heroine))
                    {
                        case HeroineStatus.Unknown:
                            image.sprite = _unknownSprite;
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
            }

            internal static HeroineStatus GetHeroineStatus(SaveData.Heroine heroine)
            {
                // Check if she wants to tell
                if (heroine.intimacy >= 80 ||
                    heroine.hCount >= 5 ||
                    heroine.parameter.attribute.bitch && heroine.favor > 50 ||
                    (heroine.isGirlfriend || heroine.favor >= 90) && (!heroine.isVirgin || heroine.hCount >= 2 || heroine.intimacy >= 40))
                {
                    if (heroine.IsHeroinePregnant(!PregnancyPlugin.ShowPregnancyIconEarly.Value))
                        return HeroineStatus.Pregnant;

                    return HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.安全日
                        ? HeroineStatus.Safe
                        : HeroineStatus.Risky;
                }

                return HeroineStatus.Unknown;
            }

            internal enum HeroineStatus
            {
                Unknown,
                Safe,
                Risky,
                Pregnant
            }
        }
    }
}