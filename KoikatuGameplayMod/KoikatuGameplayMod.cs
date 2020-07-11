using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using Manager;
using UnityEngine;

namespace KoikatuGameplayMod
{
    [BepInPlugin(GUID, "Koikatu Gameplay Tweaks and Improvements", Version)]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInDependency(KoikatuAPI.GUID, "1.12")]
    public class KoikatuGameplayMod : BaseUnityPlugin
    {
        public const string GUID = "marco-gameplaymod";
        internal const string Version = "1.4.2";

        public static ConfigEntry<bool> ForceInsert { get; set; }
        public static ConfigEntry<bool> ForceInsertAnger { get; set; }
        public static ConfigEntry<bool> DecreaseLewd { get; set; }
        public static ConfigEntry<bool> DisableTrapVagInsert { get; set; }

        public static ConfigEntry<bool> StatDecay { get; set; }
        public static ConfigEntry<bool> LewdDecay { get; set; }
        public static ConfigEntry<bool> ResetNoCondom { get; set; }
        public static ConfigEntry<int> FastTravelTimePenalty { get; set; }
        public static ConfigEntry<bool> AdjustBreastSizeQuestion { get; set; }

        private Game _gameMgr;
        private Scene _sceneMgr;

        private void Start()
        {
            var hScene = "H Scene tweaks";
            ForceInsert = Config.Bind(hScene, "Allow force insert", true, "Can insert raw even if it's denied.\nTo force insert - click on the blue insert button right after being denied, after coming outside, or after making her come multiple times. Other contitions might apply.");
            ForceInsertAnger = Config.Bind(hScene, "Force insert causes anger", true, "If you cum inside on or force insert too many times the girl will get angry with you.\nWhen enabled girl's expression changes during H (if forced).");
            DecreaseLewd = Config.Bind(hScene, "Decrease lewdness after H", false, "Decreases girl's H bar after an H scene.");
            DisableTrapVagInsert = Config.Bind(hScene, "Disable vaginal insert for traps/men", true, "Only works if you use UncensorSelector to give a female card a penis but no vagina in maker. Some positions don't have the anal option so you won't be able to insert at all in them.\nChanges take effect after game restart.");

            ResetNoCondom = Config.Bind(hScene, "Make experienced girls ask for condom", true, "If enabled, sometimes a girl will refuse raw insert on dangerous day until the second insert (once per day).\nIf disabled the default game logic is used (girl will never refuse if you did raw 5 times or more in total.)");
            var mainGame = "Main game";
            FastTravelTimePenalty = Config.Bind(mainGame, "Fast travel (F3) time cost", 50, new ConfigDescription("Value is in seconds. One period has 500 seconds.", new AcceptableValueRange<int>(0, 100)));
            StatDecay = Config.Bind(mainGame, "Player stats slowly decay overnight", true, "Player's stats slowly decrease every day to keep the training points relevant.");
            LewdDecay = Config.Bind(mainGame, "Girl lewdness decays overnight", false, "H bar of all heroines decreases overnight.");
            AdjustBreastSizeQuestion = Config.Bind(mainGame, "Adjust preferred breast size question", true, "Lowers the breast size needed for 'Average' and 'Large' breast options when a girl asks you what size you prefer.\nChanges take effect after game restart.");

            var i = new Harmony(GUID);
            Utilities.ApplyHooks(i);
            Utilities.HSceneEndClicked += UpdateGirlLewdness;

            // H Scene functions
            ForceInsertHooks.ApplyHooks(i);
            ExitFirstHHooks.ApplyHooks(i);
            if (DisableTrapVagInsert.Value)
                TrapNoVagInsertHooks.ApplyHooks(i);

            // Main game functions
            ClassCharaLimitUnlockHooks.ApplyHooks(i);
            FastTravelCostHooks.ApplyHooks(i);
            if (AdjustBreastSizeQuestion.Value)
                BustSizeQuestionHooks.ApplyHooks(i);
            
            _gameMgr = Game.Instance;
            _sceneMgr = Scene.Instance;

            // todo replace with scene load?
            InvokeRepeating(nameof(SlowUpdate), 2f, 0.5f);
        }

        // Start as false to prevent firing after loading
        private bool _inNightMenu, _firstNightMenu = true;

        private void OnNightStarted()
        {
            if (StatDecay.Value)
            {
                void LowerStat(ref int stat)
                {
                    stat -= UnityEngine.Random.Range(0, 2);

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

        private void SlowUpdate()
        {
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

        private static void UpdateGirlLewdness(HSprite hSprite)
        {
            if (!DecreaseLewd.Value) return;

            var flags = hSprite.flags;
            var count = flags.count;
            var heroine = Utilities.GetTargetHeroine(hSprite);
            if (heroine == null) return;

            if (flags.GetOrgCount() == 0)
            {
                var massageTotal = (int)(count.selectAreas.Sum() / 4 + (+count.kiss + count.houshiOutside + count.houshiInside) * 10);
                if (massageTotal <= 5)
                    heroine.lewdness = Math.Max(0, heroine.lewdness - 30);
                else
                    heroine.lewdness = Math.Min(100, heroine.lewdness + massageTotal);
            }
            else if (count.aibuOrg > 0 && count.sonyuOrg + count.sonyuAnalOrg == 0)
            {
                heroine.lewdness = Math.Min(100, heroine.lewdness - (count.aibuOrg - 1) * 20);
            }
            else
            {
                int cumCount = count.sonyuCondomInside + count.sonyuInside + count.sonyuOutside + count.sonyuAnalCondomInside + count.sonyuAnalInside + count.sonyuAnalOutside;
                if (cumCount > 0)
                    heroine.lewdness = Math.Max(0, heroine.lewdness - cumCount * 20);

                heroine.lewdness = Math.Max(0, heroine.lewdness - count.aibuOrg * 20);
            }
        }
    }
}
