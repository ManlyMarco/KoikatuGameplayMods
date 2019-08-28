using Harmony;
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
        private static MakerToggle _gameplayToggle;
        private static MakerSlider _fertilityToggle;
        private static MakerSlider _weeksSlider;

        private static PregnancyPlugin _pluginInstance;

        internal static void Init(HarmonyInstance hi, PregnancyPlugin instance)
        {
            _pluginInstance = instance;

            if (StudioAPI.InsideStudio)
            {
                RegisterStudioControls();
            }
            else
            {
                MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
                MakerAPI.MakerExiting += MakerAPI_MakerExiting;
                HeartIcons.Init(hi);
            }
        }

        private static void MakerAPI_MakerExiting(object sender, System.EventArgs e)
        {
            _gameplayToggle = null;
            _fertilityToggle = null;
            _weeksSlider = null;
        }

        private static void RegisterStudioControls()
        {
            var cat = StudioAPI.GetOrCreateCurrentStateCategory(null);
            cat.AddControl(new CurrentStateCategorySlider("Pregnancy",
                c => c.charInfo?.GetComponent<PregnancyCharaController>()?.Week ?? 0, 0, 40)).Value.Subscribe(
                f => { foreach (var ctrl in StudioAPI.GetSelectedControllers<PregnancyCharaController>()) ctrl.Week = Mathf.RoundToInt(f); });
        }

        internal static void UpdateMakerInterface(PregnancyCharaController controller)
        {
            if (MakerAPI.InsideMaker && _gameplayToggle != null)
            {
                _gameplayToggle.Value = controller.GameplayEnabled;
                _fertilityToggle.Value = controller.Fertility;
                _weeksSlider.Value = controller.Week;
            }
        }

        private static PregnancyCharaController GetMakerController()
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
            _gameplayToggle.ValueChanged.Subscribe(val => GetMakerController().GameplayEnabled = val);

            e.AddControl(new MakerText("If off, the character can't get pregnant and current pregnancy will stop progressing.", cat, _pluginInstance) { TextColor = hintColor });

            _fertilityToggle = e.AddControl(new MakerSlider(cat, "Fertility", 0f, 1f, PregnancyDataUtils.DefaultFertility, _pluginInstance));
            _fertilityToggle.ValueChanged.Subscribe(val => GetMakerController().Fertility = val);

            e.AddControl(new MakerText("How likely this character is to get pregnant.", cat, _pluginInstance) { TextColor = hintColor });

            _weeksSlider = e.AddControl(new MakerSlider(cat, "Week of pregnancy", 0f, PregnancyDataUtils.LeaveSchoolWeek - 1f, 0f, _pluginInstance));
            _weeksSlider.ValueToString = f => Mathf.RoundToInt(f).ToString();
            _weeksSlider.StringToValue = s => int.Parse(s);
            _weeksSlider.ValueChanged.Subscribe(val => GetMakerController().Week = Mathf.RoundToInt(val));

            e.AddControl(new MakerText("If the character is pregnant when added to the game, the pregnancy will continue from this point.", cat, _pluginInstance) { TextColor = hintColor });
        }
    }
}
