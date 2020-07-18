using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionGame;
using Illusion.Component.Correct;
using Manager;
using StrayTech;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace KK_MobAdder
{
    internal static class MobManager
    {
        /// <summary>
        /// Character animation state names to pick from
        /// </summary>
        private static readonly string[] _charaStateNames =
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

        private static readonly string _csvLocationPositions;
        private static readonly string _csvLocationSpread;

        static MobManager()
        {
            var csvDirectory = Path.GetDirectoryName(typeof(MobAdderPlugin).Assembly.Location) ?? BepInEx.Paths.PluginPath;
            _csvLocationPositions = Path.Combine(csvDirectory, "KK_MobAdder_MobPositions.csv");
            _csvLocationSpread = Path.Combine(csvDirectory, "KK_MobAdder_MobSpread.csv");
        }

        private static readonly Dictionary<int, List<KeyValuePair<Vector3, Quaternion>>> _mobPositionData =
            new Dictionary<int, List<KeyValuePair<Vector3, Quaternion>>>();
        private static readonly Dictionary<int, float[]> _mobSpreadData = new Dictionary<int, float[]>();
        private static readonly MaterialPropertyBlock _mobColorProperty = new MaterialPropertyBlock();
        private static GameObject _mobTemplate;
        private static readonly List<SpawnedMobInfo> _spawnedMobs = new List<SpawnedMobInfo>();

        public static void GatherMobsAroundPoint(Vector3 centerPoint)
        {
            var usedPositions = new HashSet<Vector3>();

            bool RandomPoint(out Vector3 result)
            {
                const float range = 1.75f;
                for (var i = 0; i < 30; i++)
                {
                    var onUnitSphere = Random.onUnitSphere;
                    onUnitSphere.y = 0;
                    onUnitSphere.Normalize();
                    var randomPoint = centerPoint + onUnitSphere * (range + (1 - Random.value) * 0.8f);
                    if (NavMesh.SamplePosition(randomPoint, out var hit, 0.5f, NavMesh.AllAreas))
                    {
                        result = hit.position;
                        if (usedPositions.All(x => Vector3.Distance(hit.position, x) > 0.3f))
                            return true;
                    }
                }

                result = Vector3.zero;
                return false;
            }

            foreach (var spawnedMob in _spawnedMobs)
            {
                if (Vector3.Distance(spawnedMob.InitialPosition, centerPoint) < 10)
                    if (RandomPoint(out var mobPos))
                    {
                        spawnedMob.Object.transform.position = mobPos;
                        spawnedMob.Object.transform.LookAtXZ(centerPoint);
                        usedPositions.Add(mobPos);
                        continue;
                    }

                spawnedMob.ResetPosAndRot();
            }
        }

        public static void UndoMobGathering()
        {
            foreach (var spawnedMob in _spawnedMobs) spawnedMob.ResetPosAndRot();
        }

        public static GameObject SpawnMob(Vector3 position, Quaternion rotation, bool log, int no)
        {
            var copy = Object.Instantiate(_mobTemplate, Game.Instance.actScene.Map.mapRoot.transform);

            copy.transform.SetPositionAndRotation(position, rotation);
            copy.SetActive(true);

            var anim = copy.GetComponentInChildren<Animator>(); //copy.transform.Find("p_cf_body_bone_low").GetComponent<Animator>();
            var stateName = _charaStateNames[Random.Range(0, _charaStateNames.Length - 1)];
            anim.Play(stateName);

            var hBone = copy.transform.Find("p_cf_body_bone/cf_j_root/cf_n_height");
            hBone.localScale = new Vector3(Random.Range(0.825f, 0.975f), Random.Range(0.825f, 0.975f), Random.Range(0.825f, 0.975f));

            copy.GetComponentInChildren<Renderer>().SetPropertyBlock(_mobColorProperty);

            if (log)
                MobAdderPlugin.Logger.LogMessage(
                    $"Added a mob: mapno:{no} anim:{stateName} scale:{hBone.localScale} pos:{position} rot:{rotation}");

            _spawnedMobs.Add(new SpawnedMobInfo(copy, position, rotation));

            return copy;
        }

        public static GameObject CreateTemplate()
        {
            var top = new GameObject("silhouette_template");
            Object.DontDestroyOnLoad(top);

            SkinnedMeshRenderer PrepareRenderers(GameObject obj)
            {
                SkinnedMeshRenderer result = null;
                foreach (var r in obj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
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
                        Object.Destroy(r.gameObject);
                    }

                return result;
            }

            var modelObj = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00_low", true, "abdata");
            var modelRend = PrepareRenderers(modelObj);
            GameObject animObj;
            if (modelRend != null)
            {
                animObj = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cf_body_bone_low", true, "abdata");
            }
            else
            {
                // The fallback is needed for KK Party, posibly pre-darkness KK
                Object.Destroy(modelObj);
                modelObj = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00", true, "abdata");
                modelRend = PrepareRenderers(modelObj);
                if (modelRend == null)
                {
                    Object.Destroy(modelObj);
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

                // Get rid of useless performance-eating scripts
                // Can't DestroyImmediate these two because other components rely on them so they have to be removed later
                if (c is BaseData || c is NeckLookCalcVer2)
                    Object.Destroy(c);
                else
                    Object.DestroyImmediate(c);
            }

            // Replace mesh bones with animator bones
            var animBones = animCmp.GetComponentsInChildren<Transform>(true);
            var modelBones = modelRend.bones.ToArray();
            for (var i = 0; i < modelBones.Length; i++)
                modelBones[i] = animBones.First(x => x.name == modelBones[i].name);
            modelRend.rootBone = animBones.First(x => x.name == "cf_j_root");
            modelRend.bones = modelBones;
            // Destroy no longer used bones
            Object.Destroy(modelObj.transform.Find("cf_j_root")?.gameObject);
            // Not used
            Object.Destroy(modelObj.transform.Find("cf_o_root/n_cm_body")?.gameObject);
            Object.Destroy(modelObj.transform.Find("cf_o_root/n_tang")?.gameObject);

            var animBundle = AssetBundle.LoadFromFile(@"abdata\action\animator\00.unity3d");
            var animBase = animBundle.LoadAsset("base");

            var ctrl = (RuntimeAnimatorController)Object.Instantiate(animBase);
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

        public static void ReadCsv()
        {
            try
            {
                foreach (var line in File.ReadAllText(_csvLocationPositions)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!char.IsDigit(line[0])) continue;

                    var contents = line.Split(',').Select(x => x.Trim()).ToArray();

                    var no = int.Parse(contents[0]);
                    var pos = new Vector3(float.Parse(contents[1]), float.Parse(contents[2]), float.Parse(contents[3]));
                    var rotEul = new Vector3(float.Parse(contents[4]), float.Parse(contents[5]),
                        float.Parse(contents[6]));
                    var rot = Quaternion.Euler(rotEul);

                    AddMobPosition(no, pos, rot);
                }

                var cycleTypeCount = Enum.GetValues(typeof(Cycle.Type)).Length;
                if (File.Exists(_csvLocationSpread))
                    foreach (var line in File.ReadAllText(_csvLocationSpread)
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!char.IsDigit(line[0])) continue;

                        var contents = line.Split(',').Select(x => x.Trim()).ToArray();

                        var no = int.Parse(contents[0]);

                        var arr = new float[cycleTypeCount];

                        for (var i = 0; i < cycleTypeCount; i++)
                            arr[i] = float.Parse(contents[i + 1]);

                        _mobSpreadData[no] = arr;
                    }
                else
                    foreach (var mapNo in _mobPositionData.Keys)
                        _mobSpreadData[mapNo] = Enumerable.Repeat(1f, cycleTypeCount).ToArray();
            }
            catch
            {
                _mobPositionData.Clear();
                throw;
            }
        }

        public static void SaveCsv()
        {
            try
            {
                if (File.Exists(_csvLocationPositions))
                {
                    File.Delete(_csvLocationPositions + ".bak");
                    File.Move(_csvLocationPositions, _csvLocationPositions + ".bak");
                }

                MobAdderPlugin.Logger.LogMessage("Writing mob positions to " + _csvLocationPositions);

                using (var f = File.OpenWrite(_csvLocationPositions))
                using (var w = new StreamWriter(f))
                {
                    w.WriteLine("MapNo, PosX, PosY, PosZ, RotX, RotY, RotZ");
                    foreach (var r in _mobPositionData.SelectMany(x =>
                        x.Value.Select(y => new { no = x.Key, pos = y.Key, rot = y.Value.eulerAngles })))
                        w.WriteLine(
                            $"{r.no}, {r.pos.x:F2}, {r.pos.y:F2}, {r.pos.z:F2}, {r.rot.x:F2}, {r.rot.y:F2}, {r.rot.z:F2}");
                }

                if (!Input.GetKey(KeyCode.LeftShift)) return;

                if (File.Exists(_csvLocationSpread))
                {
                    File.Delete(_csvLocationSpread + ".bak");
                    File.Move(_csvLocationSpread, _csvLocationSpread + ".bak");
                }

                MobAdderPlugin.Logger.LogMessage("Writing mob spread to " + _csvLocationSpread);

                using (var f = File.OpenWrite(_csvLocationSpread))
                using (var w = new StreamWriter(f))
                {
                    w.WriteLine($"MapNo, {string.Join(", ", Enum.GetNames(typeof(Cycle.Type)))}");

                    foreach (var r in _mobSpreadData)
                        w.WriteLine($"{r.Key}, {string.Join(", ", r.Value.Select(x => x.ToString("F2")).ToArray())}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                throw;
            }
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
                var r = Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }

        public static IEnumerator SpawnMobs(int mapNo, string mapName)
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
            foreach (var mob in _spawnedMobs) Object.Destroy(mob.Object);
            _spawnedMobs.Clear();

            if (mapNo < 0) yield break;

            if (!Manager.Config.AddData.mobVisible || MobAdderPlugin.MobAmountModifier.Value <= 0) yield break;

            if (_mobPositionData.TryGetValue(mapNo, out var list))
            {
                if (_mobTemplate == null)
                {
                    _mobTemplate = CreateTemplate();
                    // Allow for template to finish creating
                    yield return null;
                }

                _mobColorProperty.SetColor(ChaShader._Color, Manager.Config.AddData.mobColor);

                MobAdderPlugin.Logger.LogDebug($"Spawning {list.Count} mobs on map {mapName}");

                var amount = 1f;
                var cycle = Game.Instance.actScene.Cycle;
                if (_mobSpreadData.TryGetValue(mapNo, out var spreads))
                    amount = spreads[(int)cycle.nowType];

                if (cycle.nowWeek == Cycle.Week.Saturday || cycle.nowWeek == Cycle.Week.Holiday)
                    amount *= 0.3f;

                // Choose a different amount of mobs based on the time of day and some random spread
                var mobCount =
                    (int)(list.Count * amount * Random.Range(0.5f, 1.2f) * MobAdderPlugin.MobAmountModifier.Value);
                if (mobCount > 0)
                {
                    // Choose random mob positions
                    Shuffle(list);
                    foreach (var entry in list.Take(mobCount))
                        SpawnMob(entry.Key, entry.Value, false, mapNo);
                }
            }
        }

        public static void AddMobPosition(int no, Vector3 pos, Quaternion rot)
        {
            if (!_mobPositionData.TryGetValue(no, out var list))
            {
                list = new List<KeyValuePair<Vector3, Quaternion>>();
                _mobPositionData.Add(no, list);
            }

            list.Add(new KeyValuePair<Vector3, Quaternion>(pos, rot));
        }

        public static void RemoveClosestMob(int no, Vector3 position)
        {
            if (_mobPositionData.TryGetValue(no, out var list))
            {
                // Remove closest mob
                var toRemove = list.OrderBy(x => Vector3.Distance(x.Key, position)).Take(1).ToArray();
                if (toRemove.Length == 1)
                {
                    list.Remove(toRemove[0]);
                    var spawned = _spawnedMobs.FirstOrDefault(x => x.Object.transform.position == toRemove[0].Key);
                    Object.Destroy(spawned?.Object);
                }

                MobAdderPlugin.Logger.LogMessage("Removed mob at " + toRemove[0].Key);
            }
        }
    }
}