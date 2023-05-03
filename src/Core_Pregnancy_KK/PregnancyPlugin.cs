using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_Pregnancy
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(KKABMX_Core.GUID, KKABMX_Core.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class PregnancyPlugin : BaseUnityPlugin
    {
        public const string GUID = "KK_Pregnancy";
        public const string Version = "2.8.0";

        public static ConfigEntry<bool> ConceptionEnabled { get; private set; }
        public static ConfigEntry<float> FertilityOverride { get; private set; }
        public static ConfigEntry<bool> AnalConceptionEnabled { get; private set; }
        public static ConfigEntry<bool> ShowPregnancyIconEarly { get; private set; }
        public static ConfigEntry<StatusDisplayCondition> StatusDisplay { get; private set; }
        public static ConfigEntry<int> PregnancyProgressionSpeed { get; private set; }
        public static ConfigEntry<bool> HSceneMenstrIconOverride { get; private set; }

        public static ConfigEntry<bool> InflationEnable { get; private set; }
        public static ConfigEntry<int> InflationSpeed { get; private set; }
        public static ConfigEntry<bool> InflationOpenClothAtMax { get; private set; }
        public static ConfigEntry<int> InflationMaxCount { get; private set; }

        public static ConfigEntry<bool> LactationEnabled { get; private set; }
        public static ConfigEntry<int> LactationFillTime { get; private set; }
        public static ConfigEntry<bool> LactationForceMaxCapacity { get; private set; }

        internal static new ManualLogSource Logger { get; private set; }

        private void Start()
        {
            Logger = base.Logger;

            PregnancyProgressionSpeed = Config.Bind("General", "Pregnancy progression speed", 4,
                new ConfigDescription("How much faster does the in-game pregnancy progresses than the standard 40 weeks. " +
                                    "It also reduces the time characters leave school for after birth.\n\n" +
                                    "x1 is 40 weeks, x2 is 20 weeks, x4 is 10 weeks, etc.",
#if KK
                                    new AcceptableValueList<int>(1, 2, 4, 10)));
#elif KKS
                                    new AcceptableValueList<int>(1, 2, 4, 7, 14, 20))); // Multiples of 7 to match daily updates
#endif

            ConceptionEnabled = Config.Bind("General", "Enable conception", true,
                "Allows characters to get pregnant from vaginal sex. Doesn't affect already pregnant characters.");

            FertilityOverride = Config.Bind("General", "Minimum fertility level", 0f,
                new ConfigDescription("If a character has a lower fertility level than this set, this level will be used instead. \n\n" +
                    "0 - The value saved in the character card is used (30% by default)\n" +
                    "30%, 50%, 75%, 100% - If the character card's saved value is lower, it will be raised to this level in HScenes.",
                new AcceptableValueList<float>(0f, 0.3f, 0.5f, 0.75f, 1f)));

            AnalConceptionEnabled = Config.Bind("General", "Enable anal conception", false,
                "Allows characters to get pregnant from anal sex. Doesn't affect already pregnant characters.");

            ShowPregnancyIconEarly = Config.Bind("General", "Show pregnancy icon early", false,
                "By default pregnancy status icon in class roster is shown after a few days or weeks (the character had a chance to do the test or noticed something is wrong).\n" +
                "Turning this on will always make the icon show up at the end of the current day.");

            StatusDisplay = Config.Bind("General", "When to reveal Risky/Safe/Pregnant status", StatusDisplayCondition.Normal,
                                                      "Always: You can immediately see the status of all characters.\n" +
                                                      "Normal: You can only see status of characters you are reasonably close with.\n" +
                                                      "Only Girlfriend: You can only see status of your girlfriends (other requirements might also apply).\n" +
                                                      "Never: The status is always shown as Unknown.");

            HSceneMenstrIconOverride = Config.Bind("General", "Use custom safe/risky icons in H Scenes", true,
                "Replaces the standard safe/risky indicators with custom indicators that can also show pregnancy and unknown status. " +
                "If the status is unknown you will have to listen for the voice cues instead.\nChanges take effect after game restart.");

            InflationEnable = Config.Bind("Inflation", "Enable inflation", true, "Turn on the inflation effect.");

            InflationSpeed = Config.Bind("Inflation", "Inflation speed modifier", 1,
                new ConfigDescription("How quickly the belly will inflate/deflate compared to normal (1x, 2x, 3x as fast).", new AcceptableValueList<int>(1, 2, 3)));

            InflationOpenClothAtMax = Config.Bind("Inflation", "Open clothes at max inflation", true,
                "If clothes are fully on, open them when inflation reaches the max value (they 'burst' open).");

            InflationMaxCount = Config.Bind("Inflation", "Cum count until full", 8,
                new ConfigDescription("How many times you have to let out inside to reach the maximum belly size.", new AcceptableValueRange<int>(2, 15)));

            LactationEnabled = Config.Bind("Lactation", "Enable lactation", true,
                "Enable the lactation effect. For the effect to work the character has to be pregnant, or the override setting has to be enabled.");

            LactationFillTime = Config.Bind("Lactation", "Time to fully refill", 5,
                new ConfigDescription("How many minutes it takes to fully refill the milk. 0 is always fully refilled.", new AcceptableValueRange<int>(0, 10)));

            LactationForceMaxCapacity = Config.Bind("Lactation", "Force max milk capacity", false,
                "If enabled, all characters will lactate and have full capacity. If off, capacity depends on the pregnancy progress.");

            CharacterApi.RegisterExtraBehaviour<PregnancyCharaController>(GUID);
            GameAPI.RegisterExtraBehaviour<PregnancyGameController>(GUID);

            var hi = new Harmony(GUID);
            Hooks.InitHooks(hi);
            PregnancyGui.Init(hi, this);

            LoadFeatures(hi);

            if (TimelineCompatibility.IsTimelineAvailable())
            {
                TimelineCompatibility.AddCharaFunctionInterpolable<int, PregnancyCharaController>(
                    GUID,
                    "week",
                    "Pregnancy week",
                    (oci, parameter, leftValue, rightValue, factor) => parameter.Data.Week = Mathf.RoundToInt(Mathf.LerpUnclamped(leftValue, rightValue, factor)),
                    null,
                    (oci, parameter) => parameter.Data.Week
                    );
            }
        }

        private void LoadFeatures(Harmony hi)
        {
            var featureT = typeof(IFeature);
            var types = typeof(PregnancyPlugin).Assembly.GetTypes().Where(x => featureT.IsAssignableFrom(x) && x.IsClass);

            var successful = new List<string>();
            foreach (var type in types)
            {
                var feature = (IFeature)Activator.CreateInstance(type);
                if (feature.Install(hi, Config))
                    successful.Add(type.Name);
            }

            Logger.LogInfo("Loaded features: " + string.Join(", ", successful.ToArray()));
        }

        internal static PregnancyCharaController GetEffectController(SaveData.Heroine heroine)
        {
            return heroine?.chaCtrl != null ? heroine.chaCtrl.GetComponent<PregnancyCharaController>() : null;
        }
        
        public enum StatusDisplayCondition
        {
            Always,
            Normal,
            OnlyGirlfriend,
            Never
        }
    }
}
