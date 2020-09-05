using System;
using BepInEx;
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
    [BepInPlugin(GUID, "Koikatu Gameplay Tweaks and Improvements", Version)]
    [BepInProcess(GameProcessName)]
    [BepInProcess(GameProcessNameSteam)]
    [BepInProcess(VRProcessName)]
    [BepInProcess(VRProcessNameSteam)]
    [BepInIncompatibility("fulmene.experiencelogic")]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class KoikatuGameplayMod : BaseUnityPlugin
    {
        public const string GUID = "marco-gameplaymod";
        public const string Version = "2.1";
        private const string GameProcessName = "Koikatu";
        private const string GameProcessNameSteam = "Koikatsu Party";
        private const string VRProcessName = "KoikatuVR";
        private const string VRProcessNameSteam = "Koikatsu Party VR";

        public static bool IsInsideVR { get; } = Application.productName == VRProcessName || Application.productName == VRProcessNameSteam;

        public static ConfigEntry<bool> ForceInsert { get; set; }
        public static ConfigEntry<bool> ForceInsertAnger { get; set; }
        public static ConfigEntry<bool> ChangeLewdAfterH { get; set; }
        public static ConfigEntry<bool> DisableTrapVagInsert { get; set; }
        public static ConfigEntry<bool> DontHidePlayerWhenTouching { get; set; }
        public static ConfigEntry<bool> ResetNoCondom { get; set; }
        public static ConfigEntry<bool> AdjustExperiencedStateLogic { get; set; }

        public static ConfigEntry<int> FastTravelTimePenalty { get; set; }
        public static ConfigEntry<bool> StatDecay { get; set; }
        public static ConfigEntry<bool> ChangeLewdDaily { get; set; }
        public static ConfigEntry<bool> AdjustBreastSizeQuestion { get; set; }

        private void Start()
        {
            var i = new Harmony(GUID);
            Utilities.ApplyHooks(i);
            
            var hScene = "H Scene tweaks";
            ForceInsert = Config.Bind(hScene, "Allow force insert", true, "Can insert raw even if it's denied.\nTo force insert - click on the blue insert button right after being denied, after coming outside, or after making her come multiple times. Other contitions might apply.");
            ForceInsertAnger = Config.Bind(hScene, "Force insert causes anger", true, "If you cum inside on or force insert too many times the heroine will get angry with you.\nWhen enabled heroine's expression changes during H (if forced).");
            ChangeLewdAfterH = Config.Bind(hScene, "Change lewdness after H", false, "Decreases heroine's H bar after an H scene if satisfied, increases the bar if not.");
            DisableTrapVagInsert = Config.Bind(hScene, "Disable vaginal insert for traps/men", true, "Only works if you use UncensorSelector to give a female card a penis but no vagina in maker. Some positions don't have the anal option so you won't be able to insert at all in them.\nChanges take effect after game restart.");
            DontHidePlayerWhenTouching = Config.Bind(hScene, "Do not hide player when touching", true, "Prevent hiding of the player model when touching in H scenes.");
            ResetNoCondom = Config.Bind(hScene, "Make experienced girls ask for condom", true, "If enabled, sometimes a heroine will refuse raw insert on dangerous day until the second insert (once per day).\nIf disabled the default game logic is used (girl will never refuse if you did raw 5 times or more in total.)");
            AdjustExperiencedStateLogic = Config.Bind(hScene, "Can be experienced from only one hole", true, "Make it so you only need to max the girls' either vaginal caress/piston or anal caress/piston to achieve experienced state. By default you have to max out both front and rear to get the experienced status.\nChanges take effect after game restart.");
            
            // H Scene functions
            ForceInsertHooks.ApplyHooks(i);
            HSceneHooks.ApplyHooks(i);
            if (DisableTrapVagInsert.Value)
                TrapNoVagInsertHooks.ApplyHooks(i);
            if (AdjustExperiencedStateLogic.Value)
                ExperienceLogicHooks.ApplyHooks(i);

            if (!IsInsideVR)
            {
                Utilities.HSceneEndClicked += UpdateLewdAfterH;

                var mainGame = "Main game";
                FastTravelTimePenalty = Config.Bind(mainGame, "Fast travel (F3) time cost", 50, new ConfigDescription("Value is in seconds. One period has 500 seconds.", new AcceptableValueRange<int>(0, 100)));
                StatDecay = Config.Bind(mainGame, "Player stats slowly decay overnight", true, "Player's stats slowly decrease every day to keep the training points relevant.");
                ChangeLewdDaily = Config.Bind(mainGame, "Change lewdness overnight", false, "H bar of all heroines either increases or decreases overnight depending on their status.");
                AdjustBreastSizeQuestion = Config.Bind(mainGame, "Adjust preferred breast size question", true, "Lowers the breast size needed for 'Average' and 'Large' breast options when a heroine asks you what size you prefer.\nChanges take effect after game restart.");

                // Main game functions
                ClassCharaLimitUnlockHooks.ApplyHooks(i);
                FastTravelCostHooks.ApplyHooks(i);
                if (AdjustBreastSizeQuestion.Value)
                    BustSizeQuestionHooks.ApplyHooks(i);

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
            }
        }

        // Start as false to prevent firing after loading
        private bool _inNightMenu, _firstNightMenu = true;

        private static void OnNightStarted()
        {
            var gameMgr = Game.Instance;
            if (StatDecay.Value)
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

            if (ChangeLewdDaily.Value)
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

            if (ResetNoCondom.Value)
            {
                foreach (var heroine in gameMgr.HeroineList)
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

        private static void UpdateLewdAfterH(HSprite hSprite)
        {
            if (!ChangeLewdAfterH.Value) return;

            var heroine = Utilities.GetTargetHeroine(hSprite);
            if (heroine == null) return;

            var orgCount = hSprite.flags.GetOrgCount();
            if (orgCount == 0) orgCount = -hSprite.flags.GetInsideAndOutsideCount(); // Increase lewdness if girl didn't org but guy did
            heroine.lewdness = Mathf.Clamp(heroine.lewdness - orgCount * 40, 0, 100);
        }
    }
}
