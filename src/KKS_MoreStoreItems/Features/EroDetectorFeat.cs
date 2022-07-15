using System;
using ActionGame;
using ActionGame.Chara;
using BepInEx.Configuration;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UniRx;

namespace MoreShopItems.Features
{
    public class EroDetectorFeat : IFeature
    {
        private static ConfigEntry<bool> _notifyMast;
        private static ConfigEntry<bool> _notifyLesb;
        private static string _infoTextPrefixMast = "{0} オナニー：{1}"; //I am sorry if this is incorrect as i used google translate
        private static string _infoTextPrefixLesb = "{0} レズビアン：{1}"; //Same here

        public bool ApplyFeature(ref CompositeDisposable disp, MoreShopItemsPlugin inst)
        {
            const string itemName = "Ero Detection App";

            disp.Add(StoreApi.RegisterShopItem(
                itemId: MoreShopItemsPlugin.DetectorItemId,
                itemName: itemName,
                explaination: "A phone app that notifies you about erotic activities happening around the island. It's fully automatic and gives estimated locations.",
                shopType: StoreApi.ShopType.NightOnly,
                itemBackground: StoreApi.ShopBackground.Yellow,
                itemCategory: 3,
                stock: 1,
                resetsDaily: false,
                cost: 200,
                sort: 500));

            disp.Add(Harmony.CreateAndPatchAll(typeof(EroDetectorFeat)));

            _notifyMast = inst.Config.Bind(itemName, "Notification on masturbation", true, "If the item is purchased, show a notification whenever any NPC starts a masturbation action.");
            _notifyLesb = inst.Config.Bind(itemName, "Notification on lesbian", true, "If the item is purchased, show a notification whenever any NPC starts a lesbian action.");

            TranslationHelper.TranslateAsync(_infoTextPrefixMast, s => _infoTextPrefixMast = s);
            TranslationHelper.TranslateAsync(_infoTextPrefixLesb, s => _infoTextPrefixLesb = s);

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Base), nameof(Base.wpData), MethodType.Setter)]
        private static void WaitPointDataChangedHook(Base __instance, Base.WaitPointData value)
        {
            // wpData is null when the character is still walking to the current action location
            if (value == null) return;

            if (__instance is NPC npc)
            {
                if (npc.isOnanism && _notifyMast.Value || npc.isLesbian && _notifyLesb.Value)
                {
                    try
                    {
                        if (StoreApi.GetItemAmountBought(MoreShopItemsPlugin.DetectorItemId) > 0)
                        {
                            var mapNo = __instance.mapNo;
                            //if (ActionScene.initialized && ActionScene.instance.Player.mapNo != mapNo)
                            if (ActionScene.instance.Map.infoDic.TryGetValue(mapNo, out var param))
                            {
                                if (npc.isOnanism)
                                {
                                    InformationUI.SetAsync(string.Format(_infoTextPrefixMast, npc.charaData.Name, param.DisplayName), InformationUI.Mode.Normal).Forget();
                                }
                                else if (npc.isLesbian)
                                {
                                    InformationUI.SetAsync(string.Format(_infoTextPrefixLesb, npc.charaData.Name, param.DisplayName), InformationUI.Mode.Normal).Forget();
                                }
                                
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }
        }
    }
}
