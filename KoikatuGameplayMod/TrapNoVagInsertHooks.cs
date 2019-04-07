using ExtensibleSaveFormat;
using Harmony;

namespace KoikatuGameplayMod
{
    internal static class TrapNoVagInsertHooks
    {
        public static void ApplyHooks(HarmonyInstance instance)
        {
            instance.PatchAll(typeof(TrapNoVagInsertHooks));
        }

        /// <summary>
        /// Only do normal 2P stuff. 3P has no anal at all but it's good enough all things considered
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.SetSonyuStart))]
        public static void SetSonyuStartPost(HSprite __instance)
        {
            var isTrap = IsATrap(Utilities.GetTargetHeroine(__instance));
            var lstButton = __instance.sonyu.categoryActionButton.lstButton;

            // Toggle the front insert buttons. Anal buttons won't work properly if they are not already enabled so don't force enable.
            lstButton[0].gameObject.SetActive(!isTrap);
            lstButton[1].gameObject.SetActive(!isTrap);

            // Prevent getting stuck
            if (isTrap)
                __instance.flags.isAnalInsertOK = true;
        }
        
        private static bool IsATrap(SaveData.Heroine heroine)
        {
            const string selectorGuid = "com.deathweasel.bepinex.uncensorselector";

            var data = ExtendedSave.GetExtendedDataById(heroine.charFile, selectorGuid);

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