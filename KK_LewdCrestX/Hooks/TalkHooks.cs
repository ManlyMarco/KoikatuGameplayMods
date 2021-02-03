using System;
using ActionGame.Communication;
using HarmonyLib;
using Illusion.Extensions;
using UnityEngine.UI;

namespace KK_LewdCrestX
{
    internal static class TalkHooks
    {
        private static CrestType _currentCrestType;
        private static bool _isHEvent;
        private static PassingInfo _currentPassingInfo;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Info), nameof(Info.GetEventADV))]
        static void GetEventADVPrefix(Info __instance, int _command, PassingInfo ____passingInfo)
        {
            _currentCrestType = ____passingInfo?.heroine?.GetCurrentCrest() ?? CrestType.None;
            _currentPassingInfo = ____passingInfo;
            Console.WriteLine("GetEventADVPrefix " + _currentCrestType);
            _isHEvent = _command == 3;
        }
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Info), nameof(Info.GetEventADV))]
        static void GetEventADVFinalizer()
        {
            Console.WriteLine("GetEventADVFinalizer " + _currentCrestType);
            _currentCrestType = CrestType.None;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TalkScene), "UpdateUI")]
        static void UpdateUIPrefix(TalkScene __instance)
        {
            _currentCrestType = __instance.targetHeroine.GetCurrentCrest();
            Console.WriteLine("UpdateUIPrefix " + _currentCrestType);
            _isHEvent = false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(TalkScene), "UpdateUI")]
        static void UpdateUIFinalizer(TalkScene __instance, Button[] ___buttonEventContents)
        {
            Console.WriteLine("UpdateUIFinalizer " + _currentCrestType);
            if (_currentCrestType == CrestType.libido)
            {
                // 3 is lets have h
                // todo avoid using index for better compat?
                ___buttonEventContents[3]?.gameObject.SetActiveIfDifferent(true);
            }

            _currentCrestType = CrestType.None;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Info), "GetStage")]
        //private int GetStage()
        static void GetStagePatch(ref int __result)
        {
            Console.WriteLine("GetStagePatch " + _currentCrestType);
            switch (_currentCrestType)
            {
                case CrestType.libido:
                    if (_isHEvent) __result = 2;
                    break;
                case CrestType.command:
                    if (__result == 0) __result = 1;
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Info), "RandomBranch")]
        //private int RandomBranch(params int[] _values)
        static void RandomBranchPatch(ref int __result)
        {
            Console.WriteLine("RandomBranchPatch " + _currentCrestType);
            switch (_currentCrestType)
            {
                case CrestType.libido:
                    if (_isHEvent) __result = 0;
                    break;
                case CrestType.command:
                    __result = 0;
                    break;
                case CrestType.liberated:
                    if (_isHEvent && _currentPassingInfo?.isOtherPeople == true) __result = 0;
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PassingInfo), "isHPossible", MethodType.Getter)]
        static void isHPossiblePatch(ref bool __result, PassingInfo __instance)
        {
            var crest = _currentCrestType;
            if (_currentCrestType == CrestType.None)
                crest = __instance.heroine.GetCurrentCrest();

            Console.WriteLine("isHPossiblePatch " + _currentCrestType);

            switch (crest)
            {
                case CrestType.command:
                case CrestType.libido:
                case CrestType.liberated:
                    __result = true;
                    break;
            }
        }
    }
}