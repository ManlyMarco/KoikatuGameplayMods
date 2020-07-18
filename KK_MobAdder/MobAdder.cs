using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionGame;
using BepInEx;
using BepInEx.Configuration;
using Illusion.Component.Correct;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace KK_MobAdder
{
    // todo add background noise when many mobs are in
    // todo add static colliders so mobs cant be pathed around
    // todo add dynamic colliders to charas so they don't go through each other
    [BepInPlugin(GUID, "Add mob characters to roam mode", Version)]
    [BepInProcess(GameProcessName)]
    [BepInProcess(GameProcessNameSteam)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, "1.6")]
    public class MobAdder : BaseUnityPlugin
    {
        public const string GUID = "KK_MobAdder";
        public const string Version = "1.0";
        private const string GameProcessName = "Koikatu";
        private const string GameProcessNameSteam = "Koikatsu Party";

        /// <summary>
        /// Character animation state names to pick from
        /// </summary>
        private static string[] _charaStateNames = new string[]
        {
                "Idle",
                "talk",
                //"defense",
                "talk_uke",
                "res_talk",
                //"hesitantly",
                "Phone",
                "Phone2",
                //"Phone3", CHAIR
                //"Phone4", CHAIR
                "Music",
                //"Music2", CHAIR
                //"Music3", CHAIR
                "Dance",
                //"Dance2", CHAIR
                "Appearance",
                "Appearance2",
                //"Appearance3", CHAIR
                "Appearance5",
                //"Appearance6", CHAIR
                //"Drink", CHAIR
                "Drink3",
                //"Drink4", CHAIR
                "ChangeMind",
                "ChangeMind1",
                //"ChangeMind2", CHAIR
                "ChangeMind3",
                //"ChangeMind4", chair
                "ChangeMind5",
                //"ChangeMind6", CHAIR
                "ChangeMind7",
                //"ChangeMind8", lean back
                //"ChangeMind9", // poolside sit in the air
                //"ChangeMind10", CHAIR
                //"ChangeMind11", desk
                //"ChangeMind12", CHAIR SLEEP AT DESK
                "Sport",
                "Sport2",
                "Sport3",
                "Sport6"
        };

        private static string CsvLocationPositions = Path.Combine(Path.GetDirectoryName(typeof(MobAdder).Assembly.Location), "KK_MobAdder_MobPositions.csv");
        private static string CsvLocationSpread = Path.Combine(Path.GetDirectoryName(typeof(MobAdder).Assembly.Location), "KK_MobAdder_MobSpread.csv");

        private static Dictionary<int, List<KeyValuePair<Vector3, Quaternion>>> _mobPositionData = new Dictionary<int, List<KeyValuePair<Vector3, Quaternion>>>();
        private static Dictionary<int, float[]> _mobSpreadData = new Dictionary<int, float[]>();

        private static MaterialPropertyBlock _mobColorProperty = new MaterialPropertyBlock();
        private static GameObject _mobTemplate;
        private static List<GameObject> _spawnedMobs = new List<GameObject>();
        private static int lastLoadedMapNo = -1;

        private ConfigEntry<KeyboardShortcut> _spawnMobKey;
        private ConfigEntry<KeyboardShortcut> _saveMobPositionDataKey;
        private ConfigEntry<float> _mobAmountModifier;

        private void Start()
        {
            try
            {
                ReadCsv();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to read .csv files with mob data: " + ex);
                _mobPositionData.Clear();
                enabled = false;
                return;
            }

            _spawnMobKey = Config.Bind("Developer", "Spawn or remove mob", KeyboardShortcut.Empty, new ConfigDescription("Create a new mob at player position, or remove nearest mob if Shift is pressed.", null, "Advanced"));
            _saveMobPositionDataKey = Config.Bind("Developer", "Spawn mob position data", KeyboardShortcut.Empty, new ConfigDescription("Save all mob positions to the position .csv file, overwriting the original. Hold shift to also save spread data.", null, "Advanced"));
            _mobAmountModifier = Config.Bind("General", "Mob amount modifier", 1f, new ConfigDescription("How many mobs should be spawned compared to the default (1x). 0x will disable mob spawning.", new AcceptableValueRange<float>(0, 1.5f)));

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void Update()
        {
            if (_spawnMobKey.Value.IsDown())
            {
                Transform player = Manager.Game.Instance.Player.transform;
                int no = GetCurrentMapNo();

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (_mobPositionData.TryGetValue(no, out var list))
                    {
                        // Remove closest mob
                        var toRemove = list.OrderBy(x => Vector3.Distance(x.Key, player.position)).Take(1).ToArray();
                        if (toRemove.Length == 1)
                        {
                            list.Remove(toRemove[0]);
                            var spawned = _spawnedMobs.FirstOrDefault(x => x.transform.position == toRemove[0].Key);
                            Destroy(spawned);
                        }
                        Logger.LogMessage("Removed mob at " + toRemove[0].Key);
                    }
                }
                else
                {
                    SpawnMob(player.position, player.rotation, true, no);
                    AddMobPosition(no, player.position, player.rotation);
                }
            }
            else if (_saveMobPositionDataKey.Value.IsDown())
            {
                SaveCsv();
            }
        }

        private GameObject SpawnMob(Vector3 position, Quaternion rotation, bool log, int no)
        {
            var copy = Instantiate(_mobTemplate);

            copy.transform.SetPositionAndRotation(position, rotation);
            copy.SetActive(true);

            var anim = copy.GetComponentInChildren<Animator>(); //copy.transform.Find("p_cf_body_bone_low").GetComponent<Animator>();
            string stateName = _charaStateNames[UnityEngine.Random.Range(0, _charaStateNames.Length - 1)];
            anim.Play(stateName);

            var hBone = copy.transform.Find("p_cf_body_bone/cf_j_root/cf_n_height");
            hBone.localScale = new Vector3(UnityEngine.Random.Range(0.825f, 0.975f), UnityEngine.Random.Range(0.825f, 0.975f), UnityEngine.Random.Range(0.825f, 0.975f));

            // todo set only once at the start, together with on/off mob toggle?
            copy.GetComponentInChildren<Renderer>().SetPropertyBlock(_mobColorProperty);

            if (log) Logger.LogMessage($"Added a mob: mapno:{no} anim:{stateName} scale:{hBone.localScale} pos:{position} rot:{rotation}");

            _spawnedMobs.Add(copy);

            copy.transform.SetParent(Manager.Game.Instance.actScene.Map.mapRoot.transform);

            return copy;
        }

        private static GameObject CreateTemplate()
        {
            var top = new GameObject("silhouette_template");
            DontDestroyOnLoad(top);

            SkinnedMeshRenderer PrepareRenderers(GameObject obj)
            {
                SkinnedMeshRenderer result = null;
                foreach (var r in obj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (r.transform.name == "n_body_silhouette")
                    {
                        result = r;
                        r.transform.parent.gameObject.SetActive(true);

                        // Change to map layer to fix appearing on top of everything in talk scenes
                        r.gameObject.layer = 11;
                        r.receiveShadows = false;
                        r.shadowCastingMode = ShadowCastingMode.Off;
                    }
                    else
                    {
                        Destroy(r.gameObject);
                    }
                }

                return result;
            }

            var modelObj = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00_low", true, "abdata");
            var modelRend = PrepareRenderers(modelObj);
            GameObject animObj = null;
            if (modelRend != null)
            {
                animObj = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cf_body_bone_low", true, "abdata");
            }
            else
            {
                // The fallback is needed for KK Party, posibly pre-darkness KK
                Destroy(modelObj);
                modelObj = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00", true, "abdata");
                modelRend = PrepareRenderers(modelObj);
                if (modelRend == null)
                {
                    Destroy(modelObj);
                    throw new InvalidOperationException("Could not find silhouette model data");
                }
                animObj = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cf_body_bone", true, "abdata");
            }
            modelObj.transform.SetParent(top.transform);
            animObj.transform.SetParent(top.transform);

            var animCmp = animObj.GetComponent<Animator>();
            foreach (var c in animObj.GetComponentsInChildren(typeof(Component), true))
            {
                if (c is Animator || c is Transform)
                    continue;

                // Delay these because other components rely on them so they have to be removed later
                if (c is BaseData || c is NeckLookCalcVer2)
                    Destroy(c);
                else
                    DestroyImmediate(c);
            }

            // Replace mesh bones with animator bones
            var animBones = animCmp.GetComponentsInChildren<Transform>(true);
            var modelBones = modelRend.bones.ToArray();
            for (var i = 0; i < modelBones.Length; i++)
            {
                modelBones[i] = animBones.First(x => x.name == modelBones[i].name);
            }
            modelRend.rootBone = animBones.First(x => x.name == "cf_j_root");
            modelRend.bones = modelBones;
            // Destroy no longer used bones
            Destroy(modelObj.transform.Find("cf_j_root")?.gameObject);
            // Not used
            Destroy(modelObj.transform.Find("cf_o_root/n_cm_body")?.gameObject);
            Destroy(modelObj.transform.Find("cf_o_root/n_tang")?.gameObject);

            var animBundle = AssetBundle.LoadFromFile(@"abdata\action\animator\00.unity3d");
            var animBase = animBundle.LoadAsset("base");

            var ctrl = (RuntimeAnimatorController)Instantiate(animBase);
            animCmp.runtimeAnimatorController = ctrl;

            animBundle.Unload(false);

            // Add colliders so characters avoid them and player can't walk through
            var obst = top.AddComponent<NavMeshObstacle>();
            obst.carving = true;
            obst.shape = NavMeshObstacleShape.Capsule;
            obst.radius = 0.35f;

            top.SetActive(false);

            return top;
        }

        private static void ReadCsv()
        {
            foreach (var line in File.ReadAllText(CsvLocationPositions).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!char.IsDigit(line[0])) continue;

                var contents = line.Split(',').Select(x => x.Trim()).ToArray();

                var no = int.Parse(contents[0]);
                var pos = new Vector3(float.Parse(contents[1]), float.Parse(contents[2]), float.Parse(contents[3]));
                var rotEul = new Vector3(float.Parse(contents[4]), float.Parse(contents[5]), float.Parse(contents[6]));
                var rot = Quaternion.Euler(rotEul);

                AddMobPosition(no, pos, rot);
            }

            int cycleTypeCount = Enum.GetValues(typeof(Cycle.Type)).Length;
            if (File.Exists(CsvLocationSpread))
            {
                foreach (var line in File.ReadAllText(CsvLocationSpread).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!char.IsDigit(line[0])) continue;

                    var contents = line.Split(',').Select(x => x.Trim()).ToArray();

                    var no = int.Parse(contents[0]);

                    var arr = new float[cycleTypeCount];

                    for (int i = 0; i < cycleTypeCount; i++)
                        arr[i] = float.Parse(contents[i + 1]);

                    _mobSpreadData[no] = arr;
                }
            }
            else
            {
                foreach (var mapNo in _mobPositionData.Keys)
                    _mobSpreadData[mapNo] = Enumerable.Repeat(1f, cycleTypeCount).ToArray();
            }
        }

        private static void AddMobPosition(int no, Vector3 pos, Quaternion rot)
        {
            if (!_mobPositionData.TryGetValue(no, out var list))
            {
                list = new List<KeyValuePair<Vector3, Quaternion>>();
                _mobPositionData.Add(no, list);
            }

            list.Add(new KeyValuePair<Vector3, Quaternion>(pos, rot));
        }

        private void SaveCsv()
        {
            if (File.Exists(CsvLocationPositions))
            {
                File.Delete(CsvLocationPositions + ".bak");
                File.Move(CsvLocationPositions, CsvLocationPositions + ".bak");
            }

            Logger.LogMessage("Writing mob positions to " + CsvLocationPositions);

            using (var f = File.OpenWrite(CsvLocationPositions))
            using (var w = new StreamWriter(f))
            {
                w.WriteLine($"MapNo, PosX, PosY, PosZ, RotX, RotY, RotZ");
                foreach (var r in _mobPositionData.SelectMany(x => x.Value.Select(y => new { no = x.Key, pos = y.Key, rot = y.Value.eulerAngles })))
                    w.WriteLine($"{r.no}, {r.pos.x:F2}, {r.pos.y:F2}, {r.pos.z:F2}, {r.rot.x:F2}, {r.rot.y:F2}, {r.rot.z:F2}");
            }

            if (!Input.GetKey(KeyCode.LeftShift)) return;

            if (File.Exists(CsvLocationSpread))
            {
                File.Delete(CsvLocationSpread + ".bak");
                File.Move(CsvLocationSpread, CsvLocationSpread + ".bak");
            }

            Logger.LogMessage("Writing mob spread to " + CsvLocationSpread);

            using (var f = File.OpenWrite(CsvLocationSpread))
            using (var w = new StreamWriter(f))
            {
                w.WriteLine($"MapNo, {string.Join(", ", Enum.GetNames(typeof(Cycle.Type)))}");

                foreach (var r in _mobSpreadData)
                    w.WriteLine($"{r.Key}, {string.Join(", ", r.Value.Select(x => x.ToString("F2")).ToArray())}");
            }
        }

        private static int GetCurrentMapNo()
        {
            if (KKAPI.KoikatuAPI.GetCurrentGameMode() != KKAPI.GameMode.MainGame) return -1;
            if (!Manager.Game.IsInstance() || Manager.Game.Instance.actScene == null || Manager.Game.Instance.actScene.Map == null) return -1;
            return Manager.Game.Instance.actScene.Map.no;
        }

        /// <summary>
        /// Shuffles the element order of the specified list.
        /// </summary>
        private static void Shuffle<T>(IList<T> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            try
            {
                var currentMap = GetCurrentMapNo();
                if (lastLoadedMapNo == currentMap) return;

                StartCoroutine(OnSceneChange(currentMap, arg0.name));
            }
            catch (Exception ex)
            {
                // Don't crash the event
                Logger.LogError(ex);
            }
        }

        private IEnumerator OnSceneChange(int currentMap, string name)
        {
            /* Map IDs
            2 1ST FLOOR
            3 2ND FLOOR
            4 3RD FLOOR
            17 STAFF
            18 MALE TOILET
            21 LIBRARY
            31 GYM STORAGE
            32 GYM
            33 FRONTYARD
            34 FIELD
            36 ROOF
            37 POOL
            38 CAFETERIA
            47 BACKYARD
            */

            yield return null;

            // Clean up previous spawns
            foreach (var mob in _spawnedMobs) Destroy(mob);
            _spawnedMobs.Clear();

            lastLoadedMapNo = currentMap;

            if (currentMap < 0) yield break;

            if (!Manager.Config.AddData.mobVisible || _mobAmountModifier.Value <= 0) yield break;

            // todo remove all if map or period changed and reapply

            if (_mobPositionData.TryGetValue(currentMap, out var list))
            {
                if (_mobTemplate == null)
                {
                    _mobTemplate = CreateTemplate();
                    // Allow for template to finish creating
                    yield return null;
                }

                _mobColorProperty.SetColor(ChaShader._Color, Manager.Config.AddData.mobColor);

                Logger.LogDebug($"Spawning {list.Count} mobs on map {name}");

                // todo store per-cycle?

                var amount = 1f;
                Cycle cycle = Manager.Game.Instance.actScene.Cycle;
                if (_mobSpreadData.TryGetValue(currentMap, out var spreads))
                    amount = spreads[(int)cycle.nowType];

                if (cycle.nowWeek == Cycle.Week.Saturday || cycle.nowWeek == Cycle.Week.Holiday)
                    amount *= 0.3f;

                // Choose a different amount of mobs based on the time of day and some random spread
                int mobCount = (int)(list.Count * amount * UnityEngine.Random.Range(0.5f, 1.2f) * _mobAmountModifier.Value);
                if (mobCount > 0)
                {
                    // Choose random mob positions
                    Shuffle(list);
                    foreach (var entry in list.Take(mobCount))
                        SpawnMob(entry.Key, entry.Value, false, currentMap);
                }
            }
        }
    }
}