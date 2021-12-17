using System;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using Illusion.Component;
using Illusion.Game;
using KKAPI.MainGame;
using KKAPI.Utilities;
using Manager;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KK_LewdCrestX
{
    internal static class AccessPointHooks
    {
        private const int StoreItemId = 3456690;

        public static void Apply(Harmony hi)
        {
            hi.PatchAll(typeof(AccessPointHooks));

            StoreApi.RegisterShopItem(StoreItemId, "Cursed drawing board",
                "Gives access to a supposedly cursed drawing board inside the Training Center. The board is said to give its user the ability to bestow lewd crests upon people they know. Each upgrade lets you affect characters that you know less well.",
                StoreApi.ShopType.NightOnly, StoreApi.ShopBackground.Yellow, 3, 3, false, 100,
                numText: "{0} available upgrades");
        }

        public static int GetFeatureLevel()
        {
            return StoreApi.GetItemAmountBought(StoreItemId);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionMap), nameof(ActionMap.Reserve))]
        private static void OnMapChangedHook(ActionMap __instance)
        {
            if (__instance.mapRoot == null || __instance.isMapLoading) return;

            if (__instance.no == 23) // training center break room
            {
                if (GetFeatureLevel() > 0)
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
        }

        private static void SpawnCrestActionPoint()
        {
            if (_iconRootObject) return;

            LewdCrestXPlugin.Logger.LogDebug("Spawning crest action point");

            if (_icon == null)
            {
                _icon = LewdCrestXPlugin.Bundle.LoadAsset<Texture2D>("action_icon_crest_kks") ?? //todo add to bundle
                        throw new Exception("asset not found - action_icon_crest_kks");
                Object.DontDestroyOnLoad(_icon);
            }

            var inst = CommonLib.LoadAsset<GameObject>("map/playeractionpoint/00.unity3d", "PlayerActionPoint_05", true);
            var parent = GameObject.Find("Map/ActionPoints");
            inst.transform.SetParent(parent.transform, true);
            inst.name = "PlayerActionPoint_LewdCrestX";

            var pap = inst.GetComponentInChildren<PlayerActionPoint>();
            _iconRootObject = pap.gameObject;
            pap.gameObject.name = inst.name;
            var iconRootTransform = pap.transform;

            var rendererIcon = pap.renderers.Reverse().First(x =>
            {
                var tex = x.material.mainTexture;
                return tex.width == 256 && tex.height == 256;
            });
            var animator = pap.animator;

            pap.gameObject.layer = LayerMask.NameToLayer("Action/ActionPoint");

            foreach (Transform child in pap.transform.parent)
            {
                if (child != pap.transform)
                    Object.Destroy(child.gameObject);
            }
            Object.DestroyImmediate(pap, false);

            // position above the small table
            iconRootTransform.position = new Vector3(27.1f, 0.0f, -130.6f);

            // Set color to pink
            var pointColor = new Color(0.72f, 0.32f, 0.72f);
            foreach (var rend in iconRootTransform.GetComponentsInChildren<MeshRenderer>())
                rend.material.color = pointColor;
#pragma warning disable 618
            foreach (var rend in iconRootTransform.GetComponentsInChildren<ParticleSystem>())
                rend.startColor = pointColor;
#pragma warning restore 618

            // Hook up event/anim logic
            var evt = _iconRootObject.AddComponent<TriggerEnterExitEvent>();
            rendererIcon.material.mainTexture = _icon;
            var playerInRange = false;
            evt.onTriggerEnter += c =>
            {
                if (!c.CompareTag("Player")) return;
                playerInRange = true;
                animator.Play(PAP.Assist.Animation.SpinState);
                Utils.Sound.Play(Manager.Sound.Type.GameSE2D, Utils.Sound.SEClipTable[0x38], 0f);
                c.GetComponent<Player>().actionPointList.Add(evt);
                ActionScene.instance.actionChangeUI.Set(ActionChangeUI.ActionType.Shop);
                ActionScene.instance.actionChangeUI._text.text = "Cursed Drawing Board";
            };
            evt.onTriggerExit += c =>
            {
                if (!c.CompareTag("Player")) return;
                playerInRange = false;
                animator.Play(PAP.Assist.Animation.IdleState);
                c.GetComponent<Player>().actionPointList.Remove(evt);
                ActionScene.instance.actionChangeUI.Remove(ActionChangeUI.ActionType.Shop);
            };

            var player = LewdCrestXGameController.GetActionScene().Player;
            evt.UpdateAsObservable()
                .Subscribe(_ =>
                {
                    // Hide in H scenes and other places
#if KK
                    var isVisible = Game.IsInstance() && !Game.instance.IsRegulate(true);
#else
                    var isVisible = !Game.IsRegulate(true);
#endif
                    if (rendererIcon.enabled != isVisible)
                        rendererIcon.enabled = isVisible;

                    // Check if player clicked this point
                    if (isVisible && playerInRange && ActionInput.isAction && !player.isActionNow)
                    {
                        ClubInterface.ShowWindow = true;
                    }
                })
                .AddTo(evt);
            evt.OnGUIAsObservable()
                .Subscribe(ClubInterface.OnGui)
                .AddTo(evt);
        }

        private static Texture2D _icon;
        private static GameObject _iconRootObject;
    }
}