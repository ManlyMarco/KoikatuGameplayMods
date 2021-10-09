using System.Linq;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UnityEngine;

namespace KoikatuGameplayMod
{
    internal class TrapNoVagInsertHooks : IFeature
    {
        public bool Install(Harmony instance, ConfigFile config)
        {
            var s = config.Bind(KoikatuGameplayMod.ConfCatHScene, "Disable vaginal insert for traps/men", true,
                "Only works if you use UncensorSelector to give a female card a penis but no vagina in maker. Some positions don't have the anal option so you won't be able to insert at all in them.\nChanges take effect after game restart.");

            if (s.Value)
                instance.PatchAll(typeof(TrapNoVagInsertHooks));

            return true;
        }

        /// <summary>
        /// Only do normal 2P stuff. 3P has no anal at all but it's good enough all things considered
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.SetSonyuStart))]
        public static void SetSonyuStartPost(HSprite __instance)
        {
            var isTrap = IsATrap(__instance.GetLeadingHeroine());
            var lstButton = __instance.sonyu.categoryActionButton.lstButton;

            // Toggle the front insert buttons. Anal buttons won't work properly if they are not already enabled so don't force enable.
            lstButton[0].gameObject.SetActive(!isTrap);
            lstButton[1].gameObject.SetActive(!isTrap);

            // Prevent getting stuck
            if (isTrap)
                __instance.flags.isAnalInsertOK = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalInside))]
        public static void AddSonyuAnalInsidePre(HFlag __instance)
        {
            OnAnalCum(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalCondomInside))]
        public static void AddSonyuAnalCondomInsidePre(HFlag __instance)
        {
            OnAnalCum(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOutside))]
        public static void AddSonyuAnalOutsidePre(HFlag __instance)
        {
            OnAnalCum(__instance);
        }

        private static void OnAnalCum(HFlag hFlag)
        {
            if (IsATrap(hFlag.GetLeadingHeroine()))
            {
                // If it's a trap, disable the first h guide after coming inside the back hole (wouldn't disappear otherwise)
                Object.FindObjectOfType<HSprite>()?.objFirstHHelpBase?.SetActive(false);
            }
        }

        private static bool IsATrap(SaveData.Heroine heroine)
        {
            return heroine.GetRelatedChaFiles().Any(IsATrap);
        }

        private static bool IsATrap(ChaFileControl chaFile)
        {
            const string selectorGuid = "com.deathweasel.bepinex.uncensorselector";
            var data = ExtendedSave.GetExtendedDataById(chaFile, selectorGuid);

            if (data == null)
                return false;

            if (!data.data.TryGetValue("BodyGUID", out var bodyGuid) || !(bodyGuid is string bodyGuidStr))
                return false;

            if (bodyGuidStr != "Default.Body.Female" && bodyGuidStr != "Default.Body.Male")
                return false;

            return data.data.TryGetValue("DisplayPenis", out var displayPenis) && displayPenis is bool p && p;
        }
    }
}