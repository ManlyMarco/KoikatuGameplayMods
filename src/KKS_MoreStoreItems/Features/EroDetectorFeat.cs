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
        private static string _infoTextPrefixMast = "{0}は{1}でオナニーしている";
        private static string _infoTextPrefixLesb = "{0}は{1}でレズのセックスしている";
        private static string _infoTextPrefix = "エロ活動：{0}";

        public bool ApplyFeature(ref CompositeDisposable disp, MoreShopItemsPlugin inst)
        {
            const string itemName = "Ero Detection App";
            const string itemUpgradeName = "Upgraded Ero Detection App";

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

            disp.Add(StoreApi.RegisterShopItem(
                itemId: MoreShopItemsPlugin.UpgDetectorItemId,
                itemName: itemUpgradeName,
                explaination: "An upgrade to the Ero Detection App, lets you know who and what type of activity they're doing. Requires the Ero Detection App",
                shopType: StoreApi.ShopType.NightOnly,
                itemBackground: StoreApi.ShopBackground.Yellow,
                itemCategory: 3,
                stock: 1,
                resetsDaily: false,
                cost: 200,
                sort: 501));

            disp.Add(Harmony.CreateAndPatchAll(typeof(EroDetectorFeat)));

            _notifyMast = inst.Config.Bind(itemName, "Notification on masturbation", true, "If the item is purchased, show a notification whenever any NPC starts a masturbation action.");
            _notifyLesb = inst.Config.Bind(itemName, "Notification on lesbian", true, "If the item is purchased, show a notification whenever any NPC starts a lesbian action.");

            TranslationHelper.TranslateAsync(_infoTextPrefix, s => _infoTextPrefix = s);
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
                                var location = "";
                                TranslationHelper.TryTranslate(param.DisplayName,out location);

                                if (StoreApi.GetItemAmountBought(MoreShopItemsPlugin.UpgDetectorItemId) > 0)
                                {
                                    TranslationHelper.TranslateAsync(npc.charaData.Name, s =>
                                    {
                                        if (npc.isOnanism)
                                        {
                                            InformationUI.SetAsync(string.Format(_infoTextPrefixMast, s, location), InformationUI.Mode.Normal).Forget();
                                        }
                                        else if (npc.isLesbian)
                                        {
                                            InformationUI.SetAsync(string.Format(_infoTextPrefixLesb, s, location), InformationUI.Mode.Normal).Forget();
                                        }
                                        return;
                                    });
                                    return;
                                }

                                InformationUI.SetAsync(string.Format(_infoTextPrefix, location), InformationUI.Mode.Normal).Forget();

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
