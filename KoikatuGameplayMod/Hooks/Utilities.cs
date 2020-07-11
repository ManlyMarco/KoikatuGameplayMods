using System;
using System.Reflection;
using HarmonyLib;
using UniRx;

namespace KoikatuGameplayMod
{
    internal static class Utilities
    {
        public static void ApplyHooks(Harmony instance)
        {
            instance.PatchAll(typeof(Utilities));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), "Start", new Type[] { })]
        public static void HookToEndHButton(HSprite __instance)
        {
            var f = typeof(HSprite).GetField("btnEnd", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f == null) throw new ArgumentException("Could not find field btnEnd in HSprite");
            var b = f.GetValue(__instance) as UnityEngine.UI.Button;
            b.OnClickAsObservable().Subscribe(unit => HSceneEndClicked?.Invoke(__instance));
        }

        public static event Action<HSprite> HSceneEndClicked;

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
            return (__instance.mode == HFlag.EMode.houshi3P || __instance.mode == HFlag.EMode.sonyu3P) ? (__instance.nowAnimationInfo.id % 2) : 0;
        }

        public static int GetTargetHeroineId(HSprite __instance)
        {
            return (__instance.flags.mode == HFlag.EMode.houshi3P || __instance.flags.mode == HFlag.EMode.sonyu3P) ? (__instance.flags.nowAnimationInfo.id % 2) : 0;
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