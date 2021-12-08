using System;
using ActionGame.Communication;
using HarmonyLib;
using Illusion.Extensions;
using KKAPI.MainGame;
using Manager;
using UnityEngine;
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

            _isHEvent =
#if KK
                _command == 3;
#elif KKS
                _command == 20;
#endif
            Console.WriteLine($"GetEventADVPrefix crest={_currentCrestType} _command={_command} _isHEvent={_isHEvent}");
        }
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Info), nameof(Info.GetEventADV))]
        static void GetEventADVFinalizer()
        {
            Console.WriteLine($"GetEventADVFinalizer crest={_currentCrestType}");
            _currentCrestType = CrestType.None;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TalkScene), nameof(TalkScene.ReflectChangeValue))]
        static void ReflectChangeValuePrefix(TalkScene __instance, bool ___isDesire)
        {
            // This is set by using the talk lewd option
            if (___isDesire)
            {
                var heroine = __instance.targetHeroine;
                if (heroine.GetCurrentCrest() == CrestType.triggered)
                {
                    var actCtrl = GameAPI.GetActionControl();
                    actCtrl.AddDesire(4, heroine, 60);
                    actCtrl.AddDesire(5, heroine, 60);
                    actCtrl.AddDesire(26, heroine, heroine.parameter.attribute.likeGirls ? 60 : 30);
                    actCtrl.AddDesire(29, heroine, 40);
                    heroine.lewdness = Mathf.Min(100, heroine.lewdness + 60);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TalkScene), nameof(TalkScene.UpdateUI))]
        static void UpdateUIPrefix(TalkScene __instance)
        {
            _currentCrestType = __instance.targetHeroine.GetCurrentCrest();
            Console.WriteLine($"UpdateUIPrefix crest={_currentCrestType}");
            _isHEvent = false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(TalkScene), nameof(TalkScene.UpdateUI))]
        static void UpdateUIFinalizer(TalkScene __instance)
        {
            Console.WriteLine($"UpdateUIFinalizer crest={_currentCrestType}");
            if (_currentCrestType == CrestType.libido)
            {
                // todo avoid using index for better compat?
#if KK
                // 3 is lets have h
                __instance.buttonEventContents[3]?.gameObject.SetActiveIfDifferent(true);
#elif KKS
                // 1 is lets have h
                __instance.buttonR18Contents[1]?.gameObject.SetActiveIfDifferent(true);
                // Need to turn on the main r18 button in case none of the sub options were active
                __instance.buttonInfos[4].Active = true;
#else
                throw new NotImplementedException();
#endif
            }

            _currentCrestType = CrestType.None;
        }

        // Used to override relationship level during talkscene conversation
        [HarmonyPostfix]
#if KK
        [HarmonyPatch(typeof(Info), nameof(Info.GetStage))]
#else
        [HarmonyPatch(typeof(Info), nameof(Info.relation), MethodType.Getter)] //todo test if works
#endif
        static void GetStagePatch(ref int __result)
        {
            Console.WriteLine($"GetStagePatch crest={_currentCrestType}");
            switch (_currentCrestType)
            {
                case CrestType.libido:
                    if (_isHEvent && __result < 2) __result = 2;
                    break;
                case CrestType.command:
                    if (__result == 0) __result = 1;
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Info), nameof(Info.RandomBranch))]
        static void RandomBranchPatch(ref int __result)
        {
            Console.WriteLine($"RandomBranchPatch crest={_currentCrestType}");
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
        [HarmonyPatch(typeof(PassingInfo), nameof(PassingInfo.isHPossible), MethodType.Getter)]
        static void isHPossiblePatch(ref bool __result, PassingInfo __instance)
        {
            var crest = _currentCrestType;
            if (_currentCrestType == CrestType.None)
                crest = __instance.heroine.GetCurrentCrest();

            Console.WriteLine($"isHPossiblePatch crest={_currentCrestType}");

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