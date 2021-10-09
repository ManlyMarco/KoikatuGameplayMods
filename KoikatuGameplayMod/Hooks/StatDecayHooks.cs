using System;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using Scene = Manager.Scene;

namespace KoikatuGameplayMod
{
    internal class StatDecayHooks : IFeature
    {
        private static ConfigEntry<bool> _statDecay;
        private static ConfigEntry<bool> _changeLewdDaily;

        // Start as false to prevent firing after loading
        private bool _inNightMenu, _firstNightMenu = true;

        public bool Install(Harmony instance, ConfigFile config)
        {
            if (KoikatuAPI.IsVR()) return false;

            _statDecay = config.Bind(KoikatuGameplayMod.ConfCatMainGame, "Player stats slowly decay overnight", true,
                "Player's stats slowly decrease every day to keep the training points relevant.");
            _changeLewdDaily = config.Bind(KoikatuGameplayMod.ConfCatMainGame, "Change lewdness overnight", false,
                "H bar of all heroines either increases or decreases overnight depending on their status.");

            SceneManager.sceneLoaded += (arg0, mode) =>
            {
                if (arg0.name != "MyRoom" || Singleton<Scene>.Instance.LoadSceneName == "H")
                {
                    _inNightMenu = false;
                }
                else
                {
                    if (!_inNightMenu && !_firstNightMenu)
                    {
                        try { OnNightStarted(); }
                        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
                    }

                    _inNightMenu = true;
                    _firstNightMenu = false;
                }
            };

            return true;
        }

        private static void OnNightStarted()
        {
            var gameMgr = Game.Instance;
            if (_statDecay.Value)
            {
                void LowerStat(ref int stat)
                {
                    stat -= Random.Range(0, 2);

                    if (stat < 0) stat = 0;
                }

                LowerStat(ref gameMgr.Player.intellect);
                LowerStat(ref gameMgr.Player.hentai);
                LowerStat(ref gameMgr.Player.physical);
            }

            if (_changeLewdDaily.Value)
            {
                foreach (var heroine in gameMgr.HeroineList)
                {
                    var totalChange = (int)(20 * (Random.value - 0.3f));
                    if (heroine.favor > 10) totalChange += 5 + heroine.favor / 8;
                    if (heroine.parameter.attribute.bitch || heroine.parameter.attribute.likeGirls) totalChange += 10;
                    if (heroine.parameter.attribute.friendly || heroine.parameter.attribute.undo) totalChange += 5;
                    if (heroine.parameter.attribute.kireizuki || heroine.parameter.attribute.majime) totalChange -= 5;
                    // club member
                    if (heroine.isStaff) totalChange += 10;

                    heroine.lewdness = Mathf.Clamp(heroine.lewdness + totalChange, 0, 100);

                    // self relief
                    if (!heroine.isGirlfriend && heroine.lewdness > 85 && Random.value > 0.9f)
                        heroine.lewdness = 0;
                }
            }
        }
    }
}