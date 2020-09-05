using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

namespace KK_MobAdder
{
    [BepInPlugin(GUID, "Add mobs to roam mode", Version)]
    [BepInProcess(GameProcessName)]
    [BepInProcess(GameProcessNameSteam)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class MobAdderPlugin : BaseUnityPlugin
    {
        public const string GUID = "KK_MobAdder";
        public const string Version = "2.0";

        private const string GameProcessName = "Koikatu";
        private const string GameProcessNameSteam = "Koikatsu Party";

        private static int _lastLoadedMapNo = -1;

        internal static ConfigEntry<KeyboardShortcut> SpawnMobKey;
        internal static ConfigEntry<KeyboardShortcut> SaveMobPositionDataKey;
        internal static ConfigEntry<float> MobAmountModifier;

        internal static new ManualLogSource Logger;

        private void Start()
        {
            Logger = base.Logger;

            try
            {
                MobManager.ReadCsv();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to read .csv files with mob data: {ex}");
                enabled = false;
                return;
            }

            SpawnMobKey = Config.Bind("Developer", "Spawn or remove mob", KeyboardShortcut.Empty,
                new ConfigDescription("Create a new mob at player position, or remove nearest mob if Shift is pressed.", null, "Advanced"));
            SaveMobPositionDataKey = Config.Bind("Developer", "Spawn mob position data", KeyboardShortcut.Empty,
                new ConfigDescription("Save all mob positions to the position .csv file, overwriting the original. Hold shift to also save spread data.", null, "Advanced"));
            MobAmountModifier = Config.Bind("General", "Mob amount modifier", 1f,
                new ConfigDescription("How many mobs should be spawned compared to the default (1x). 0x will disable mob spawning.", new AcceptableValueRange<float>(0, 1.5f)));

            // Used for spawning mobs
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            // Used for gathering mobs during h scenes
            GameAPI.StartH += (sender, e) =>
            {
                var initialPos = FindObjectOfType<HScene>().GetComponentInChildren<ChaControl>().transform.position;
                MobManager.GatherMobsAroundPoint(initialPos);
            };
            GameAPI.EndH += (sender, e) => MobManager.UndoMobGathering();
            Harmony.CreateAndPatchAll(typeof(MobAdderPlugin));
        }

        /// <summary>
        /// Triggers when changing location in h scenes
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeCategory))]
        private static void HLocationChangeHook(List<ChaControl> ___lstFemale)
        {
            try
            {
                var hsceneCenterPoint = ___lstFemale[0].transform.position;
                MobManager.GatherMobsAroundPoint(hsceneCenterPoint);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        private void Update()
        {
            if (SpawnMobKey.Value.IsDown())
            {
                var mapNo = GetCurrentMapNo();
                var player = Game.Instance.Player.transform;
                var position = player.position;
                var rotation = player.rotation;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    MobManager.RemoveClosestMob(mapNo, position);
                }
                else
                {
                    MobManager.SpawnMob(position, rotation, true, mapNo);
                    MobManager.AddMobPosition(mapNo, position, rotation);
                }
            }
            else if (SaveMobPositionDataKey.Value.IsDown())
            {
                try
                {
                    MobManager.SaveCsv();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error | LogLevel.Message, $"Failed to save .csv file with mob data: {ex.Message}");
                }
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            try
            {
                var currentMap = GetCurrentMapNo();
                if (_lastLoadedMapNo == currentMap) return;
                _lastLoadedMapNo = currentMap;

                StartCoroutine(MobManager.SpawnMobs(currentMap, arg0.name));
            }
            catch (Exception ex)
            {
                // Don't crash the event
                Logger.LogError(ex);
            }
        }

        private static int GetCurrentMapNo()
        {
            if (KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame) return -1;
            if (!Game.IsInstance() || Game.Instance.actScene == null || Game.Instance.actScene.Map == null) return -1;
            return Game.Instance.actScene.Map.no;
        }
    }
}