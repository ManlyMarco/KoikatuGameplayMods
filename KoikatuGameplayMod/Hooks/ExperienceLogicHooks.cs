using HarmonyLib;

namespace KoikatuGameplayMod
{
    internal static class ExperienceLogicHooks
    {
        public static void ApplyHooks(Harmony instance)
        {
            instance.PatchAll(typeof(ExperienceLogicHooks));
        }

        /// <summary>
        /// Originally from https://github.com/Fulmene/KK_ExperienceLogic/blob/master/KK_ExperienceLogic/ExperienceLogic.Hooks.cs
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveData.Heroine), nameof(SaveData.Heroine.HExperience), MethodType.Getter)]
        public static void GetHExperiencePost(SaveData.Heroine __instance, ref SaveData.Heroine.HExperienceKind __result)
        {
            if (__result == SaveData.Heroine.HExperienceKind.不慣れ) // inexperienced
            {
                float caressBreasts = __instance.hAreaExps[1];
                float caressVagina = __instance.hAreaExps[2];
                float caressAnus = __instance.hAreaExps[3];
                float caressButt = __instance.hAreaExps[4];
                float caressNipple = __instance.hAreaExps[5];
                float service = __instance.houshiExp;
                float pistonVagina = __instance.countKokanH;
                float pistonAnus = __instance.countAnalH;

                const int threshold = 99; // 100 is default
                if (caressBreasts >= threshold && caressButt >= threshold && caressNipple >= threshold && service >= threshold)
                {
                    if ((caressVagina >= threshold && pistonVagina >= threshold) || (caressAnus >= threshold && pistonAnus >= threshold))
                    {
                        __result = __instance.lewdness >= threshold
                            ? SaveData.Heroine.HExperienceKind.淫乱 // lewd
                            : SaveData.Heroine.HExperienceKind.慣れ; // experienced
                    }
                }
            }
        }
    }
}