using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using BepInEx;
using ExtensibleSaveFormat;
using Harmony;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using Manager;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KK_Pregnancy
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(KKABMX_Core.GUID)]
    [BepInDependency(KoikatuAPI.GUID)]
    public class PregnancyPlugin : BaseUnityPlugin
    {
        private static MakerToggle _gameplayToggle;
        private static MakerSlider _weeksSlider;
        private static MakerSlider _fertilityToggle;

        public const string GUID = "KK_Pregnancy";
        internal const string Version = "0.5";

        public static readonly float DefaultFertility = 0.3f;

        /// <summary>
        /// Week at which pregnancy reaches max level and the girl leaves school
        /// </summary>
        public static readonly int LeaveSchoolWeek = 41;

        /// <summary>
        /// Week at which pregnancy ends and the girl returns to school
        /// </summary>
        public static readonly int ReturnToSchoolWeek = LeaveSchoolWeek + 7;

        [DisplayName("Pregnancy progression speed")]
        [Description("How much faster does the in-game pregnancy progresses than the standard 40 weeks.\n\n" +
                     "x1 is 40 weeks, x2 is 20 weeks, x4 is 10 weeks, x10 is 4 weeks.")]
        [AcceptableValueList(new object[] { 1, 2, 4, 10 })]
        public static ConfigWrapper<int> PregnancyProgressionSpeed { get; private set; }

        [DisplayName("Enable conception")]
        [Description("If disabled no new characters will be able to get pregnant. Doesn't affect pregnant characters.")]
        public static ConfigWrapper<bool> ConceptionEnabled { get; private set; }

        private static class Hooks
        {
            public static void InitHooks()
            {
                var hi = HarmonyInstance.Create(GUID);
                hi.PatchAll(typeof(Hooks));

                PatchNPCLoadAll(hi, new HarmonyMethod(typeof(Hooks), nameof(NPCLoadAllTpl)));
            }

            private static void PatchNPCLoadAll(HarmonyInstance instance, HarmonyMethod transpiler)
            {
                var t = typeof(ActionScene).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name.StartsWith("<NPCLoadAll>c__Iterator"));
                var m = t.GetMethod("MoveNext");
                instance.Patch(m, null, null, transpiler);
            }

            public static IEnumerable<CodeInstruction> NPCLoadAllTpl(IEnumerable<CodeInstruction> instructions)
            {
                var target = AccessTools.Property(typeof(Game), nameof(Game.HeroineList)).GetGetMethod();
                var customFilterM = AccessTools.Method(typeof(Hooks), nameof(GetFilteredHeroines));
                foreach (var instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.operand == target)
                    {
                        // Grab the return of get_HeroineList and process it
                        yield return new CodeInstruction(OpCodes.Call, customFilterM);
                    }
                }
            }

            private static List<SaveData.Heroine> GetFilteredHeroines(List<SaveData.Heroine> originalList)
            {
                var filteredHeroines = originalList.Where(CanGetSpawned).ToList();
                return filteredHeroines;
            }

            private static bool CanGetSpawned(SaveData.Heroine heroine)
            {
                var data = ExtendedSave.GetExtendedDataById(heroine.charFile, GUID);
                if (data == null)
                    return true;

                ParseData(data, out var week, out var gameplayEnabled, out var _);
                if (gameplayEnabled && week >= LeaveSchoolWeek)
                    return false;

                return true;
            }
        }

        private void Awake()
        {
            Hooks.InitHooks();

            PregnancyProgressionSpeed = new ConfigWrapper<int>(nameof(PregnancyProgressionSpeed), this, 4);
            ConceptionEnabled = new ConfigWrapper<bool>(nameof(ConceptionEnabled), this, true);

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;

            CharacterApi.RegisterExtraBehaviour<PregnancyController>(GUID);
            // todo add when slider is implemented
            //StudioAPI.CreateCurrentStateCategory(new CurrentStateCategory("Pregnancy", new CurrentStateCategorySubItemBase[]{new CurrentStateCategorySlider()}));
        }

        private void MakerAPI_MakerBaseLoaded(object sender, RegisterCustomControlsEvent e)
        {
            var cat = MakerConstants.Parameter.Character;

            _gameplayToggle = e.AddControl(new MakerToggle(cat, "Enable pregnancy progression", true, this));
            _gameplayToggle.ValueChanged.Subscribe(val => GetController().GameplayEnabled = val);

            _fertilityToggle = e.AddControl(new MakerSlider(cat, "Fertility", 0, 1, DefaultFertility, this));
            _fertilityToggle.ValueChanged.Subscribe(val => GetController().Fertility = val);

            e.AddControl(new MakerText("How likely is this character to get pregnant.", cat, this) { TextColor = Color.gray });

            _weeksSlider = e.AddControl(new MakerSlider(cat, "Week of pregnancy", 0, LeaveSchoolWeek - 1, 0, this));
            _weeksSlider.ValueToString = f => Mathf.RoundToInt(f).ToString();
            _weeksSlider.ValueChanged.Subscribe(val => GetController().Week = Mathf.RoundToInt(val));

            e.AddControl(new MakerText("If the character is pregnant when added to the game, the pregnancy will continue from this point, unless \"Enable pregnancy progression\" is turned off.", cat, this) { TextColor = Color.gray });
        }

        private static PregnancyController GetController()
        {
            return MakerAPI.GetCharacterControl().GetComponent<PregnancyController>();
        }

        internal static void UpdateInterface(PregnancyController controller)
        {
            if (MakerAPI.InsideMaker)
            {
                if (_gameplayToggle != null) _gameplayToggle.Value = controller.GameplayEnabled;
                if (_fertilityToggle != null) _fertilityToggle.Value = controller.Fertility;
                if (_weeksSlider != null) _weeksSlider.Value = controller.Week;
            }
        }

        public static void ParseData(PluginData data, out int week, out bool gameplayEnabled, out float fertility)
        {
            week = 0;
            gameplayEnabled = true;
            fertility = DefaultFertility;

            if (data?.data == null) return;

            if (data.data.TryGetValue("Week", out var value) && value is int w)
                week = w;

            if (data.data.TryGetValue("GameplayEnabled", out var value2) && value2 is bool g)
                gameplayEnabled = g;

            if (data.data.TryGetValue("Fertility", out var value3) && value3 is float f)
                fertility = f;
        }

        public static PluginData WriteData(int week, bool gameplayEnabled, float fertility)
        {
            if (week <= 0 && gameplayEnabled && !Mathf.Approximately(fertility, DefaultFertility)) return null;

            var data = new PluginData();
            data.data["Week"] = week;
            data.data["GameplayEnabled"] = gameplayEnabled;
            data.data["Fertility"] = fertility;
            return data;
        }
    }
    public class PregnancyController : CharaCustomFunctionController
    {
        private PregnancyBoneEffect _boneEffect;
        private bool _gameplayEnabled;
        private int _week;
        private float _fertility;

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (maintainState) return;

            ReadSavedData();

            if (_boneEffect == null)
                _boneEffect = new PregnancyBoneEffect(this);

            GetComponent<BoneController>().AddBoneEffect(_boneEffect);

            PregnancyPlugin.UpdateInterface(this);
        }

        public void ReadSavedData()
        {
            var data = GetExtendedData();
            PregnancyPlugin.ParseData(data, out _week, out _gameplayEnabled, out _fertility);
        }

        /// <summary>
        /// If 0 or negative, the character is not pregnant.
        /// If between 0 and <see cref="PregnancyPlugin.LeaveSchoolWeek"/> the character is pregnant and the belly is proportionately sized.
        /// If equal or above <see cref="PregnancyPlugin.LeaveSchoolWeek"/> the character is on a maternal leave until <see cref="PregnancyPlugin.ReturnToSchoolWeek"/>.
        /// </summary>
        public int Week
        {
            get => _week;
            set => _week = value;
        }

        /// <summary>
        /// Should any gameplay code be executed for this character.
        /// If false the current pregnancy week doesn't change and the character can't get pregnant.
        /// </summary>
        public bool GameplayEnabled
        {
            get => _gameplayEnabled;
            set => _gameplayEnabled = value;
        }

        /// <summary>
        /// The character is harder to get pregnant.
        /// </summary>
        public float Fertility
        {
            get => _fertility;
            set => _fertility = value;
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            SetExtendedData(PregnancyPlugin.WriteData(Week, GameplayEnabled, Fertility));
        }

        public float GetBellySizePercent()
        {
            // Don't show any effect at week 1 since it begins right after winning a child lottery
            return Mathf.Clamp01((Week - 1f) / (PregnancyPlugin.LeaveSchoolWeek - 1f));
        }

        public bool IsDuringPregnancy()
        {
            return Week > 0;
        }

        public void StartPregnancy()
        {
            if (!IsDuringPregnancy())
            {
                Week = 1;
                OnCardBeingSaved(GameMode.Unknown);
            }
        }
    }

    public class PregnancyManager : GameCustomFunctionController
    {
        protected override void OnDayChange(Cycle.Week day)
        {
            // Use monday for weekly stuff because it is always triggered (alternative would be sunday/saturday)
            if (day == Cycle.Week.Monday)
            {
                // At start of each week increase pregnancy week counters of all pregnant characters
                foreach (var heroine in Game.Instance.HeroineList)
                {
                    var data = ExtendedSave.GetExtendedDataById(heroine.charFile, PregnancyPlugin.GUID);
                    if (data != null)
                    {
                        PregnancyPlugin.ParseData(data, out var week, out var gameplayEnabled, out var lowFertility);
                        // Advance the week of pregnancy. If week is 0 the character is not pregnant
                        if (gameplayEnabled && week > 0)
                        {
                            if (week < PregnancyPlugin.LeaveSchoolWeek)
                            {
                                // Advance through in-school at full configured speed
                                var weekChange = PregnancyPlugin.PregnancyProgressionSpeed.Value;
                                week = Mathf.Min(PregnancyPlugin.LeaveSchoolWeek, week + weekChange);

                                // Force the girl to always be on the safe day, happens every day after day of conception
                                HFlag.SetMenstruation(heroine, HFlag.MenstruationType.安全日);
                            }
                            else if (week < PregnancyPlugin.ReturnToSchoolWeek)
                            {
                                // Make sure at least one week is spent out of school
                                var weekChange = Mathf.Min(PregnancyPlugin.ReturnToSchoolWeek - PregnancyPlugin.LeaveSchoolWeek - 1, PregnancyPlugin.PregnancyProgressionSpeed.Value);
                                week = week + weekChange;
                            }

                            if (week >= PregnancyPlugin.ReturnToSchoolWeek)
                                week = 0;

                            ExtendedSave.SetExtendedDataById(heroine.charFile, PregnancyPlugin.GUID, PregnancyPlugin.WriteData(week, true, lowFertility));
                            // If controller exists then update its values so it doesn't overwrite them when saving
                            heroine.chaCtrl?.GetComponent<PregnancyController>()?.ReadSavedData();
                        }
                    }
                }
            }
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            if (!PregnancyPlugin.ConceptionEnabled.Value) return;

            // Don't know which girl player came inside
            if (proc.flags.mode == HFlag.EMode.houshi3P || proc.flags.mode == HFlag.EMode.sonyu3P) return;

            var heroine = proc.flags.lstHeroine.First(x => x != null);
            var isDangerousDay = HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日;
            var cameInside = proc.flags.count.sonyuInside > 0;
            if (isDangerousDay && cameInside)
            {
                var controller = heroine.chaCtrl.GetComponent<PregnancyController>();
                if (controller == null) throw new ArgumentNullException(nameof(controller));

                if (!controller.GameplayEnabled || controller.IsDuringPregnancy()) return;

                var winThreshold = Mathf.RoundToInt(controller.Fertility * 100);
                var childLottery = Random.Range(1, 100);
                var wonAChild = winThreshold >= childLottery;
                if (wonAChild)
                    controller.StartPregnancy();
            }
        }
    }

    public class PregnancyBoneEffect : BoneEffect
    {
        private readonly Dictionary<string, BoneModifierData> _pregnancyFullValues = new Dictionary<string, BoneModifierData>
        {
            // Belly
            {"cf_s_spine01", new BoneModifierData(new Vector3(1.45f, 1.4f, 1.8f), -4f) },
            {"cf_s_waist01", new BoneModifierData(new Vector3(1.2f, 1.28f, 1.6f), -3.5f) },
            // Skirt Front
            {"cf_d_sk_00_00", new BoneModifierData(new Vector3(1.35f, 1f, 1f), 1.95f) },
            // Skirt Front sides
            {"cf_d_sk_07_00", new BoneModifierData(new Vector3(2f, 1f, 1f), 1.5f) },
            {"cf_d_sk_01_00", new BoneModifierData(new Vector3(2f, 1f, 1f), 1.5f) },
            // Skirt Back
            {"cf_d_sk_04_00", new BoneModifierData(new Vector3(1f, 1f, 1f), 1.1f) },
            // Breasts
            {"cf_d_bust01_L", new BoneModifierData(new Vector3(1.15f, 1.15f, 1.15f), 1f) },
            {"cf_d_bust01_R", new BoneModifierData(new Vector3(1.15f, 1.15f, 1.15f), 1f) },
            // Areolas
            {"cf_s_bnip01_L", new BoneModifierData(new Vector3(1.2f, 1.2f, 1f), 1f)},
            {"cf_s_bnip01_R", new BoneModifierData(new Vector3(1.2f, 1.2f, 1f), 1f) },
            // Nipples
            {"cf_d_bnip01_L", new BoneModifierData(new Vector3(1.2f, 1.2f, 1.2f), 1f)},
            {"cf_d_bnip01_R", new BoneModifierData(new Vector3(1.2f, 1.2f, 1.2f), 1f) },
        };

        private readonly PregnancyController _controller;

        public PregnancyBoneEffect(PregnancyController controller)
        {
            _controller = controller;
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            if (_controller.IsDuringPregnancy() || MakerAPI.InsideMaker || StudioAPI.InsideStudio)
                return _pregnancyFullValues.Keys;

            return Enumerable.Empty<string>();
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, ChaFileDefine.CoordinateType coordinate)
        {
            if (_controller.IsDuringPregnancy())
            {
                if (_pregnancyFullValues.TryGetValue(bone, out var mod))
                {
                    var bellySize = _controller.GetBellySizePercent();
                    return new BoneModifierData(
                        new Vector3(
                            Mathf.Lerp(1f, mod.ScaleModifier.x, bellySize),
                            Mathf.Lerp(1f, mod.ScaleModifier.y, bellySize),
                            Mathf.Lerp(1f, mod.ScaleModifier.z, bellySize)),
                        Mathf.Lerp(1f, mod.LengthModifier, bellySize));
                }
            }

            return null;
        }
    }
}
