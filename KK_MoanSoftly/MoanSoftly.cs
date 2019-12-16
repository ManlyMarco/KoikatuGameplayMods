using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Harmony;
using HarmonyLib;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_MoanSoftly
{
    [BepInPlugin(GUID, "Moan softly when I H you", Version)]
    [BepInDependency(KKAPI.KoikatuAPI.VersionConst, "1.6")]
    public class MoanSoftly : BaseUnityPlugin
    {
        public const string GUID = "KK_MoanSoftly";
        public const string Version = "1.0";

        private static float _additionalVolume;
        private static MethodInfo _findMethod;
        private static MethodInfo _addMethod;

        private void Awake()
        {
            _findMethod = AccessTools.Method(typeof(ChaControl), nameof(ChaControl.SetVoiceTransform));
            if (_findMethod == null) throw new ArgumentNullException(nameof(_findMethod));
            _addMethod = AccessTools.Method(typeof(MoanSoftly), nameof(ApplyBreathingTweaks));
            if (_addMethod == null) throw new ArgumentNullException(nameof(_addMethod));

            HarmonyWrapper.PatchAll(typeof(MoanSoftly));
        }

        private static void ApplyBreathingTweaks(ChaControl _female)
        {
            var hFlag = FindObjectOfType<HFlag>();

            var startValue = 30;

            var attribute = _female.chaFile.parameter.attribute;
            if (attribute.bitch || attribute.choroi) startValue += 20;
            if (attribute.hitori || attribute.kireizuki || attribute.dokusyo) startValue -= 20;
            if (attribute.majime) startValue -= 20;

            var heroine = _female.GetHeroine() ?? hFlag.GetLeadingHeroine();
            if (heroine != null)
            {
                startValue += ((int)heroine.HExperience - 2) * 10;
                if (!heroine.isGirlfriend)
                    startValue -= 10;
            }

            startValue = Mathf.Clamp(startValue, 20, 100);

            var reducedGauge = hFlag.gaugeFemale - hFlag.gaugeFemale / 3;
            _additionalVolume = Mathf.Max(hFlag.gaugeFemale, reducedGauge);

            var calculatedVolume = startValue + hFlag.GetOrgCount() * 20 + _additionalVolume / 2;

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
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Callvirt && _findMethod.Equals(instruction.operand))
                {
                    // load ChaControl _female
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, _addMethod);
                }
            }
        }
    }
}
