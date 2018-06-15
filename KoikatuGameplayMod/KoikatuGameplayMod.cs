using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using Manager;
using UnityEngine;

namespace KoikatuGameplayMod
{
    [BepInPlugin("KoikatuGameplayMod", "Koikatu Gameplay Tweaks and Improvements", "1.0")]
    [BepInDependency("marco-gameplaymod", BepInDependency.DependencyFlags.SoftDependency)]
    public class KoikatuGameplayMod : BaseUnityPlugin
    {
        [DisplayName("Allow stats to decay")]
        public ConfigWrapper<bool> StatDecay { get; }

        private Game _gameMgr;
        private Scene _sceneMgr;

        public KoikatuGameplayMod()
        {
            StatDecay = new ConfigWrapper<bool>("StatDecay", this, true);

            Hooks.ApplyHooks();
        }

        // Start as false to prevent firing after loading
        private bool _inNightMenu, _firstNightMenu = true;

        private void Update()
        {
            if (!_gameMgr.saveData.isOpening && StatDecay.Value && !_sceneMgr.IsNowLoading)
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

        private void OnNightStarted()
        {
            LowerStat(ref _gameMgr.Player.intellect);
            LowerStat(ref _gameMgr.Player.hentai);
            LowerStat(ref _gameMgr.Player.physical);
        }

        private void LowerStat(ref int stat)
        {
            if (stat > 30)
                stat = stat - (int)Math.Log10(_gameMgr.Player.intellect);

            stat -= Hooks.RandomGen.Next(0, 3);

            if (stat < 0) stat = 0;
        }

        private void Start()
        {
            _gameMgr = Manager.Game.Instance;
            _sceneMgr = Manager.Scene.Instance;
        }

        /*
        private void OnGUI()
        {

        }*/
    }
}
