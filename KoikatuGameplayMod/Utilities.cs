namespace KoikatuGameplayMod
{
    internal static class Utilities
    {
        public static SaveData.Heroine GetTargetHeroine(HFlag __instance)
        {
            return __instance.lstHeroine[GetTargetHeroineId(__instance)];
        }
        public static SaveData.Heroine GetTargetHeroine(HSprite __instance)
        {
            return __instance.flags.lstHeroine[GetTargetHeroineId(__instance)];
        }

        public static int GetTargetHeroineId(HFlag __instance)
        {
            return (__instance.mode >= HFlag.EMode.houshi3P) ? (__instance.nowAnimationInfo.id % 2) : 0;
        }

        public static int GetTargetHeroineId(HSprite __instance)
        {
            return (__instance.flags.mode >= HFlag.EMode.houshi3P) ? (__instance.flags.nowAnimationInfo.id % 2) : 0;
        }

        public static void ForceAllowInsert(HSprite instance)
        {
            instance.flags.isDebug = true;
        }

        public static void ResetForceAllowInsert(HSprite __instance)
        {
            __instance.flags.isDebug = false;
        }
    }
}