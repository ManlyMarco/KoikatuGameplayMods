using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using Manager;
using UnityEngine;

namespace KoikatuGameplayMod
{
    internal class ResetNoCondomHooks : IFeature
    {
        private static ConfigEntry<bool> _resetNoCondom;

        public bool Install(Harmony instance, ConfigFile config)
        {
            if (KoikatuAPI.IsVR()) return false;

            _resetNoCondom = config.Bind(KoikatuGameplayMod.ConfCatHScene, "Make experienced girls ask for condom", true,
                "If enabled, sometimes a heroine will refuse raw insert on dangerous day until the second insert (once per day).\nIf disabled the default game logic is used (girl will never refuse if you did raw 5 times or more in total.)");

            GameAPI.DayChange += OnDayChanged;

            return true;
        }

        private static void OnDayChanged(object sender, GameAPI.DayChangeEventArgs e)
        {
            if (_resetNoCondom.Value)
            {
                foreach (var heroine in KoikatuGameplayMod.GetHeroineList())
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
    }
}