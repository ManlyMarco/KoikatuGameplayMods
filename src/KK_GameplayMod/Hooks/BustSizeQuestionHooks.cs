using System.Collections.Generic;
using ADV.Commands.Game;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;

namespace KoikatuGameplayMod
{
    internal class BustSizeQuestionHooks : IFeature
    {
        public bool Install(Harmony instance, ConfigFile config)
        {
            if (KoikatuAPI.IsVR()) return false;

            var s = config.Bind(KoikatuGameplayMod.ConfCatMainGame, "Adjust preferred breast size question", true,
                "Lowers the breast size needed for 'Average' and 'Large' breast options when a heroine asks you what size you prefer.\nChanges take effect after game restart.");
            
            if (s.Value)
                instance.PatchAll(typeof(BustSizeQuestionHooks));
            
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CharaPersonal), nameof(CharaPersonal.GetBustSize), typeof(ChaFileControl))]
        private static IEnumerable<CodeInstruction> GetBustSizeTranspiler(IEnumerable<CodeInstruction> instr)
        {
            foreach (var instruction in instr)
            {
                if (instruction.operand is float f && Equals(f, 0.4f))
                {
                    instruction.operand = 0.3f;
                }
                else if (instruction.operand is float f2 && Equals(f2, 0.7f))
                {
                    instruction.operand = 0.55f;
                }
                yield return instruction;
            }
        }
    }
}