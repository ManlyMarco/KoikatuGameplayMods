using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using HarmonyLib;
using KK_Pregnancy;
using KKAPI.MainGame;
using KKAPI.Utilities;
using Manager;
using StrayTech;
using UnityEngine;

namespace KK_LewdCrestX
{
    public sealed class LewdCrestXGameController : GameCustomFunctionController
    {
        private HsceneHeroineInfo[] _hSceneHeroines;
        private HSceneProc _hSceneProc;

        private static readonly HashSet<SaveData.Heroine> _tempPreggers = new HashSet<SaveData.Heroine>();
        private List<LewdCrestXController> _existingControllers;

        public static void ApplyTempPreggers(SaveData.Heroine heroine)
        {
            if (_tempPreggers.Add(heroine))
                LewdCrestXPlugin.Logger.LogInfo("Triggering temporary pregnancy because of breedgasm crest: " + heroine.charFile?.parameter?.fullname);
        }

        private static void ClearTempPreggers()
        {
            foreach (var heroine in _tempPreggers)
            {
                var pregCtrl = heroine?.GetCrestController()?.GetComponent<PregnancyCharaController>();
                if (pregCtrl != null)
                {
                    pregCtrl.Data.Week = 0;
                    pregCtrl.SaveData();
                }
            }
            _tempPreggers.Clear();
        }

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
                    var totalTime = (int)heroine.TotalRoughTime;
                    var h = heroine.Heroine;
                    h.lewdness = Mathf.Min(100, h.lewdness + totalTime / 10);
                    h.favor = Mathf.Min(100, h.favor + totalTime / 20);
                    h.intimacy = Mathf.Min(100, h.intimacy + totalTime / 30);
                }

                if (heroine.NeedsRegenRestored)
                    heroine.GetRegenProp().Value = false;
            }

            _hSceneProc = null;
            _hSceneHeroines = null;
        }

        private void Update()
        {
            if (_hSceneHeroines != null)
            {
                var speed = _hSceneProc.flags.IsSonyu() && _hSceneProc.flags.speedCalc > 0.7f
                    ? (_hSceneProc.flags.speedCalc - 0.7f) * 3.3f
                    : (_hSceneProc.flags.speedItem > 1.4f ? 0.33f : 0);
                if (speed > 0)
                {
                    var id = _hSceneProc.flags.GetLeadingHeroineId();
                    var heroineInfo = _hSceneHeroines[id];
                    heroineInfo.TotalRoughTime += Time.deltaTime;

                    if (heroineInfo.CrestType == CrestType.suffer && !_hSceneProc.flags.lockGugeFemale)
                        _hSceneProc.flags.gaugeFemale += speed * Time.deltaTime * 2;
                }
            }
            else if (_existingControllers != null)
            {
                for (var i = 0; i < _existingControllers.Count; i++)
                {
                    var controller = _existingControllers[i];
                    if (controller.CurrentCrest == CrestType.mantraction)
                    {
                        var actScene = Game.Instance.actScene;
                        if (actScene == null) return;
                        var player = actScene.Player;
                        if (player == null) continue;

                        if (player.chaser == null)
                        {
                            var npc = controller.Heroine.GetNPC();
                            if (player.mapNo == npc.mapNo)
                            {
                                LewdCrestXPlugin.Logger.LogInfo("Chasing player because of mantraction crest: " + controller.Heroine.charFile?.parameter?.fullname);
                                player.ChaserSet(npc);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnPeriodChange(Cycle.Type period)
        {
            StopAllCoroutines();
            StartCoroutine(OnPeriodChangeCo());
        }

        private IEnumerator OnPeriodChangeCo()
        {
            yield return null;
            yield return new WaitWhile(() => Scene.Instance.IsNowLoadingFade);

            // Apply the effect now to get a delay
            foreach (var heroine in _tempPreggers)
            {
                var pregCtrl = heroine?.GetCrestController()?.GetComponent<PregnancyCharaController>();
                if (pregCtrl != null)
                {
                    pregCtrl.Data.Week = PregnancyData.LeaveSchoolWeek;
                    pregCtrl.SaveData();
                }
            }

            _existingControllers = Game.Instance.HeroineList.Select(x => x.GetCrestController()).Where(x => x != null).ToList();

            var actCtrl = Game.Instance.actScene?.actCtrl;
            if (actCtrl != null)
            {
                foreach (var ctrl in _existingControllers)
                {
                    var heroine = ctrl.Heroine;
                    if (heroine == null) continue;
                    switch (ctrl.CurrentCrest)
                    {
                        case CrestType.libido:
                            heroine.lewdness = 100;
                            actCtrl.AddDesire(4, heroine, 20); //want to mast
                            actCtrl.AddDesire(5, heroine, 40); //want to h
                            actCtrl.AddDesire(26, heroine, heroine.parameter.attribute.likeGirls ? 30 : 10); //les
                            actCtrl.AddDesire(27, heroine, heroine.parameter.attribute.likeGirls ? 30 : 10); //les
                            actCtrl.AddDesire(29, heroine, 60); //ask for h
                            break;

                        case CrestType.liberated:
                            heroine.lewdness = Mathf.Min(100, heroine.lewdness + 20);
                            actCtrl.AddDesire(4, heroine, 40); //want to mast
                            break;
                    }
                }
            }
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            foreach (var heroine in Game.Instance.HeroineList)
            {
                if (heroine == null) continue;

                switch (heroine.GetCurrentCrest())
                {
                    case CrestType.restore:
                        if (!heroine.isVirgin)
                        {
                            LewdCrestXPlugin.Logger.LogInfo("Resetting heroine to virgin because of restore crest: " + heroine.charFile?.parameter?.fullname);
                            heroine.isVirgin = true;
                            heroine.hCount = 0;
                        }
                        break;
                }
            }

            ClearTempPreggers();
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            ClearTempPreggers();
        }

        private sealed class HsceneHeroineInfo
        {
            public readonly LewdCrestXController Controller;
            public CrestType CrestType => Controller?.CurrentCrest ?? CrestType.None;
            public readonly SaveData.Heroine Heroine;
            public bool NeedsRegenRestored;
            public float TotalRoughTime;

            public HsceneHeroineInfo(SaveData.Heroine heroine)
            {
                Heroine = heroine;
                if (heroine != null) Controller = heroine.GetCrestController();
            }

            public Traverse<bool> GetRegenProp()
            {
                if (LewdCrestXPlugin.SkinEffectsType == null) return null;
                if (Controller == null) return null;
                var seCtrl = Controller.GetComponent(LewdCrestXPlugin.SkinEffectsType);
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