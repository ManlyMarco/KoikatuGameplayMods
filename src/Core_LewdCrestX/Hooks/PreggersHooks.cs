using System;
using System.Collections.Generic;
using HarmonyLib;
using KKAPI.Chara;

namespace KK_LewdCrestX
{
    internal static class PreggersHooks
    {
        private static readonly HashSet<SaveData.Heroine> _tempPreggers = new HashSet<SaveData.Heroine>();

        private static bool _patched;

        public static bool TryPatchPreggers(Harmony hi)
        {
#if KK
            var lactType = Type.GetType("KK_Pregnancy.LactationController, KK_Pregnancy", false);
            if (lactType != null)
            {
                var lactDataType = lactType.GetNestedType("CharaData", AccessTools.all);
                if (lactDataType != null)
                {
                    var milkAmountMethod = lactDataType.GetMethod("GetMilkAmount", AccessTools.all);
                    if (milkAmountMethod != null)
                    {
                        hi.Patch(milkAmountMethod,
                            postfix: new HarmonyMethod(typeof(PreggersHooks), nameof(PreggersHooks.GetMilkAmountPatch)));
                        _patched = true;
                        return true;
                    }
                    else
                        LewdCrestXPlugin.Logger.LogWarning(
                            "Could not find KK_Pregnancy.LactationController.CharaData.GetMilkAmount - something isn't right, please report this");
                }
                else
                    LewdCrestXPlugin.Logger.LogWarning(
                        "Could not find KK_Pregnancy.LactationController.CharaData - something isn't right, please report this");
            }
            else
                LewdCrestXPlugin.Logger.LogWarning(
                    "Could not find KK_Pregnancy.LactationController, some features might not work until you install KK_Pregnancy (please report this if you do have latest version of KK_Pregnancy installed)");
#endif

            return false;
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
#if KK
            if (!_patched) return;

            if (_tempPreggers.Add(heroine))
                LewdCrestXPlugin.Logger.LogInfo("Triggering temporary pregnancy because of breedgasm crest: " + heroine.charFile?.parameter?.fullname);
#endif
        }

        public static void OnPeriodChanged()
        {
#if KK
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
#endif
        }

        public static void ClearTempPreggers()
        {
#if KK
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
#endif
        }
    }
}