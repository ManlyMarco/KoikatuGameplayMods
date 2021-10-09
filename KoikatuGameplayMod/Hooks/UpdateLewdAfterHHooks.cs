using System;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UnityEngine;

namespace KoikatuGameplayMod
{
    internal class UpdateLewdAfterHHooks : IFeature
    {
        private static ConfigEntry<bool> _changeLewdAfterH;

        public bool Install(Harmony instance, ConfigFile config)
        {
            if (KoikatuAPI.IsVR()) return false;

            _changeLewdAfterH = config.Bind(KoikatuGameplayMod.ConfCatHScene, "Change lewdness after H", false,
                "Decreases heroine's H bar after an H scene if satisfied, increases the bar if not.");

            GameAPI.EndH += UpdateLewdAfterH;

            return true;
        }

        private static void UpdateLewdAfterH(object sender, EventArgs eventArgs)
        {
            if (!_changeLewdAfterH.Value) return;

            var hSprite = GameObject.FindObjectOfType<HFlag>();

            var heroine = hSprite.GetLeadingHeroine();
            if (heroine == null) return;

            var orgCount = hSprite.GetOrgCount();
            if (orgCount == 0) orgCount = -hSprite.GetInsideAndOutsideCount(); // Increase lewdness if girl didn't org but guy did
            heroine.lewdness = Mathf.Clamp(heroine.lewdness - orgCount * 40, 0, 100);
        }
    }
}