using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;

namespace KK_Pregnancy
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(KKABMX_Core.GUID, "3.2.3")]
    [BepInDependency(KoikatuAPI.GUID, "1.4")]
    public partial class PregnancyPlugin : BaseUnityPlugin
    {
        public const string GUID = "KK_Pregnancy";
        public const string Version = "1.2";

        public static ConfigEntry<bool> ConceptionEnabled { get; private set; }
        public static ConfigEntry<bool> AnalConceptionEnabled { get; private set; }
        public static ConfigEntry<bool> ShowPregnancyIconEarly { get; private set; }
        public static ConfigEntry<int> PregnancyProgressionSpeed { get; private set; }
        public static ConfigEntry<bool> HideHSceneMenstrIcon { get; private set; }

        private void Start()
        {
            PregnancyProgressionSpeed = Config.Bind("General", "Pregnancy progression speed", 4,
                new ConfigDescription("How much faster does the in-game pregnancy progresses than the standard 40 weeks. " +
                                      "It also reduces the time characters leave school for after birth.\n\n" +
                                      "x1 is 40 weeks, x2 is 20 weeks, x4 is 10 weeks, x10 is 4 weeks.",
                    new AcceptableValueList<int>(1, 2, 4, 10)));

            ConceptionEnabled = Config.Bind("General", "Enable conception", true,
                "Allows characters to get pregnant from vaginal sex. Doesn't affect already pregnant characters.");

            AnalConceptionEnabled = Config.Bind("General", "Enable anal conception", false,
                "Allows characters to get pregnant from anal sex. Doesn't affect already pregnant characters.");

            ShowPregnancyIconEarly = Config.Bind("General", "Show pregnancy icon early", false,
                "By default pregnancy status icon in class roster is shown after a few days (the girl had a chance to do the test). " +
                "Turning this on will make the icon show up at the end of the current day.");

            HideHSceneMenstrIcon = Config.Bind("General", "Allow hiding safe/risky icon in H Scenes", true,
                "Replaces the safe/risky indicator with a question mark if the character didn't tell you their schedule yet. You have to listen for the voice cues instead.\n" +
                "Changes take effect after game restart.");

            CharacterApi.RegisterExtraBehaviour<PregnancyCharaController>(GUID);
            GameAPI.RegisterExtraBehaviour<PregnancyGameController>(GUID);

            var hi = new Harmony(GUID);
            Hooks.InitHooks(hi);
            PregnancyGui.Init(hi, this);
        }
    }
}
