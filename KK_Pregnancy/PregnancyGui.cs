using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using UniRx;
using UnityEngine;

namespace KK_Pregnancy
{
    public static partial class PregnancyGui
    {
        private static PregnancyPlugin _pluginInstance;

        internal static void Init(Harmony hi, PregnancyPlugin instance)
        {
            _pluginInstance = instance;

            if (StudioAPI.InsideStudio)
            {
                RegisterStudioControls();
            }
            else
            {
                MakerAPI.RegisterCustomSubCategories += MakerAPI_MakerBaseLoaded;

                StatusIcons.Init(hi);
                HideHSceneMenstrIcon.Init(hi, StatusIcons._unknownSprite);
            }
        }

        private static void RegisterStudioControls()
        {
            var cat = StudioAPI.GetOrCreateCurrentStateCategory(null);
            cat.AddControl(new CurrentStateCategorySlider("Pregnancy", c =>
                {
                    if (c.charInfo == null) return 0;
                    var controller = c.charInfo.GetComponent<PregnancyCharaController>();
                    if (controller == null) return 0;
                    return controller.Week;
                }, 0, 40))
                .Value.Subscribe(f => { foreach (var ctrl in StudioAPI.GetSelectedControllers<PregnancyCharaController>()) ctrl.Week = Mathf.RoundToInt(f); });
        }

        private static void MakerAPI_MakerBaseLoaded(object sender, RegisterSubCategoriesEvent e)
        {
            // Only female characters
            if (MakerAPI.GetMakerSex() == 0) return;

            // This category is inaccessible from class maker
            var cat = new MakerCategory(MakerConstants.Parameter.Character.CategoryName, "Pregnancy"); //MakerConstants.Parameter.Character;
            e.AddSubCategory(cat);

            var hintColor = new Color(0.7f, 0.7f, 0.7f);

            var gameplayToggle = e.AddControl(new MakerToggle(cat, "Enable pregnancy progression", true, _pluginInstance));
            gameplayToggle.BindToFunctionController<PregnancyCharaController, bool>(controller => controller.GameplayEnabled, (controller, value) => controller.GameplayEnabled = value);

            e.AddControl(new MakerText("If off, the character can't get pregnant and current pregnancy will stop progressing.", cat, _pluginInstance) { TextColor = hintColor });

            var fertilityToggle = e.AddControl(new MakerSlider(cat, "Fertility", 0f, 1f, PregnancyDataUtils.DefaultFertility, _pluginInstance));
            fertilityToggle.BindToFunctionController<PregnancyCharaController, float>(controller => controller.Fertility, (controller, value) => controller.Fertility = value);

            e.AddControl(new MakerText("How likely this character is to get pregnant.", cat, _pluginInstance) { TextColor = hintColor });

            var weeksSlider = e.AddControl(new MakerSlider(cat, "Week of pregnancy", 0f, PregnancyDataUtils.LeaveSchoolWeek - 1f, 0f, _pluginInstance));
            weeksSlider.ValueToString = f => Mathf.RoundToInt(f).ToString();
            weeksSlider.StringToValue = s => int.Parse(s);
            weeksSlider.BindToFunctionController<PregnancyCharaController, float>(controller => controller.Week, (controller, value) => controller.Week = Mathf.RoundToInt(value));

            e.AddControl(new MakerText("If the character is pregnant when added to the game, the pregnancy will continue from this point.", cat, _pluginInstance) { TextColor = hintColor });

            var scheduleToggle = e.AddControl(new MakerRadioButtons(cat, _pluginInstance, "Menstruation schedule", "Default", "More risky", "Always safe", "Always risky"));
            scheduleToggle.BindToFunctionController<PregnancyCharaController, int>(controller => (int)controller.Schedule, (controller, value) => controller.Schedule = (PregnancyDataUtils.MenstruationSchedule)value);

            e.AddControl(new MakerText("Changes how many risky days the character has in a cycle. Default is more safe days than risky days.", cat, _pluginInstance) { TextColor = hintColor });
        }
    }
}
