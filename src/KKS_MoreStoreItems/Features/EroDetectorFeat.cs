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
        private static string _infoTextPrefixGeneral = "エロ活動：{0}";

        public bool ApplyFeature(ref CompositeDisposable disp, MoreShopItemsPlugin inst)
        {
            const string itemName = "Ero Detection App";

            disp.Add(StoreApi.RegisterShopItem(
                itemId: MoreShopItemsPlugin.DetectorItemId,
                itemName: itemName,
                explaination: "A phone app that notifies you about erotic activities happening around the island. It's fully automatic and gives estimated locations. Further upgrade lets you see who and what is being done",
                shopType: StoreApi.ShopType.NightOnly,
                itemBackground: StoreApi.ShopBackground.Yellow,
                itemCategory: 3,
                stock: 2,
                resetsDaily: false,
                cost: 200,
                sort: 500,
                numText: "{0} available upgrades"));

            disp.Add(Harmony.CreateAndPatchAll(typeof(EroDetectorFeat)));

            _notifyMast = inst.Config.Bind(itemName, "Notification on masturbation", true, "If the item is purchased, show a notification whenever any NPC starts a masturbation action.");
            _notifyLesb = inst.Config.Bind(itemName, "Notification on lesbian", true, "If the item is purchased, show a notification whenever any NPC starts a lesbian action.");

            TranslationHelper.TranslateAsync(_infoTextPrefixGeneral, s => _infoTextPrefixGeneral = s);
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

            if (__instance is NPC npc && (npc.isOnanism && _notifyMast.Value || npc.isLesbian && _notifyLesb.Value))
            {
                try
                {
                    var featureLevel = StoreApi.GetItemAmountBought(MoreShopItemsPlugin.DetectorItemId);
                    if (featureLevel > 0)
                    {
                        var mapNo = __instance.mapNo;
                        //if (ActionScene.initialized && ActionScene.instance.Player.mapNo != mapNo)
                        if (ActionScene.instance.Map.infoDic.TryGetValue(mapNo, out var param))
                        {
                            TranslationHelper.TryTranslate(param.DisplayName, out var locationName);

                            if (featureLevel > 1)
                            {
                                TranslationHelper.TranslateAsync(
                                    npc.charaData.Name,
                                    s => InformationUI.SetAsync(string.Format(npc.isOnanism ? _infoTextPrefixMast : _infoTextPrefixLesb, s, locationName), InformationUI.Mode.Normal).Forget());
                            }
                            else
                            {
                                InformationUI.SetAsync(string.Format(_infoTextPrefixGeneral, locationName), InformationUI.Mode.Normal).Forget();
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
