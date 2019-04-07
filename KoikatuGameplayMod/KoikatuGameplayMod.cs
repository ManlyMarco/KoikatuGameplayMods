using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using Harmony;
using Manager;
using UnityEngine;

namespace KoikatuGameplayMod
{
    [BepInProcess("Koikatu")]
    [BepInPlugin(GUID, "Koikatu Gameplay Tweaks and Improvements", Version)]
    public class KoikatuGameplayMod : BaseUnityPlugin
    {
        public const string GUID = "marco-gameplaymod";
        internal const string Version = "1.3";

        private const string HScene = "H Scene tweaks";

        [Category(HScene)]
        [DisplayName("!Allow force insert")]
        [Description("Can insert raw even if it's denied.\n\n" +
                     "To force insert - click on the blue insert button right after being denied, " +
                     "after coming outside, or after making her come multiple times. Other contitions might apply.")]
        public static ConfigWrapper<bool> ForceInsert { get; set; }

        [Category(HScene)]
        [DisplayName("!Force insert causes anger")]
        [Description("If you cum inside on or force insert too many times the girl will get angry with you.\n\n" +
                     "When enabled girl's expression changes during H (if forced).")]
        public static ConfigWrapper<bool> ForceInsertAnger { get; set; }

        [Category(HScene)]
        [DisplayName("Decrease girl's lewdness after H")]
        public static ConfigWrapper<bool> DecreaseLewd { get; set; }

        [Category(HScene)]
        [DisplayName("Disable vaginal insert for traps/men")]
        [Description("Only works if you use UncensorSelector to give a female card a penis but no vagina in maker.\n\n" +
                     "Changes take effect after game restart.")]
        public static ConfigWrapper<bool> DisableTrapVagInsert { get; set; }

        [DisplayName("Player's stats slowly decay overnight")]
        public static ConfigWrapper<bool> StatDecay { get; set; }

        [DisplayName("Girls' lewdness decays overnight. Will make lesbian and masturbation scenes less common.")]
        public static ConfigWrapper<bool> LewdDecay { get; set; }

        [DisplayName("Make experienced girls ask for condom")]
        [Description("If enabled, sometimes a girl will refuse raw insert on dangerous day until the second insert (once per day).\n" +
                     "If disabled the default game logic is used (girl will never refuse if you did raw 5 times or more in total.)")]
        public static ConfigWrapper<bool> ResetNoCondom { get; set; }

        [DisplayName("Fast travel (F3) time cost")]
        [Description("Value is in seconds.\nOne period has 500 seconds.")]
        [AcceptableValueRange(0, 100, false)]
        public static ConfigWrapper<int> FastTravelTimePenalty { get; set; }

        [DisplayName("Adjust preferred breast size question")]
        [Description("Lowers the breast size needed for 'Average' and 'Large' breast options when a girl asks you what size you prefer.\n\n" +
                     "Changes take effect after game restart.")]
        [AcceptableValueRange(0, 100, false)]
        public static ConfigWrapper<bool> AdjustBreastSizeQuestion { get; set; }

        private Game _gameMgr;
        private Scene _sceneMgr;

        public KoikatuGameplayMod()
        {
            ForceInsert = new ConfigWrapper<bool>("ForceInsert", this, true);
            ForceInsertAnger = new ConfigWrapper<bool>("ForceInsertAnger", this, true);
            DecreaseLewd = new ConfigWrapper<bool>("DecreaseLewd", this, false);
            DisableTrapVagInsert = new ConfigWrapper<bool>("DisableTrapVagInsert", this, true);

            ResetNoCondom = new ConfigWrapper<bool>("ResetNoCondom", this, true);

            FastTravelTimePenalty = new ConfigWrapper<int>("FastTravelTimePenalty", this, 50);
            StatDecay = new ConfigWrapper<bool>("StatDecay", this, true);
            LewdDecay = new ConfigWrapper<bool>("LewdDecay", this, false);
            AdjustBreastSizeQuestion = new ConfigWrapper<bool>("AdjustBreastSizeQuestion", this, true);

            var i = HarmonyInstance.Create(GUID);
            Hooks.ApplyHooks(i);
            if (DisableTrapVagInsert.Value)
                TrapNoVagInsertHooks.ApplyHooks(i);
        }

        // Start as false to prevent firing after loading
        private bool _inNightMenu, _firstNightMenu = true;

        private void OnNightStarted()
        {
            if (StatDecay.Value)
            {
                void LowerStat(ref int stat)
                {
                    stat -= Hooks.RandomGen.Next(0, 2);

                    if (stat < 0) stat = 0;
                }

                LowerStat(ref _gameMgr.Player.intellect);
                LowerStat(ref _gameMgr.Player.hentai);
                LowerStat(ref _gameMgr.Player.physical);
            }

            if (LewdDecay.Value)
            {
                foreach (var heroine in _gameMgr.HeroineList)
                    heroine.lewdness = Math.Max(0, heroine.lewdness - 50);
            }

            if (ResetNoCondom.Value)
            {
                foreach (var heroine in _gameMgr.HeroineList)
                {
                    if (heroine.parameter.attribute.bitch) continue;

                    // Lovers stop asking for condom at 3 or more, friends at 5 or more
                    if (heroine.isGirlfriend)
                        heroine.countNamaInsert = Mathf.Min(heroine.countNamaInsert, 2);
                    else
                        heroine.countNamaInsert = Mathf.Min(heroine.countNamaInsert, 4);
                }
            }
        }

        public void Start()
        {
            _gameMgr = Game.Instance;
            _sceneMgr = Scene.Instance;

            StartCoroutine(SlowUpdate());
        }

        private IEnumerator SlowUpdate()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.5f);

                if (!_gameMgr.saveData.isOpening && !_sceneMgr.IsNowLoading)
                {
                    if (_sceneMgr.NowSceneNames.Any(x => x.Equals("NightMenu", StringComparison.Ordinal)))
                    {
                        if (!_inNightMenu && !_firstNightMenu)
                            OnNightStarted();
                        _inNightMenu = true;
                        _firstNightMenu = false;
                    }
                    else
                    {
                        _inNightMenu = false;
                    }
                }
            }
        }
    }
}
