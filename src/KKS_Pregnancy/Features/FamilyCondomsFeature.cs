using System.Threading;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI.MainGame;
using KKAPI.Studio;
using KKAPI.Utilities;

namespace KK_Pregnancy
{
    public class FamilyCondomsFeature : IFeature
    {
        public const int RubberStoreId = 3456700;

        private static int _installed;

        public bool Install(Harmony instance, ConfigFile config)
        {
            if (StudioAPI.InsideStudio || Interlocked.Increment(ref _installed) != 1) return false;

            var rubberCat = StoreApi.RegisterShopItemCategory(ResourceUtils.GetEmbeddedResource("item_rubber.png").LoadTexture());

            StoreApi.RegisterShopItem(RubberStoreId, "Family Making Condoms",
                "Makes pregnancy up to 95% likely after cumming inside with a condom on. Active in all H scenes until the end of the next day, use with caution! (Doesn't work on infertile characters)",
                StoreApi.ShopType.NightOnly, StoreApi.ShopBackground.Pink, rubberCat, 1, true, 100, 460);

            instance.PatchAll(typeof(FamilyCondomsFeature));

            return true;
        }

        private static bool IsEffectActive()
        {
            // Check from the moment item is bought up to the end of the next day
            return StoreApi.GetItemAmountBought(RubberStoreId) + StoreApi.GetShopItemEffect(RubberStoreId) > 0;
        }

        [HarmonyPrefix]
        [HarmonyWrapSafe] // Ignore crashes
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuCondomInside))]
        private static void OnFinishInside(HFlag __instance)
        {
            if (!IsEffectActive()) return;

            var heroine = __instance.GetLeadingHeroine();
            var controller = PregnancyPlugin.GetEffectController(heroine);

            if (controller.Data.GameplayEnabled && !controller.Data.IsPregnant && controller.Data.Fertility > 0.001f)
            {
                var isDangerousDay = HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日;
                if (UnityEngine.Random.RandomRangeInt(0, 100) < (isDangerousDay ? 95 : 30))
                {
                    PregnancyPlugin.Logger.LogInfo("Through the power of a pin hole, pregnancy");
                    PregnancyGameController.StartPregnancy(heroine);
                }
            }
        }

        // effect of cum leaking out despite condom on. doesn't work no idea why
        //[HarmonyPrefix]
        //[HarmonyWrapSafe]
        //[HarmonyPatch(typeof(HActionBase), nameof(HActionBase.SetPlay))]
        //private static void ProcPre(HActionBase __instance, ref string _nextAnimation)
        //{
        //    if (_nextAnimation != "OUT_A" && _nextAnimation != "A_OUT_A") return;
        //    if (!IsEffectActive()) return;
        //
        //    var animState = __instance.flags.GetLeadingHeroine().chaCtrl.getAnimatorStateInfo(0);
        //    if (animState.IsName("Pull") || animState.IsName("A_Pull"))
        //    {
        //        _nextAnimation = !__instance.flags.isAnalPlay ? "Drop" : "A_Drop";
        //    }
        //}
    }
}
