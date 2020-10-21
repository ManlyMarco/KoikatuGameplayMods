using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;

namespace KK_Pregnancy
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(KKABMX_Core.GUID, "4.1")]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class PregnancyPlugin : BaseUnityPlugin
    {
        public const string GUID = "KK_Pregnancy";
        public const string Version = "2.2";

        public static ConfigEntry<bool> ConceptionEnabled { get; private set; }
        public static ConfigEntry<float> FertilityOverride { get; private set; }
        public static ConfigEntry<bool> AnalConceptionEnabled { get; private set; }
        public static ConfigEntry<bool> ShowPregnancyIconEarly { get; private set; }
        public static ConfigEntry<int> PregnancyProgressionSpeed { get; private set; }
        public static ConfigEntry<bool> HSceneMenstrIconOverride { get; private set; }

        public static ConfigEntry<bool> InflationEnable { get; private set; }
        public static ConfigEntry<int> InflationSpeed { get; private set; }
        public static ConfigEntry<bool> InflationOpenClothAtMax { get; private set; }
        public static ConfigEntry<int> InflationMaxCount { get; private set; }
        //public static ConfigEntry<int> InflationDrainSpeed { get; private set; }

        internal static new ManualLogSource Logger { get; private set; }

        private void Start()
        {
            Logger = base.Logger;

            PregnancyProgressionSpeed = Config.Bind("General", "Pregnancy progression speed", 4,
                new ConfigDescription("How much faster does the in-game pregnancy progresses than the standard 40 weeks. " +
                                      "It also reduces the time characters leave school for after birth.\n\n" +
                                      "x1 is 40 weeks, x2 is 20 weeks, x4 is 10 weeks, x10 is 4 weeks.",
                    new AcceptableValueList<int>(1, 2, 4, 10)));

            ConceptionEnabled = Config.Bind("General", "Enable conception", true,
                "Allows characters to get pregnant from vaginal sex. Doesn't affect already pregnant characters.");

            FertilityOverride = Config.Bind<float>("General", "Fertility override", 0.3f,
                new ConfigDescription("Overrides default character fertility values with the one selected. \n\n" +
                    "30%, 50%, 75%, 100% chance to get pregnant after HScene",
                new AcceptableValueList<float>(0.3f, 0.5f, 0.75f, 1f)));

            AnalConceptionEnabled = Config.Bind("General", "Enable anal conception", false,
                "Allows characters to get pregnant from anal sex. Doesn't affect already pregnant characters.");

            ShowPregnancyIconEarly = Config.Bind("General", "Show pregnancy icon early", false,
                "By default pregnancy status icon in class roster is shown after a few days or weeks (the character had a chance to do the test or noticed something is wrong).\n" +
                "Turning this on will always make the icon show up at the end of the current day.");

            HSceneMenstrIconOverride = Config.Bind("General", "Use custom safe/risky icons in H Scenes", true,
                "Replaces the standard safe/risky indicators with custom indicators that can also show pregnancy and unknown status. " +
                "If the status is unknown you will have to listen for the voice cues instead.\nChanges take effect after game restart.");

            InflationEnable = Config.Bind("Inflation", "Enable inflation", true, "Turn on the inflation effect.");

            InflationSpeed = Config.Bind("Inflation", "Inflation speed", 1, 
                new ConfigDescription("How quickly the belly will inflate/deflate. \n\n1x, 2x, 3x",
                new AcceptableValueList<int>(1, 2, 3)));

            InflationOpenClothAtMax = Config.Bind("Inflation", "Open clothes at max inflation", true, "If clothes are fully on, open them when inflation reaches the max value (they 'burst' open).");

            InflationMaxCount = Config.Bind("Inflation", "Cum count until full", 8, new ConfigDescription("How many times you have to let out inside to reach the maximum belly size.",
                new AcceptableValueRange<int>(2, 15)));

            CharacterApi.RegisterExtraBehaviour<PregnancyCharaController>(GUID);
            GameAPI.RegisterExtraBehaviour<PregnancyGameController>(GUID);

            var hi = new Harmony(GUID);
            Hooks.InitHooks(hi);
            PregnancyGui.Init(hi, this);
        }
    }
}
