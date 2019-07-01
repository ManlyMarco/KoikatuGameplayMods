using Harmony;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UniRx;
using UnityEngine;

namespace KK_Pregnancy
{
    public static partial class PregnancyGui
    {
        private static MakerToggle _gameplayToggle;
        private static MakerSlider _fertilityToggle;
        private static MakerSlider _weeksSlider;

        private static PregnancyPlugin _pluginInstance;

        internal static void Init(HarmonyInstance hi, PregnancyPlugin instance)
        {
            _pluginInstance = instance;

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;

            // todo add when slider is implemented
            //StudioAPI.CreateCurrentStateCategory(new CurrentStateCategory("Pregnancy", new CurrentStateCategorySubItemBase[]{new CurrentStateCategorySlider()}));

            HeartIcons.Init(hi);
        }

        internal static void UpdateInterface(PregnancyCharaController controller)
        {
            if (MakerAPI.InsideMaker && _gameplayToggle != null)
            {
                _gameplayToggle.Value = controller.GameplayEnabled;
                _fertilityToggle.Value = controller.Fertility;
                _weeksSlider.Value = controller.Week;
            }
        }

        private static PregnancyCharaController GetController()
        {
            return MakerAPI.GetCharacterControl().GetComponent<PregnancyCharaController>();
        }

        private static void MakerAPI_MakerBaseLoaded(object sender, RegisterCustomControlsEvent e)
        {
            // Only female characters
            if (MakerAPI.GetMakerSex() == 0) return;

            // This category is inaccessible from class maker
            var cat = MakerConstants.Parameter.Character;
            var hintColor = new Color(0.7f, 0.7f, 0.7f);

            _gameplayToggle = e.AddControl(new MakerToggle(cat, "Enable pregnancy progression", true, _pluginInstance));
            _gameplayToggle.ValueChanged.Subscribe(val => GetController().GameplayEnabled = val);

            e.AddControl(new MakerText("If off, the character can't get pregnant and current pregnancy will stop progressing.", cat, _pluginInstance) { TextColor = hintColor });

            _fertilityToggle = e.AddControl(new MakerSlider(cat, "Fertility", 0f, 1f, PregnancyDataUtils.DefaultFertility, _pluginInstance));
            _fertilityToggle.ValueChanged.Subscribe(val => GetController().Fertility = val);

            e.AddControl(new MakerText("How likely this character is to get pregnant.", cat, _pluginInstance) { TextColor = hintColor });

            _weeksSlider = e.AddControl(new MakerSlider(cat, "Week of pregnancy", 0f, PregnancyDataUtils.LeaveSchoolWeek - 1f, 0f, _pluginInstance));
            _weeksSlider.ValueToString = f => Mathf.RoundToInt(f).ToString();
            _weeksSlider.ValueChanged.Subscribe(val => GetController().Week = Mathf.RoundToInt(val));

            e.AddControl(new MakerText("If the character is pregnant when added to the game, the pregnancy will continue from this point.", cat, _pluginInstance) { TextColor = hintColor });
        }
    }
}
