using System.ComponentModel;
using BepInEx;
using ExtensibleSaveFormat;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UniRx;
using UnityEngine;

namespace KK_Pregnancy
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(KKABMX_Core.GUID)]
    [BepInDependency(KoikatuAPI.GUID)]
    public partial class PregnancyPlugin : BaseUnityPlugin
    {
        public const string GUID = "KK_Pregnancy";

        public static readonly float DefaultFertility = 0.3f;

        /// <summary>
        /// Week at which pregnancy reaches max level and the girl leaves school
        /// </summary>
        public static readonly int LeaveSchoolWeek = 41;

        /// <summary>
        /// Week at which pregnancy ends and the girl returns to school
        /// </summary>
        public static readonly int ReturnToSchoolWeek = LeaveSchoolWeek + 7;

        internal const string Version = "0.5";
        private static MakerSlider _fertilityToggle;
        private static MakerToggle _gameplayToggle;
        private static MakerSlider _weeksSlider;

        [DisplayName("Enable conception")]
        [Description("If disabled no new characters will be able to get pregnant. Doesn't affect already pregnant characters.")]
        public static ConfigWrapper<bool> ConceptionEnabled { get; private set; }

        [DisplayName("Pregnancy progression speed")]
        [Description("How much faster does the in-game pregnancy progresses than the standard 40 weeks. " +
                     "It also reduces the time characters leave school for after birth.\n\n" +
                     "x1 is 40 weeks, x2 is 20 weeks, x4 is 10 weeks, x10 is 4 weeks.")]
        [AcceptableValueList(new object[] { 1, 2, 4, 10 })]
        public static ConfigWrapper<int> PregnancyProgressionSpeed { get; private set; }

        private void Start()
        {
            Hooks.InitHooks();

            PregnancyProgressionSpeed = new ConfigWrapper<int>(nameof(PregnancyProgressionSpeed), this, 4);
            ConceptionEnabled = new ConfigWrapper<bool>(nameof(ConceptionEnabled), this, true);

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;

            CharacterApi.RegisterExtraBehaviour<PregnancyCharaController>(GUID);
            GameAPI.RegisterExtraBehaviour<PregnancyGameController>(GUID);

            // todo add when slider is implemented
            //StudioAPI.CreateCurrentStateCategory(new CurrentStateCategory("Pregnancy", new CurrentStateCategorySubItemBase[]{new CurrentStateCategorySlider()}));
        }

        private static PregnancyCharaController GetController()
        {
            return MakerAPI.GetCharacterControl().GetComponent<PregnancyCharaController>();
        }

        private void MakerAPI_MakerBaseLoaded(object sender, RegisterCustomControlsEvent e)
        {
            if (MakerAPI.GetMakerSex() == 0) return;

            var cat = MakerConstants.Parameter.Character;

            _gameplayToggle = e.AddControl(new MakerToggle(cat, "Enable pregnancy progression", true, this));
            _gameplayToggle.ValueChanged.Subscribe(val => GetController().GameplayEnabled = val);

            e.AddControl(new MakerText("If off, the character can't get pregnant and current pregnancy will stop progressing.", cat, this) { TextColor = Color.gray });

            _fertilityToggle = e.AddControl(new MakerSlider(cat, "Fertility", 0f, 1f, DefaultFertility, this));
            _fertilityToggle.ValueChanged.Subscribe(val => GetController().Fertility = val);

            e.AddControl(new MakerText("How likely this character is to get pregnant.", cat, this) { TextColor = Color.gray });

            _weeksSlider = e.AddControl(new MakerSlider(cat, "Week of pregnancy", 0f, LeaveSchoolWeek - 1f, 0f, this));
            _weeksSlider.ValueToString = f => Mathf.RoundToInt(f).ToString();
            _weeksSlider.ValueChanged.Subscribe(val => GetController().Week = Mathf.RoundToInt(val));

            e.AddControl(new MakerText("If the character is pregnant when added to the game, the pregnancy will continue from this point.", cat, this) { TextColor = Color.gray });
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

        public static void ParseData(PluginData data, out int week, out bool gameplayEnabled, out float fertility)
        {
            week = 0;
            gameplayEnabled = true;
            fertility = DefaultFertility;

            if (data?.data == null) return;

            if (data.data.TryGetValue("Week", out var value) && value is int w) week = w;
            if (data.data.TryGetValue("GameplayEnabled", out var value2) && value2 is bool g) gameplayEnabled = g;
            if (data.data.TryGetValue("Fertility", out var value3) && value3 is float f) fertility = f;
        }

        public static PluginData WriteData(int week, bool gameplayEnabled, float fertility)
        {
            if (week <= 0 && gameplayEnabled && !Mathf.Approximately(fertility, DefaultFertility)) return null;

            var data = new PluginData();
            data.data["Week"] = week;
            data.data["GameplayEnabled"] = gameplayEnabled;
            data.data["Fertility"] = fertility;
            return data;
        }
    }
}
