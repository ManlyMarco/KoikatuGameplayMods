using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using ActionGame.Chara;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Illusion.Game;
using KKAPI;
using KKAPI.Utilities;
using Manager;
using StrayTech;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KK_WarpToCharacters
{
    // Inspired by KK_MoveMapFromCharaList
    [BepInProcess(GameProcessName)]
    [BepInProcess(GameProcessNameSteam)]
    [BepInPlugin(GUID, "WarpToCharacters", Version)]
    [BepInDependency(KoikatuAPI.GUID, "1.10")]
    public sealed class WarpToCharactersPlugin : BaseUnityPlugin
    {
        public const string GUID = "WarpToCharacters";
        public const string Version = "1.0";

        private const string GameProcessName = "Koikatu";
        private const string GameProcessNameSteam = "Koikatsu Party";

        private const string ButtonName = "Button_MoveMap";

        private static new ManualLogSource Logger;

        private static Sprite _spriteMoveIcon;
        private static ActionScene _actionScene;

        public void Start()
        {
            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(WarpToCharactersPlugin));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaStatusScene), "Start")]
        private static void CreateButtonsPre(ChaStatusComponent ___cmpFix, ChaStatusComponent ___cmpTeacher, ChaStatusComponent ___cmpFemale)
        {
            try
            {
                if (_spriteMoveIcon == null)
                {
                    var texture2D = ResourceUtils.GetEmbeddedResource("button.png").LoadTexture(TextureFormat.DXT5);
                    _spriteMoveIcon = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height),
                        new Vector2(0.5f, 0.5f), 100f, 0U, SpriteMeshType.FullRect);
                    DontDestroyOnLoad(_spriteMoveIcon);
                }

                if (_actionScene == null) _actionScene = FindObjectOfType<ActionScene>();

                foreach (var chaStatusComponent in new[] { ___cmpFix, ___cmpFemale, ___cmpTeacher })
                {
                    var cardTr = chaStatusComponent.cmpStudentCard.transform;
                    // Check if the button isn't already spawned
                    if (cardTr.Find(ButtonName)) continue;

                    // Always do this to prevent old versions from creating its own buttons if installed
                    var go = new GameObject(ButtonName);
                    go.transform.SetParent(cardTr, false);

                    var image = go.AddComponent<Image>();
                    image.sprite = _spriteMoveIcon;

                    var button = go.AddComponent<Button>();
                    button.targetGraphic = image;
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.97f, 0.73f);
                    rt.offsetMin = new Vector2(-40, -40);
                    rt.offsetMax = Vector2.zero;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaStatusScene), "Start")]
        private static void CreateButtons(ChaStatusScene __instance, ChaStatusComponent ___cmpMale)
        {
            try
            {
                foreach (var chaStatusComponent in __instance.gameObject.GetComponentsInChildren<ChaStatusComponent>())
                {
                    if (chaStatusComponent == ___cmpMale) continue;

                    var cardTr = chaStatusComponent.cmpStudentCard.transform;
                    var go = cardTr.Find(ButtonName)?.gameObject;
                    if (go == null) continue;

                    // Do not show the button if character is on the same map as player
                    var npc = GetCharaNpc(chaStatusComponent);
                    if (npc == null || npc.mapNo == _actionScene.Player.mapNo)
                    {
                        go.SetActive(false);
                        continue;
                    }

                    var button = go.GetComponent<Button>();
                    if (button != null) button.onClick.AddListener(() => WarpToNpc(npc));
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static Base GetCharaNpc(ChaStatusComponent statusComponent)
        {
            var heroine = statusComponent.heroine;
            // Staff or event character
            if (heroine == null)
                return _actionScene.fixChara;

            var heroineNpc = _actionScene.npcList.FirstOrDefault(npc => heroine == npc.heroine);
            if (heroineNpc != null)
                return heroineNpc;

            Logger.LogError("Could not find Npc belonging to " + heroine.parameter.fullname);
            return null;
        }

        private static void WarpToNpc(Base npc)
        {
            if (npc.mapNo == _actionScene.Player.mapNo) return;

            Utils.Sound.Play(SystemSE.ok_s);
            _actionScene.Map.PlayerMapWarp(npc.mapNo, () =>
            {
                var player = _actionScene.Player;

                // Bring the following character with us
                var chaser = player.chaser;
                if (chaser != null && chaser.mapNo == player.mapNo)
                    chaser.mapNo = npc.mapNo;

                player.StartCoroutine(MovePlayerToNpc());
            });
            FindObjectOfType<ChaStatusScene>().Unload();

            IEnumerator MovePlayerToNpc()
            {
                // Need to wait until map changes or our position gets overwritten
                var currentScene = Scene.ActiveScene;
                yield return new WaitUntil(() =>
                {
                    var activeScene = Scene.ActiveScene;
                    return currentScene != activeScene && activeScene.name != "Action";
                });

                // Prevent getting instagibbed
                var activeSceneName = Scene.ActiveScene.name;
                if (activeSceneName == "LockerRoom" || activeSceneName == "ShawerRoom" || activeSceneName.EndsWith("Toilet")) yield break;

                // Find a valid place to move player to in a circle around the target npc
                var onUnitSphere = Random.onUnitSphere;
                onUnitSphere.y = 0;
                onUnitSphere.Normalize();
                if (NavMesh.SamplePosition(npc.position + onUnitSphere, out var hit, 2f, NavMesh.AllAreas))
                {
                    var player = _actionScene.Player;
                    player.position = hit.position;
                    player.transform.LookAtXZ(npc.position);
                }
            }
        }
    }
}