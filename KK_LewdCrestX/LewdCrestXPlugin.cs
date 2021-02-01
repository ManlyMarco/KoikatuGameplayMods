using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ActionGame;
using ActionGame.Chara;
using ActionGame.Communication;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Extensions;
using JetBrains.Annotations;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Utilities;
using KoiSkinOverlayX;
using StrayTech;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KK_LewdCrestX
{
    [BepInPlugin(GUID, "LewdCrestX", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(KKABMX_Core.GUID, "4.0")]
    [BepInDependency(KoiSkinOverlayMgr.GUID, "5.2")]
    [BepInDependency("KK_Pregnancy", BepInDependency.DependencyFlags.SoftDependency)]
    public class LewdCrestXPlugin : BaseUnityPlugin
    {
        public const string GUID = "LewdCrestX";
        public const string Version = "1.0";

        internal static new ManualLogSource Logger;
        private MakerText _descTxtControl;
        private Harmony _hi;
        private Sprite _iconOff, _iconOn;
        private ConfigEntry<bool> _confUnlockStoryMaker;

        public static Dictionary<CrestType, CrestInfo> CrestInfos { get; } = new Dictionary<CrestType, CrestInfo>();

        private void OnDestroy()
        {
            _hi?.UnpatchSelf();
        }

        private static void GetMilkAmountPatch(CharaCustomFunctionController controller, ref float __result)
        {
            if (__result < 1f && controller != null)
            {
                if (LewdCrestXPlugin.GetCurrentCrest(controller.ChaControl) == CrestType.lactation)
                    __result = 1f;
            }
        }
        //todo hook only when entering story mode?
        private void Start()
        {
            Logger = base.Logger;

            _confUnlockStoryMaker = Config.Bind("Gameplay", "Allow changing crests in story mode character maker", false,
                "If false, to change crests inside story mode you have to invite the character to the club and use the crest icon in clubroom.");


            _hi = Harmony.CreateAndPatchAll(typeof(TalkHooks), GUID);
            _hi.PatchAll(typeof(HsceneHooks));
            _hi.PatchAll(typeof(CharacterHooks));

            var lactType = Type.GetType("KK_Pregnancy.LactationController, KK_Pregnancy", false);
            if (lactType != null)
            {
                var lactDataType = lactType.GetNestedType("CharaData", AccessTools.all);
                if (lactDataType != null)
                {
                    var milkAmountMethod = lactDataType.GetMethod("GetMilkAmount", AccessTools.all);
                    if (milkAmountMethod != null)
                        _hi.Patch(milkAmountMethod, postfix: new HarmonyMethod(typeof(LewdCrestXPlugin), nameof(LewdCrestXPlugin.GetMilkAmountPatch)));
                    else
                        Logger.LogWarning("Could not find KK_Pregnancy.LactationController.CharaData.GetMilkAmount - something isn't right, please report this");
                }
                else
                    Logger.LogWarning("Could not find KK_Pregnancy.LactationController.CharaData - something isn't right, please report this");
            }
            else
                Logger.LogWarning("Could not find KK_Pregnancy.LactationController, some features might not work until you install KK_Pregnancy (please report this if you do have latest version of KK_Pregnancy installed)");

            ////var mat = new Material(Shader.Find("Standard"));
            ////ChaControl.rendBody.materials = ChaControl.rendBody.materials.Where(x => x != mat).AddItem(mat).ToArray();
            //
            var resource = ResourceUtils.GetEmbeddedResource("crests");
            var bundle = AssetBundle.LoadFromMemory(resource);
            DontDestroyOnLoad(bundle);

            // todo on demand
            _iconOff = (bundle.LoadAsset<Texture2D>("action_icon_crest_off") ?? throw new Exception("asset not found - action_icon_crest_off")).ToSprite();
            _iconOn = (bundle.LoadAsset<Texture2D>("action_icon_crest_on") ?? throw new Exception("asset not found - action_icon_crest_on")).ToSprite();
            DontDestroyOnLoad(_iconOff);
            DontDestroyOnLoad(_iconOn);

            GameAPI.RegisterExtraBehaviour<LewdCrestXGameController>(GUID);

            var textAsset = bundle.LoadAsset<TextAsset>("crestinfo");
            var infoText = textAsset.text;
            Destroy(textAsset);

            var xd = XDocument.Parse(infoText);
            // ReSharper disable PossibleNullReferenceException
            var infoElements = xd.Root.Elements("Crest");
            var crestInfos = infoElements
                .Select(x => new CrestInfo(
                    x.Element("ID").Value,
                    x.Element("Name").Value,
                    x.Element("Description").Value,
                    bool.Parse(x.Element("Implemented").Value),
                    bundle));
            // ReSharper restore PossibleNullReferenceException
            foreach (var crestInfo in crestInfos)
            {
                Logger.LogDebug("Added implemented crest - " + crestInfo.Id);
                CrestInfos.Add(crestInfo.Id, crestInfo);
            }

            CharacterApi.RegisterExtraBehaviour<LewdCrestXController>(GUID);

            if (StudioAPI.InsideStudio)
            {
                //todo
            }
            else
            {
                MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
                MakerAPI.MakerFinishedLoading += MakerAPIOnMakerFinishedLoading;
            }
        }

        private void MakerAPIOnMakerFinishedLoading(object sender, EventArgs e)
        {
            _descTxtControl.ControlObject.GetComponent<LayoutElement>().minHeight = 80;
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            var category = new MakerCategory(MakerConstants.Parameter.ADK.CategoryName, "Crest");
            e.AddSubCategory(category);

            if (!_confUnlockStoryMaker.Value && MakerAPI.IsInsideClassMaker())
            {
                _descTxtControl = e.AddControl(new MakerText("To change crests inside story mode you have to invite the character to the club and use the crest icon in clubroom. You can also disable this limitation in plugin settings.", category, this));
            }
            else
            {
                e.AddControl(new MakerText("Crests with the [+] tag will change gameplay in story mode.", category, this) { TextColor = MakerText.ExplanationGray });
                
                var infos = CrestInfos.Values.ToList();
                var crests = new[] { "None" }.Concat(infos.Select(x => x.Implemented ? "[+] " + x.Name : x.Name)).ToArray();

                var dropdownControl = e.AddControl(new MakerDropdown("Crest type", crests, category, 0, this));
                dropdownControl.BindToFunctionController<LewdCrestXController, int>(
                    controller => infos.FindIndex(info => info.Id == controller.CurrentCrest) + 1,
                    (controller, value) => controller.CurrentCrest = value <= 0 ? CrestType.None : infos[value - 1].Id);

                _descTxtControl = e.AddControl(new MakerText("Description", category, this));
                var implementedTxtControl = e.AddControl(new MakerText("", category, this));
                e.AddControl(new MakerText("The crests were created by novaksus on pixiv", category, this) { TextColor = MakerText.ExplanationGray });
                dropdownControl.ValueChanged.Subscribe(value =>
                {
                    if (value <= 0)
                    {
                        _descTxtControl.Text = "No crest selected, no effects applied";
                        implementedTxtControl.Text = "";
                    }
                    else
                    {
                        var crestInfo = infos[value - 1];
                        _descTxtControl.Text = crestInfo.Description;
                        implementedTxtControl.Text = crestInfo.Implemented
                            ? "This crest will affect gameplay in story mode as described"
                            : "This crest is only for looks (it might be implemented in the future with modified lore)";
                    }
                });
            }
        }


        //private static LewdCrestXController GetController(SaveData.Heroine heroine) => GetController(heroine?.chaCtrl);
        //private static LewdCrestXController GetController(ChaControl chaCtrl) => chaCtrl != null ? chaCtrl.GetComponent<LewdCrestXController>() : null;
        public static CrestType GetCurrentCrest(SaveData.Heroine heroine)
        {
            //todo maybe cache too
            //var ctrl = GetController(heroine);
            //return ctrl == null ? CrestType.None : ctrl.CurrentCrest;
            return CrestType.liberated;
        }
        public static CrestType GetCurrentCrest(ChaControl chaCtrl)
        {
            //todo
            //var ctrl = GetController(chaCtrl);
            //return ctrl == null ? CrestType.None : ctrl.CurrentCrest;
            return CrestType.liberated;
        }
    }

    public sealed class LewdCrestXGameController : GameCustomFunctionController
    {
        protected override void OnDayChange(Cycle.Week day)
        {
            //todo use cached? might not detect all
            foreach (var controller in GameObject.FindObjectsOfType<LewdCrestXController>())
            {
                if (controller.CurrentCrest == CrestType.restore)
                {
                    var heroine = controller.Heroine;
                    if (heroine != null)
                    {
                        LewdCrestXPlugin.Logger.LogDebug("Resetting heroine to virgin because of restore crest: " + heroine.charFile.parameter.fullname);
                        heroine.isVirgin = true;
                        heroine.hCount = 0;
                    }
                }
            }
        }
    }

    internal static class CharacterHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.notBra), MethodType.Setter)]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.notShorts), MethodType.Setter)]
        private static void notBraOverride(ChaControl __instance, ref bool value)
        {
            Console.WriteLine("notBraOverride");
            if (LewdCrestXPlugin.GetCurrentCrest(__instance) == CrestType.liberated)
            {
                // Force underwear to be off
                value = true;
            }
        }

        private static readonly ChaFileParameter.Denial _noDenial = new ChaFileParameter.Denial { aibu = true, anal = true, kiss = true, massage = true, notCondom = true };
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveData.CharaData), nameof(SaveData.CharaData.denial), MethodType.Getter)]
        public static void DenialOverride(SaveData.CharaData __instance, ref ChaFileParameter.Denial __result)
        {
            // todo is this fast enough? cache upstream?
            var commandcrest = LewdCrestXPlugin.GetCurrentCrest(__instance as SaveData.Heroine) == CrestType.command;
            if (commandcrest) __result = _noDenial;
        }
    }

    internal static class HsceneHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddAibuOrg))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
        public static void OnOrg(HFlag __instance)
        {
            var h = __instance.GetLeadingHeroine();
            var c = LewdCrestXPlugin.GetCurrentCrest(h);
            switch (c)
            {
                case CrestType.mindmelt:
                    // This effect makes character slowly forget things on every org
                    h.favor = Mathf.Clamp(h.favor - 10, 0, 100);
                    h.intimacy = Mathf.Clamp(h.intimacy - 8, 0, 100);

                    h.anger = Mathf.Clamp(h.anger - 10, 0, 100);
                    if (h.anger == 0) h.isAnger = false;

                    if (Random.value < 0.2f) h.isDate = false;

                    // In exchange they get lewder
                    h.lewdness = Mathf.Clamp(h.lewdness + 30, 0, 100);

                    var orgCount = __instance.GetOrgCount();
                    if (orgCount >= 2)
                    {
                        if (h.favor == 0 && h.intimacy == 0)
                        {
                            h.isGirlfriend = false;
                            if (Random.value < 0.2f) h.confessed = false;
                        }

                        if (h.isKiss && Random.value < 0.1f) h.isKiss = false;
                        else if (!h.isAnalVirgin && Random.value < 0.1f) h.isAnalVirgin = true;
                        else if (Random.value < 0.3f + orgCount / 10f)
                        {
                            // Remove a random seen event so she acts like it never happened
                            var randomEvent = h.talkEvent.GetRandomElement();
                            var isMeetingEvent = randomEvent == 0 || randomEvent == 1;
                            if (isMeetingEvent)
                            {
                                if (h.talkEvent.Count <= 2)
                                    h.talkEvent.Clear();
                            }
                            else
                            {
                                h.talkEvent.Remove(randomEvent);
                            }
                        }
                    }

                    break;
            }
        }
    }

    internal static class TalkHooks
    {

        private static CrestType _currentCrestType;
        private static bool _isHEvent;
        private static PassingInfo _currentPassingInfo;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Info), nameof(Info.GetEventADV))]
        static void GetEventADVPrefix(Info __instance, int _command, PassingInfo ____passingInfo)
        {
            _currentCrestType = LewdCrestXPlugin.GetCurrentCrest(____passingInfo.heroine);
            _currentPassingInfo = ____passingInfo;
            Console.WriteLine("GetEventADVPrefix " + _currentCrestType);
            _isHEvent = _command == 3;
        }
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Info), nameof(Info.GetEventADV))]
        static void GetEventADVFinalizer()
        {
            Console.WriteLine("GetEventADVFinalizer " + _currentCrestType);
            _currentCrestType = CrestType.None;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TalkScene), "UpdateUI")]
        //private void UpdateUI(bool _gauge = false)
        static void UpdateUIPrefix(TalkScene __instance)
        {
            _currentCrestType = LewdCrestXPlugin.GetCurrentCrest(__instance.targetHeroine);
            Console.WriteLine("UpdateUIPrefix " + _currentCrestType);
            _isHEvent = false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(TalkScene), "UpdateUI")]
        static void UpdateUIFinalizer(TalkScene __instance, Button[] ___buttonEventContents)
        {
            Console.WriteLine("UpdateUIFinalizer " + _currentCrestType);
            if (_currentCrestType == CrestType.libido)
            {
                // 3 is lets have h
                ___buttonEventContents[3].gameObject.SetActiveIfDifferent(true);
            }

            _currentCrestType = CrestType.None;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Info), "GetStage")]
        //private int GetStage()
        static void GetStagePatch(ref int __result)
        {
            Console.WriteLine("GetStagePatch " + _currentCrestType);
            switch (_currentCrestType)
            {
                case CrestType.libido:
                    if (_isHEvent) __result = 2;
                    break;
                case CrestType.command:
                    if (__result == 0) __result = 1;
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Info), "RandomBranch")]
        //private int RandomBranch(params int[] _values)
        static void RandomBranchPatch(ref int __result)
        {
            Console.WriteLine("RandomBranchPatch " + _currentCrestType);
            switch (_currentCrestType)
            {
                case CrestType.libido:
                    if (_isHEvent) __result = 0;
                    break;
                case CrestType.command:
                    __result = 0;
                    break;
                case CrestType.liberated:
                    if (_isHEvent && _currentPassingInfo.isOtherPeople) __result = 0;
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PassingInfo), "isHPossible", MethodType.Getter)]
        static void isHPossiblePatch(ref bool __result, PassingInfo __instance)
        {
            var crest = _currentCrestType;
            if (_currentCrestType == CrestType.None) crest = LewdCrestXPlugin.GetCurrentCrest(__instance.heroine);
            Console.WriteLine("isHPossiblePatch " + _currentCrestType);

            switch (crest)
            {
                case CrestType.command:
                case CrestType.libido:
                case CrestType.liberated:
                    __result = true;
                    break;
            }
        }
    }

    sealed class LewdCrestXBoneModifier : BoneEffect
    {
        private readonly LewdCrestXController _controller;

        // todo might cause issues if in future abmx holds on to the bone modifiers since we reuse them for all characters
        private static readonly Dictionary<string, KeyValuePair<Vector3, BoneModifierData>> _vibrancyBoneModifiers;
        private static readonly string[] _vibrancyBones;
        private float _previousVibRatio;

        private static readonly Dictionary<string, BoneModifierData> _lactationModifiers =
            new Dictionary<string, BoneModifierData>
            {
                {"cf_d_bust01_L" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_d_bust01_R" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_d_bnip01_L" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_d_bnip01_R" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
            };
        private static readonly string[] _lactationBones;

        public LewdCrestXBoneModifier(LewdCrestXController controller)
        {
            _controller = controller;
        }

        static LewdCrestXBoneModifier()
        {
            var vibDict = new Dictionary<string, Vector3>
            {
                {"cf_d_bust01_L", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_d_bust01_R", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_d_bnip01_L", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_d_bnip01_R", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_bnip02_L", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_s_bnip02_R", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_s_siri_L", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_s_siri_R", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_s_waist01", new Vector3(0.9f, 0.9f, 0.9f)},
                {"cf_s_waist02", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh01_L", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh01_R", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh02_L", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh02_R", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh03_L", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh03_R", new Vector3(1.1f, 1.1f, 1.1f)},
            };
            _vibrancyBones = vibDict.Keys.ToArray();
            _vibrancyBoneModifiers = vibDict.ToDictionary(
                x => x.Key,
                x => new KeyValuePair<Vector3, BoneModifierData>(x.Value, new BoneModifierData(x.Value, 1)));

            _lactationBones = _lactationModifiers.Keys.ToArray();
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            switch (_controller.CurrentCrest)
            {
                case CrestType.vibrancy:
                    return _vibrancyBones;
                case CrestType.lactation:
                    return _lactationBones;
                default:
                    return Enumerable.Empty<string>();
            }
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, ChaFileDefine.CoordinateType coordinate)
        {
            switch (_controller.CurrentCrest)
            {
                case CrestType.vibrancy:
                    if (_vibrancyBoneModifiers.TryGetValue(bone, out var kvp))
                    {
                        var vibMod = kvp.Value;
                        if (_controller.Heroine != null)
                        {
                            // Effect increases the lewder the character is
                            var vibRatio = _controller.Heroine.lewdness / 120f + (int)_controller.Heroine.HExperience * 0.1f;
                            if (vibRatio != _previousVibRatio)
                            {
                                vibRatio = Mathf.MoveTowards(_previousVibRatio, vibRatio, Time.deltaTime / 10);
                                _previousVibRatio = vibRatio;
                            }
                            vibMod.ScaleModifier = Vector3.Lerp(Vector3.one, kvp.Key, vibRatio);
                        }
                        else
                        {
                            // If outside of main game always set to max
                            vibMod.ScaleModifier = kvp.Key;
                        }
                        return vibMod;
                    }
                    return null;
                case CrestType.lactation:
                    return _lactationModifiers.TryGetValue(bone, out var lactMod) ? lactMod : null;
                default:
                    return null;
            }
        }
    }

    public class LewdCrestXController : CharaCustomFunctionController
    {
        private CrestType _currentCrest;
        private KoiSkinOverlayController _overlayCtrl;
        private BoneController _boneCtrl;

        internal SaveData.Heroine Heroine { get; private set; }
        //private NPC _npc;

        public CrestType CurrentCrest
        {
            get => _currentCrest;
            set
            {
                if (_currentCrest != value)
                {
                    _currentCrest = value;
                    ApplyCrestTexture();
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            _boneCtrl = GetComponent<BoneController>() ?? throw new Exception("Missing BoneController");
            _boneCtrl.AddBoneEffect(new LewdCrestXBoneModifier(this));
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            // todo save null if default
            var data = new PluginData();
            data.data[nameof(CurrentCrest)] = CurrentCrest;
            SetExtendedData(data);
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            // todo better handling
            var data = GetExtendedData()?.data;
            if (data != null && data.TryGetValue(nameof(CurrentCrest), out var cr))
            {
                try
                {
                    CurrentCrest = (CrestType)cr;
                }
                catch (Exception e)
                {
                    LewdCrestXPlugin.Logger.LogError(e);
                    CurrentCrest = CrestType.None;
                }
            }

            Heroine = ChaControl.GetHeroine();
            //if (_heroine != null) _npc = ChaControl.transform.parent?.GetComponent<NPC>(););

            if (Heroine != null)
            {
                var actCtrl = Manager.Game.Instance.actScene.actCtrl;
                switch (CurrentCrest)
                {
                    case CrestType.libido:
                        Heroine.lewdness = 100;
                        actCtrl.AddDesire(4, Heroine, 50); //want to mast
                        actCtrl.AddDesire(5, Heroine, 60); //want to h
                        actCtrl.AddDesire(26, Heroine, 40); //les
                        actCtrl.AddDesire(27, Heroine, 40); //les
                        actCtrl.AddDesire(29, Heroine, 100); //ask for h
                        break;

                    case CrestType.liberated:
                        Heroine.lewdness = Mathf.Min(100, Heroine.lewdness + 20);
                        actCtrl.AddDesire(4, Heroine, 50); //want to mast
                        break;
                }
            }
        }

        //protected override void Update()
        //{
        //    base.Update();
        //
        //    // todo reduce run rate?
        //}

        private void ApplyCrestTexture()
        {
            if (_overlayCtrl == null)
                _overlayCtrl = GetComponent<KoiSkinOverlayController>() ?? throw new Exception("Missing KoiSkinOverlayController");

            var any = _overlayCtrl.AdditionalTextures.RemoveAll(texture => ReferenceEquals(texture.Tag, this)) > 0;

            if (CurrentCrest > CrestType.None)
            {
                if (LewdCrestXPlugin.CrestInfos.TryGetValue(CurrentCrest, out var info))
                {
                    var tex = new AdditionalTexture(info.GetTexture(), TexType.BodyOver, this, 1010);
                    _overlayCtrl.AdditionalTextures.Add(tex);
                    any = true;
                }
                else
                {
                    LewdCrestXPlugin.Logger.LogWarning($"Unknown crest type \"{CurrentCrest}\", resetting to no crest");
                    CurrentCrest = CrestType.None;
                }
            }

            if (any) _overlayCtrl.UpdateTexture(TexType.BodyOver);
        }
    }
}