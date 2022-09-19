using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using HarmonyLib;
using KKAPI;
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
        private HFlag _hFlag;

        private List<LewdCrestXController> _existingControllers;

        private readonly Dictionary<SaveData.Heroine, float> _actionCooldowns = new Dictionary<SaveData.Heroine, float>();

        private void SetActionCooldown(SaveData.Heroine heroine, int seconds)
        {
            _actionCooldowns[heroine] = seconds;
        }

        private float AdvanceAndGetActionCooldown(SaveData.Heroine heroine)
        {
            _actionCooldowns.TryGetValue(heroine, out var timeLeft);
            timeLeft = Mathf.Max(0, timeLeft - Time.deltaTime);
            _actionCooldowns[heroine] = timeLeft;
            return timeLeft;
        }

#if KK
        protected override void OnStartH(BaseLoader proc, HFlag hFlag, bool vr)
#else
        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr)
#endif
        {
            _hFlag = hFlag;
            _hSceneHeroines = hFlag.lstHeroine.Select(x => new HsceneHeroineInfo(x)).ToArray();

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

#if KK
        protected override void OnEndH(BaseLoader proc, HFlag hFlag, bool vr)
#else
        protected override void OnEndH(MonoBehaviour proc, HFlag hFlag, bool vr)
#endif
        {
            foreach (var heroine in _hSceneHeroines)
            {
                if (heroine.CrestType == CrestType.violove)
                {
                    var totalTime = (int)heroine.TotalRoughTime;
                    var h = heroine.Heroine;
                    h.lewdness = Mathf.Min(100, h.lewdness + totalTime / 10);
#if KK
                    h.intimacy = Mathf.Min(100, h.intimacy + totalTime / 30);
                    h.favor = Mathf.Min(100, h.favor + totalTime / 20);
#else
                    h.favor = Mathf.Min(h.isGirlfriend ? 150 : 100, h.favor + totalTime / 20);
#endif
                }

                if (heroine.NeedsRegenRestored)
                    heroine.GetRegenProp().Value = false;
            }

            _hFlag = null;
            _hSceneHeroines = null;
        }

        private void Update()
        {
            if (_hSceneHeroines != null)
            {
                if (SceneApi.GetIsNowLoadingFade()) return;

                var speed = _hFlag.IsSonyu() && _hFlag.speedCalc > 0.7f
                    ? (_hFlag.speedCalc - 0.7f) * 3.3f
                    : (_hFlag.speedItem > 1.4f ? 0.33f : 0);
                if (speed > 0)
                {
                    var id = _hFlag.GetLeadingHeroineId();
                    var heroineInfo = _hSceneHeroines[id];
                    heroineInfo.TotalRoughTime += Time.deltaTime;

                    if (heroineInfo.CrestType == CrestType.suffer && !_hFlag.lockGugeFemale)
                        _hFlag.gaugeFemale += speed * Time.deltaTime * 2;
                }
            }
            else if (_existingControllers != null)
            {
                for (var i = 0; i < _existingControllers.Count; i++)
                {
                    var controller = _existingControllers[i];
                    if (controller.CurrentCrest == CrestType.mantraction)
                    {
                        if (SceneApi.GetIsNowLoadingFade()) return;

                        var actScene = GetActionScene();
                        if (actScene == null) return;
                        var player = actScene.Player;
                        if (player == null) continue;

                        // Stop the crest from activating outsied of when player runs around
                        // needed because if the code below activates during an adv scene or some other inopportune times it will softlock the game
                        if (player.isActionNow) return;
#if KKS
                        if (Game.IsRegulate(true)) return;
                        if (actScene.regulate != 0 || (actScene.AdvScene != null && actScene.AdvScene.gameObject.activeSelf) || TalkScene.isPaly) return;
#elif KK
                        if (Game.instance.IsRegulate(true)) return;
#endif

                        var heroine = controller.Heroine;
                        if (heroine == null) continue;

                        var npc = heroine.GetNPC();
                        if (player.mapNo == npc.mapNo)
                        {
                            var cooldownTimeLeft = AdvanceAndGetActionCooldown(heroine);
                            if (cooldownTimeLeft <= 0)
                            {
                                if (Vector3.Distance(player.position, npc.position) > 3)
                                {
                                    LewdCrestXPlugin.Logger.LogInfo("Chasing player because of mantraction crest: " + controller.Heroine.GetFullname());
                                    npc.ItemClear();
                                    npc.AI.ChaseAction();
                                    // Need to replace this from the default chase id because that disables the talk to bubble
                                    // This action is triggered when character approaches player
                                    // 28 - wanting to talk to player. If too annoying might want to replace with actions in range 10 - 16, maybe randomized
                                    npc.AI.actResult.value.actionNo = 28;

                                    SetActionCooldown(heroine, 10);
                                }
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
            yield return new WaitWhile(SceneApi.GetIsNowLoadingFade);

            PreggersHooks.OnPeriodChanged();

            _existingControllers = GetHeroineList().Select(x => x.GetCrestController()).Where(x => x != null).ToList();
            var actCtrl = GameAPI.GetActionControl();
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
            foreach (var heroine in GetHeroineList())
            {
                if (heroine == null) continue;

                switch (heroine.GetCurrentCrest())
                {
                    case CrestType.restore:
                        if (!heroine.isVirgin)
                        {
                            LewdCrestXPlugin.Logger.LogInfo("Resetting heroine to virgin because of restore crest: " + heroine.GetFullname());
                            heroine.isVirgin = true;
                            heroine.hCount = 0;
                        }
                        break;
                }
            }

            PreggersHooks.ClearTempPreggers();
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            PreggersHooks.ClearTempPreggers();
        }

        internal static List<SaveData.Heroine> GetHeroineList()
        {
#if KK
            return Game.Instance.HeroineList;
#else
            return Game.HeroineList;
#endif
        }

        internal static ActionScene GetActionScene()
        {
#if KK
            return Game.Instance.actScene;
#else
            return GameAPI.GetActionControl().actionScene;
#endif
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
                if (heroine != null)
                {
                    Controller = heroine.GetCrestController();
                    // Needed in free H because GetHeroine used in the Controller doesn't work outside main game
                    if (Controller != null) Controller.Heroine = heroine;
                }
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
