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
    [BepInPlugin("KoikatuGameplayMod", "Koikatu Gameplay Tweaks and Improvements", "1.1")]
    public class KoikatuGameplayMod : BaseUnityPlugin
    {
        [DisplayName("Allow player stats to slowly decay")]
        public static ConfigWrapper<bool> StatDecay { get; set; }

        [DisplayName("Fast travel (F3) time cost")]
        [Description("Value is in seconds.\nOne period has 500 seconds.")]
        [AcceptableValueRange(0, 100, false)]
        public static ConfigWrapper<int> FastTravelTimePenalty { get; set; }

        private Game _gameMgr;
        private Scene _sceneMgr;

        public KoikatuGameplayMod()
        {
            FastTravelTimePenalty = new ConfigWrapper<int>("FastTravelTimePenalty", this, 50);
            StatDecay = new ConfigWrapper<bool>("StatDecay", this, true);

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

            foreach (var heroine in _gameMgr.HeroineList)
            {
                heroine.lewdness = Math.Max(0, heroine.lewdness - 50);
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
            _gameMgr = Manager.Game.Instance;
            _sceneMgr = Manager.Scene.Instance;

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
