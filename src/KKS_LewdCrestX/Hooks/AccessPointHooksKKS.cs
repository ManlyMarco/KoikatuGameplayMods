using System;
using ActionGame;
using HarmonyLib;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UniRx;
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
                StoreApi.ShopType.Normal, StoreApi.ShopBackground.Yellow, 3, 3, false, 100,
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
                        // Probably don't need to dispose at all since changing map removes all custom points but too lazy to test
                        if (_instance != null) _instance.Dispose();

                        if (_icon == null)
                        {
                            _icon = LewdCrestXPlugin.Bundle.LoadAsset<Texture2D>("action_icon_crest_kks") ??
                                    throw new Exception("asset not found - action_icon_crest_kks");
                            Object.DontDestroyOnLoad(_icon);
                        }

                        // Only immediate so it has to always be manaully spawned
                        _instance = GameAPI.AddActionIcon(
                            mapNo: 23,
                            position: new Vector3(27.1f, 0.0f, -130.6f),
                            icon: _icon,
                            color: new Color(0.72f, 0.32f, 0.72f),
                            popupText: "Cursed Drawing Board",
                            onOpen: () => ClubInterface.ShowWindow = true,
                            onCreated: evt => evt.OnGUIAsObservable().Subscribe(ClubInterface.OnGui),
                            delayed: false,
                            immediate: true);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(e);
                    }
                }
            }
        }

        private static Texture2D _icon;
        private static IDisposable _instance;
    }
}