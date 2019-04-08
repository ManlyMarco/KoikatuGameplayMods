using System.Collections.Generic;
using ADV.Commands.Game;
using Harmony;

namespace KoikatuGameplayMod
{
    internal static class BustSizeQuestionHooks
    {
        public static void ApplyHooks(HarmonyInstance instance)
        {
            instance.PatchAll(typeof(BustSizeQuestionHooks));
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CharaPersonal), "GetBustSize", new[] { typeof(ChaFileControl) })]
        public static IEnumerable<CodeInstruction> GetBustSizeTranspiler(IEnumerable<CodeInstruction> instr)
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