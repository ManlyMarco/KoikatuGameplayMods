using System.Collections;
using System.Collections.Generic;
using ActionGame;
using Harmony;
using KK_Pregnancy.Properties;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_Pregnancy
{
    public static partial class PregnancyGui
    {
        private static class HeartIcons
        {
            private static Sprite _pregSprite;

            internal static void Init(HarmonyInstance hi)
            {
                _pregSprite = LoadPregnancyIcon();

                SceneManager.sceneLoaded += SceneManager_sceneLoaded;

                hi.PatchAll(typeof(HeartIcons));
            }

            private static Sprite LoadPregnancyIcon()
            {
                var pregIconTex = new Texture2D(2, 2, TextureFormat.DXT5, false);
                pregIconTex.LoadImage(Resource.preg_icon);
                var pregSprite = Sprite.Create(pregIconTex, new Rect(0f, 0f, pregIconTex.width, pregIconTex.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
                Resource.ResourceManager.ReleaseAllResources();
                return pregSprite;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ClassRoomList), "PreviewUpdate")]
            public static void ClassroomPregIconUpdate(ClassRoomList __instance)
            {
                _pluginInstance.StartCoroutine(ClassroomPregInconUpdateCo(__instance));
            }

            private static IEnumerator ClassroomPregInconUpdateCo(ClassRoomList __instance)
            {
                yield return new WaitForEndOfFrame();

                var entries = Traverse.Create(__instance).Property("charaPreviewList").GetValue<List<PreviewClassData>>();

                foreach (var chaEntry in entries)
                {
                    var baseImg = Traverse.Create(chaEntry).Field("_objHeart").GetValue<GameObject>();
                    SetHeart(baseImg, chaEntry.data?.charFile != null && chaEntry.data.charFile.IsChaFilePregnant(), -70f);
                }
            }

            private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (mode == LoadSceneMode.Single || _pluginInstance == null)
                    return;

                var chaStatusScene = Object.FindObjectOfType<ChaStatusScene>();

                if (chaStatusScene != null)
                    _pluginInstance.StartCoroutine(CreatePregnancyIconCo(chaStatusScene));
            }

            private static IEnumerator CreatePregnancyIconCo(ChaStatusScene chaStatusScene)
            {
                yield return new WaitForEndOfFrame();

                foreach (var chaStatusComponent in chaStatusScene.transform.GetComponentsInChildren<ChaStatusComponent>())
                {
                    if (chaStatusComponent.heroine.IsHeroinePregnant())
                    {
                        var heartObj = chaStatusComponent.objHeart;
                        SetHeart(heartObj, true, -91.1f);
                    }
                }
            }

            private static void SetHeart(GameObject heartObj, bool enabled, float xOffset)
            {
                const string name = "Pregnancy_Icon";
                var owner = heartObj.transform.parent;
                var existing = owner.Find(name);

                if (!enabled)
                {
                    if (existing != null)
                        Object.Destroy(existing);
                }
                else
                {
                    if (existing != null)
                        return;

                    var copy = Object.Instantiate(heartObj, owner);
                    copy.name = name;

                    copy.GetComponent<Image>().sprite = _pregSprite;
                    var rt = copy.GetComponent<RectTransform>();

                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + xOffset, rt.anchoredPosition.y);
                    rt.sizeDelta = new Vector2(48, 48);
                    copy.SetActive(true);
                }
            }
        }
    }
}
