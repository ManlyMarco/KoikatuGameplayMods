using System;
using KKAPI.MainGame;
using KKAPI.Utilities;
using SaveData;
using UniRx;
using UnityEngine;

namespace MoreShopItems.Features
{
    public class TwinTalismanFeat : IFeature
    {
        public bool ApplyFeature(ref CompositeDisposable disp, MoreShopItemsPlugin inst)
        {
            var ico = ResourceUtils.GetEmbeddedResource("item_talisman.png", typeof(MoreShopItemsPlugin).Assembly).LoadTexture();
            var talismanCategoryId = StoreApi.RegisterShopItemCategory(ico);

            disp.Add(ico);

            const int talismanItemId = 3456650;
            const int maxTalismansOwned = 2;
            disp.Add(StoreApi.RegisterShopItem(
                itemId: talismanItemId,
                itemName: "Twin-making Talisman",
                explaination: "An old talisman that was allegedly used for conversing with spirits in the past. It can summon a fake twin of any living person that is experienced in sex. (One time use during H scenes)",
                shopType: StoreApi.ShopType.NightOnly,
                itemBackground: StoreApi.ShopBackground.Pink,
                itemCategory: talismanCategoryId,
                stock: maxTalismansOwned,
                resetsDaily: false,
                cost: 100,
                numText: "{0} remaining out of " + maxTalismansOwned));

            disp.Add(CustomTrespassingHsceneButtons.AddHsceneTrespassingButtonWithConfirmation(
                buttonText: "Let's use a Twin-making Talisman",
                spawnConditionCheck: hSprite =>
                {
                    var flags = hSprite.flags;
                    return (flags.mode == HFlag.EMode.aibu || flags.mode == HFlag.EMode.sonyu || flags.mode == HFlag.EMode.houshi)
                           && !flags.isFreeH
                           // 3P is only available for experienced or horny
                           && flags.lstHeroine[0].HExperience >= Heroine.HExperienceKind.慣れ
                           && StoreApi.GetItemAmountBought(talismanItemId) > 0
                           // Only enable if not coming from a peeping H scene, because trying to start 3P in that state just ends the H scene
                           && GameObject.FindObjectOfType<HScene>().dataH.peepCategory.Count == 0;
                },
                confirmBoxTitle: "Hシーン確認",
                confirmBoxSentence: "Do you want to use one of your Twin-making Talismans?\n\nIt's supposed to create a twin of the girl, but what will actually happen?",
                onConfirmed: hSprite =>
                {
                    MoreShopItemsPlugin.Logger.LogDebug("Twin-making Talisman used");

                    // Custom code, copy current heroine and add a copy to the "other heroine waiting" field
                    var hsp = GameObject.FindObjectOfType<HSceneProc>();
                    if (hsp == null) throw new ArgumentNullException(nameof(hsp));

                    var currHeroine = hsp.dataH.lstFemale[0];
                    if (currHeroine == null) throw new ArgumentNullException(nameof(currHeroine));

                    MoreShopItemsPlugin.Logger.LogDebug($"Creating a copy of the main heroine: {currHeroine.Name} chaCtrl={currHeroine.chaCtrl}");
                    Heroine.SetBytes(currHeroine.version, Heroine.GetBytes(currHeroine), out var copyHeroine);
                    copyHeroine.chaCtrl = currHeroine.chaCtrl;
                    hsp.dataH.newHeroione = copyHeroine;

                    // Stock game code to initialize the 3P transition
                    MoreShopItemsPlugin.Logger.LogDebug("Starting 3P transition copied character");
                    var flags = hSprite.flags;
                    flags.click = HFlag.ClickKind.end;
                    flags.isHSceneEnd = true;
                    flags.numEnd = 2;
                    //flags.lstHeroine[0].lewdness = 100;
                    var asi = ActionScene.instance;
                    if (asi == null) throw new ArgumentNullException(nameof(asi));
                    if (asi) asi.isPenetration = true;
                    if (flags.shortcut != null) flags.shortcut.enabled = false;
                    if (asi) asi.ShortcutKeyEnable(false);

                    // Eat one of the held items
                    StoreApi.DecreaseItemAmountBought(talismanItemId);
                }));

            return true;
        }
    }
}