using System;
using HarmonyLib;
using KKAPI.Chara;

namespace KK_LewdCrestX
{
    internal static class PreggersHooks
    {
        public static void TryPatchPreggers(Harmony hi)
        {
            var lactType = Type.GetType("KK_Pregnancy.LactationController, KK_Pregnancy", false);
            if (lactType != null)
            {
                var lactDataType = lactType.GetNestedType("CharaData", AccessTools.all);
                if (lactDataType != null)
                {
                    var milkAmountMethod = lactDataType.GetMethod("GetMilkAmount", AccessTools.all);
                    if (milkAmountMethod != null)
                        hi.Patch(milkAmountMethod,
                            postfix: new HarmonyMethod(typeof(PreggersHooks), nameof(PreggersHooks.GetMilkAmountPatch)));
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
        }

        private static void GetMilkAmountPatch(CharaCustomFunctionController controller, ref float __result)
        {
            if (__result < 1f && controller != null)
            {
                if (controller.ChaControl.GetCurrentCrest() == CrestType.lactation)
                    __result = 1f;
            }
        }
    }
}