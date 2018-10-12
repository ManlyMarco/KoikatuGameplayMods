using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using Manager;
using UnityEngine;

namespace KoikatuGameplayMod
{
    [BepInProcess("Koikatu")]
    [BepInPlugin("marco-gameplaymod", "Koikatu Gameplay Tweaks and Improvements", "1.1")]
    public class KoikatuGameplayMod : BaseUnityPlugin
    {
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

        [DisplayName("Player's stats slowly decay overnight")]
        public static ConfigWrapper<bool> StatDecay { get; set; }

        [DisplayName("Girls' lewdness decays overnight")]
        public static ConfigWrapper<bool> LewdDecay { get; set; }

        [DisplayName("Fast travel (F3) time cost")]
        [Description("Value is in seconds.\nOne period has 500 seconds.")]
        [AcceptableValueRange(0, 100, false)]
        public static ConfigWrapper<int> FastTravelTimePenalty { get; set; }

        private Game _gameMgr;
        private Scene _sceneMgr;

        public KoikatuGameplayMod()
        {
            ForceInsert = new ConfigWrapper<bool>("ForceInsertAnger", this, true);
            ForceInsertAnger = new ConfigWrapper<bool>("ForceInsertAnger", this, true);
            DecreaseLewd = new ConfigWrapper<bool>("DecreaseLewd", this, true);

            FastTravelTimePenalty = new ConfigWrapper<int>("FastTravelTimePenalty", this, 50);
            StatDecay = new ConfigWrapper<bool>("StatDecay", this, true);
            LewdDecay = new ConfigWrapper<bool>("LewdDecay", this, false);

            Hooks.ApplyHooks();
        }

        // Start as false to prevent firing after loading
        private bool _inNightMenu, _firstNightMenu = true;

        private void OnNightStarted()
        {
            if (StatDecay.Value)
            {
                LowerStat(ref _gameMgr.Player.intellect);
                LowerStat(ref _gameMgr.Player.hentai);
                LowerStat(ref _gameMgr.Player.physical);
            }

            if (LewdDecay.Value)
            {
                foreach (var heroine in _gameMgr.HeroineList)
                {
                    heroine.lewdness = Math.Max(0, heroine.lewdness - 50);
                }
            }
        }

        private void LowerStat(ref int stat)
        {
            if (stat > 40)
                stat = stat - (int)(Math.Log10(_gameMgr.Player.intellect) * 1.33);

            stat -= Hooks.RandomGen.Next(0, 3);

            if (stat < 0) stat = 0;
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

        /*

        private void Update()
        {

        }
        private void OnGUI()
        {

        }*/
    }
}
