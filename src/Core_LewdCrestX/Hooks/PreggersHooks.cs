using System;
using System.Collections.Generic;
using HarmonyLib;
using KKAPI.Chara;
using UnityEngine;

namespace KK_LewdCrestX
{
    internal static class PreggersHooks
    {
        private static readonly HashSet<SaveData.Heroine> _tempPreggers = new HashSet<SaveData.Heroine>();

        private static bool _patched;

        public static bool TryPatchPreggers(Harmony hi)
        {
            // Lactation patches
            {
#if KK
                var lactType = Type.GetType("KK_Pregnancy.LactationController, KK_Pregnancy", false);
#elif KKS
                var lactType = Type.GetType("KK_Pregnancy.LactationController, KKS_Pregnancy", false);
#endif
                if (lactType == null)
                {
                    LewdCrestXPlugin.Logger.LogWarning("Could not find KK_Pregnancy.LactationController, some features might not work until you install KK_Pregnancy (please report this if you do have latest version of KK_Pregnancy installed)");
                    return false;
                }

                var lactDataType = lactType.GetNestedType("CharaData", AccessTools.all);
                if (lactDataType == null)
                {
                    LewdCrestXPlugin.Logger.LogWarning("Could not find KK_Pregnancy.LactationController.CharaData - something isn't right, please report this");
                    return false;
                }

                var milkAmountMethod = lactDataType.GetMethod("GetMilkAmount", AccessTools.all);
                if (milkAmountMethod == null)
                {
                    LewdCrestXPlugin.Logger.LogWarning("Could not find KK_Pregnancy.LactationController.CharaData.GetMilkAmount - something isn't right, please report this");
                    return false;
                }

                hi.Patch(milkAmountMethod, postfix: new HarmonyMethod(typeof(PreggersHooks), nameof(GetMilkAmountPatch)));
            }

            // Stat patches, needs newer version of the preggers plugin than Lactation patches
            try
            {
#if KK
                var utilsType = Type.GetType("KK_Pregnancy.PregnancyDataUtils, KK_Pregnancy", true);
#elif KKS
                var utilsType = Type.GetType("KK_Pregnancy.PregnancyDataUtils, KKS_Pregnancy", true);
#endif
                hi.Patch(utilsType.GetMethod(nameof(KK_Pregnancy.PregnancyDataUtils.GetFertility), AccessTools.allDeclared), postfix: new HarmonyMethod(typeof(PreggersHooks), nameof(GetFertilityHook)));
                hi.Patch(utilsType.GetMethod(nameof(KK_Pregnancy.PregnancyDataUtils.GetMenstruation), AccessTools.allDeclared), postfix: new HarmonyMethod(typeof(PreggersHooks), nameof(GetMenstruationHook)));
                hi.Patch(utilsType.GetMethod(nameof(KK_Pregnancy.PregnancyDataUtils.GetPregnancyProgressionSpeed), AccessTools.allDeclared), postfix: new HarmonyMethod(typeof(PreggersHooks), nameof(GetPregnancyProgressionSpeedHook)));
            }
            catch (Exception ex)
            {
                LewdCrestXPlugin.Logger.LogWarning("Could not patch KK_Pregnancy.PregnancyDataUtils, some features might not work until you install the latest version of KK_Pregnancy. Details: " + ex);
            }

            _patched = true;
            return true;
        }

        public static void GetFertilityHook(SaveData.CharaData character, ref float __result)
        {
            if (character is SaveData.Heroine heroine)
            {
                Console.WriteLine("GetFertilityHook");
                if (heroine.GetCurrentCrest() == CrestType.broodmother)
                {
                    // result is 0-1 range
                    __result = Mathf.Max(__result, 0.96f);
                    LewdCrestXPlugin.Logger.LogDebug($"Overriding GetFertility for {character.GetFullname()} to {__result} because of broodmother crest");
                }
            }
        }

        public static void GetMenstruationHook(SaveData.CharaData character, ref object __result)
        {
            if (character is SaveData.Heroine heroine)
            {
                if (heroine.GetCurrentCrest() == CrestType.broodmother)
                {
                    // result is actually enum MenstruationSchedule
                    __result = KK_Pregnancy.MenstruationSchedule.AlwaysRisky;
                    LewdCrestXPlugin.Logger.LogDebug($"Overriding GetMenstruation for {character.GetFullname()} to {__result} because of broodmother crest");
                }
            }
        }

        public static void GetPregnancyProgressionSpeedHook(SaveData.CharaData character, ref int __result)
        {
            if (character is SaveData.Heroine heroine)
            {
                if (heroine.GetCurrentCrest() == CrestType.broodmother)
                {
                    // todo Only max 20 for 2 weeks? If it worked on days instead of whole weeks it could be more granular
                    __result = Mathf.Min(20, __result * 5);
                    LewdCrestXPlugin.Logger.LogDebug($"Overriding GetPregnancyProgression for {character.GetFullname()} to {__result} because of broodmother crest");
                }
            }
        }

        private static void GetMilkAmountPatch(CharaCustomFunctionController controller, ref float __result)
        {
            if (__result < 1f && controller != null)
            {
                if (controller.ChaControl.GetCurrentCrest() == CrestType.lactation)
                    __result = 1f;
            }
        }

        public static void ApplyTempPreggers(SaveData.Heroine heroine)
        {
            if (!_patched) return;

            if (_tempPreggers.Add(heroine))
                LewdCrestXPlugin.Logger.LogInfo($"Triggering temporary pregnancy because of breedgasm crest for {heroine.GetFullname()}");
        }

        public static void OnPeriodChanged()
        {
            if (!_patched) return;

            // Apply the effect now to get a delay
            foreach (var heroine in _tempPreggers)
            {
                var pregCtrl = heroine?.GetCrestController()?.GetComponent<KK_Pregnancy.PregnancyCharaController>();
                if (pregCtrl != null)
                {
                    pregCtrl.Data.Week = KK_Pregnancy.PregnancyData.LeaveSchoolWeek;
                    pregCtrl.SaveData();
                }
            }
        }

        public static void ClearTempPreggers()
        {
            if (!_patched) return;

            foreach (var heroine in _tempPreggers)
            {
                var pregCtrl = heroine?.GetCrestController()?.GetComponent<KK_Pregnancy.PregnancyCharaController>();
                if (pregCtrl != null)
                {
                    pregCtrl.Data.Week = 0;
                    pregCtrl.SaveData();
                }
            }
            _tempPreggers.Clear();
        }
    }
}