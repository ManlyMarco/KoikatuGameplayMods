using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace KoikatuGameplayMod
{
    internal class HidePlayerWhenTouchingHooks : IFeature
    {
        private static ConfigEntry<bool> _dontHidePlayerWhenTouching;

        public bool Install(Harmony instance, ConfigFile config)
        {
            _dontHidePlayerWhenTouching = config.Bind(KoikatuGameplayMod.ConfCatHScene, "Do not hide player when touching", true, 
                "Prevent hiding of the player model when touching in H scenes.");
            
            instance.PatchAll(typeof(HidePlayerWhenTouchingHooks));

            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(HHoushi), nameof(HHoushi.Proc))]
        [HarmonyPatch(typeof(HSonyu), nameof(HSonyu.Proc))]
        private static IEnumerable<CodeInstruction> MaleVisibleOverrideTpl(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(HActionBase), nameof(HActionBase.IsBodyTouch)) ?? throw new ArgumentNullException("HActionBase.IsBodyTouch)");
            var replacement = AccessTools.Method(typeof(HidePlayerWhenTouchingHooks), nameof(HidePlayerWhenTouchingHooks.IsBodyTouchOverride)) ?? throw new ArgumentNullException("HSceneHooks.CanHide");

            var asd = new CodeMatcher(instructions.ToList())
                .MatchForward(true, new CodeMatch(null, target))
                .Repeat(m => m.InsertAndAdvance(new CodeInstruction(OpCodes.Call, replacement)), err => throw new Exception("Nothing replaced. " + err))
                .Instructions().ToList();

            var orig = CodeInstructions(instructions, target, replacement).ToList();

            if (!asd.SequenceEqual(orig)) throw new Exception("aaaaaa");
            return orig;
        }

        private static IEnumerable<CodeInstruction> CodeInstructions(IEnumerable<CodeInstruction> instructions, MethodInfo target, MethodInfo replacement)
        {
            foreach (var codeInstruction in instructions)
            {
                yield return codeInstruction;

                if (codeInstruction.operand == target)
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                }
            }
        }

        private static bool IsBodyTouchOverride(bool isTouch)
        {
            return isTouch && !_dontHidePlayerWhenTouching.Value;
        }
    }
}