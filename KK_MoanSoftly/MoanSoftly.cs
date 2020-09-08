using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using KKAPI.Studio;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_MoanSoftly
{
    [BepInPlugin(GUID, "Moan softly when I H you", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class MoanSoftly : BaseUnityPlugin
    {
        public const string GUID = "KK_MoanSoftly";
        public const string Version = "1.0";

        private void Awake()
        {
            if (StudioAPI.InsideStudio) return;

            Harmony.CreateAndPatchAll(typeof(MoanSoftly));
        }

        private static HFlag _hFlag;
        private static void ApplyBreathingTweaks(ChaControl _female)
        {
            if (_hFlag == null)
                _hFlag = FindObjectOfType<HFlag>();

            var startValue = 30;

            var attribute = _female.chaFile.parameter.attribute;
            if (attribute.bitch || attribute.choroi) startValue += 20;
            if (attribute.hitori || attribute.kireizuki || attribute.dokusyo) startValue -= 20;
            if (attribute.majime) startValue -= 20;

            var heroine = _female.GetHeroine() ?? _hFlag.GetLeadingHeroine();
            if (heroine != null)
            {
                startValue += ((int)heroine.HExperience - 2) * 10;
                if (!heroine.isGirlfriend)
                    startValue -= 10;
            }

            startValue = Mathf.Clamp(startValue, 20, 100);

            var calculatedVolume = startValue + _hFlag.GetOrgCount() * 20 + _hFlag.gaugeFemale / 2;

            _female.asVoice.minDistance = Mathf.Clamp(calculatedVolume / 100, 0.17f, 1f);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(HVoiceCtrl), "BreathProc")]
        private static IEnumerable<CodeInstruction> BreathProcTpl(IEnumerable<CodeInstruction> instructions)
        {
            return Apply(instructions);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(HVoiceCtrl), "ShortBreathProc")]
        private static IEnumerable<CodeInstruction> ShortBreathTpl(IEnumerable<CodeInstruction> instructions)
        {
            return Apply(instructions);
        }

        private static IEnumerable<CodeInstruction> Apply(IEnumerable<CodeInstruction> instructions)
        {
            var findMethod = AccessTools.Method(typeof(ChaControl), nameof(ChaControl.SetVoiceTransform));
            if (findMethod == null) throw new ArgumentNullException(nameof(findMethod));
            var addMethod = AccessTools.Method(typeof(MoanSoftly), nameof(ApplyBreathingTweaks));
            if (addMethod == null) throw new ArgumentNullException(nameof(addMethod));

            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Callvirt && findMethod.Equals(instruction.operand))
                {
                    // load ChaControl _female
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, addMethod);
                }
            }
        }
    }
}
