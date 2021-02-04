using System;
using System.Linq;
using ActionGame;
using HarmonyLib;
using KKAPI.MainGame;
using KKAPI.Utilities;
using Manager;
using StrayTech;
using UnityEngine;

namespace KK_LewdCrestX
{
    public sealed class LewdCrestXGameController : GameCustomFunctionController
    {
        internal static Type SkinEffectsType;
        private HsceneHeroineInfo[] _hSceneHeroines;
        private HSceneProc _hSceneProc;

        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            _hSceneProc = proc;
            _hSceneHeroines = proc.flags.lstHeroine.Select(x => new HsceneHeroineInfo(x)).ToArray();

            foreach (var heroine in _hSceneHeroines)
            {
                if (heroine.CrestType == CrestType.regrowth)
                {
                    var prop = heroine.GetRegenProp();
                    if (prop != null && !prop.Value)
                    {
                        prop.Value = true;
                        heroine.NeedsRegenRestored = true;
                    }
                }
            }
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            foreach (var heroine in _hSceneHeroines)
            {
                if (heroine.CrestType == CrestType.violove)
                {
                    var totalTime = (int) heroine.TotalRoughTime;
                    var h = heroine.Heroine;
                    h.lewdness = Mathf.Min(100, h.lewdness + totalTime / 10);
                    h.favor = Mathf.Min(100, h.favor + totalTime / 20);
                    h.intimacy = Mathf.Min(100, h.favor + totalTime / 30);
                }

                if (heroine.NeedsRegenRestored)
                    heroine.GetRegenProp().Value = false;
            }

            _hSceneProc = null;
            _hSceneHeroines = null;
        }

        private void Update()
        {
            if (_hSceneProc != null && _hSceneProc.flags.speed > 0.7f)
            {
                var id = _hSceneProc.flags.GetLeadingHeroineId();
                var heroineInfo = _hSceneHeroines[id];
                heroineInfo.TotalRoughTime += Time.deltaTime;

                if (heroineInfo.CrestType == CrestType.suffer)
                    _hSceneProc.flags.gaugeFemale += (_hSceneProc.flags.speed - 0.7f) * 3 * Time.deltaTime * 4;
            }
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            foreach (var heroine in Game.Instance.HeroineList)
            {
                if (heroine != null && heroine.GetCurrentCrest() == CrestType.restore && !heroine.isVirgin)
                {
                    LewdCrestXPlugin.Logger.LogDebug("Resetting heroine to virgin because of restore crest: " + heroine.charFile?.parameter?.fullname);
                    heroine.isVirgin = true;
                    heroine.hCount = 0;
                }
            }
        }

        private sealed class HsceneHeroineInfo
        {
            public readonly LewdCrestXController Controller;
            public readonly CrestType CrestType;
            public readonly SaveData.Heroine Heroine;
            public bool NeedsRegenRestored;
            public float TotalRoughTime;

            public HsceneHeroineInfo(SaveData.Heroine heroine)
            {
                Heroine = heroine;
                if (heroine != null) Controller = heroine.GetCrestController();
                if (Controller != null) CrestType = Controller.CurrentCrest;
            }

            public Traverse<bool> GetRegenProp()
            {
                if (SkinEffectsType == null) return null;
                if (Controller == null) return null;
                var seCtrl = Controller.GetComponent(SkinEffectsType);
                if (seCtrl == null)
                {
                    LewdCrestXPlugin.Logger.LogWarning("SkinEffectsController was not found on " + Controller.FullPath());
                    return null;
                }

                var prop = Traverse.Create(seCtrl).Property<bool>("HymenRegen");
                return prop;
            }
        }
    }
}