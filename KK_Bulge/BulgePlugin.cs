using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using KKABMX.Core;
using KKABMX.GUI;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using StrayTech;
using UniRx;
using UnityEngine;

namespace KK_Bulge
{
    [BepInPlugin(GUID, "Bulge in the pants, tent in the woods", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(KKABMX_Core.GUID, "4.2")]
    public class BulgePlugin : BaseUnityPlugin
    {
        public const string GUID = "Bulge";
        public const string Version = "1.0";

        internal static ConfigEntry<float> DefaultBulgeSize;
        internal static ConfigEntry<BulgeEnableLevel> DefaultBulgeState;
        internal static bool DuringH;

        private void Awake()
        {
            DefaultBulgeSize = Config.Bind("Default settings", "Default bulge size", 0.5f,
                new ConfigDescription("Default size of bulges if not specified per-character (in Body/Genitals tab in character maker).", new AcceptableValueRange<float>(0, 1)));
            DefaultBulgeState = Config.Bind("Default settings", "Enable bulge by default", BulgeEnableLevel.Auto,
                "Should bulges be enabled if not specified per-character (in Body/Genitals tab in character maker).\nAuto will only apply bulges to characters with shlogns. Always will apply no matter what, and Never will not apply unless you enable it per-character.");

            CharacterApi.RegisterExtraBehaviour<BulgeController>(GUID);

            CreateInterface();

            GameAPI.StartH += (sender, args) => DuringH = true;
            GameAPI.EndH += (sender, args) => DuringH = false;
        }

        private void CreateInterface()
        {
            if (!StudioAPI.InsideStudio)
            {
                MakerAPI.MakerBaseLoaded += (sender, e) =>
                {
                    var cat = InterfaceData.BodyGenitals;

                    e.AddControl(new MakerRadioButtons(cat, this, "Enable crotch bulge", (int)DefaultBulgeState.Value, "Auto", "Always", "Never"))
                        .BindToFunctionController<BulgeController, int>(controller => (int)controller.EnableBulge, (controller, value) => controller.EnableBulge = (BulgeEnableLevel)value);
                    e.AddControl(new MakerText("Auto will enable the bulge only if the character has a shlong (either male or added with UncensorSelector).\nThe effect is applied only when wearing clothes.", cat, this) { TextColor = MakerText.ExplanationGray });
                    e.AddControl(new MakerSlider(cat, "Bulge size", 0, 1, DefaultBulgeSize.Value, this))
                        .BindToFunctionController<BulgeController, float>(controller => controller.BulgeSize, (controller, value) => controller.BulgeSize = value);
                };
            }
            else
            {
                var category = StudioAPI.GetOrCreateCurrentStateCategory(null);
                category.AddControl(new CurrentStateCategoryDropdown(
                    "Enable crotch bulge", new[] { "Auto", "Always", "Never" },
                    c => (int)c.charInfo.GetComponent<BulgeController>().EnableBulge)).Value.Subscribe(val =>
               {
                   foreach (var controller in StudioAPI.GetSelectedControllers<BulgeController>())
                   {
                       controller.EnableBulge = (BulgeEnableLevel)val;
                   }
               });
                category.AddControl(new CurrentStateCategorySlider("Bulge size",
                    c => c.charInfo.GetComponent<BulgeController>().BulgeSize)).Value.Subscribe(val =>
                {
                    foreach (var controller in StudioAPI.GetSelectedControllers<BulgeController>())
                    {
                        controller.BulgeSize = val;
                    }
                });
            }
        }
    }

    internal class BulgeController : CharaCustomFunctionController
    {
        private BulgeBoneEffect _bulgeBoneEffect;

        public BulgeEnableLevel EnableBulge { get; set; }
        public float BulgeSize { get; set; }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (EnableBulge != BulgePlugin.DefaultBulgeState.Value || !Mathf.Approximately(BulgeSize, BulgePlugin.DefaultBulgeSize.Value))
            {
                var data = new PluginData { version = 1 };
                data.data[nameof(EnableBulge)] = EnableBulge;
                data.data[nameof(BulgeSize)] = BulgeSize;
                SetExtendedData(data);
            }
            else
            {
                SetExtendedData(null);
            }
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            base.OnReload(currentGameMode);

            if (_bulgeBoneEffect == null)
            {
                _bulgeBoneEffect = new BulgeBoneEffect(this);
                var bc = GetComponent<BoneController>();
                bc.AddBoneEffect(_bulgeBoneEffect);
            }

            EnableBulge = BulgePlugin.DefaultBulgeState.Value;
            BulgeSize = BulgePlugin.DefaultBulgeSize.Value;
            var data = GetExtendedData();
            if (data != null)
            {
                if (data.data.TryGetValue(nameof(EnableBulge), out var eb)) EnableBulge = (BulgeEnableLevel)eb;
                if (data.data.TryGetValue(nameof(BulgeSize), out var bs)) BulgeSize = (float)bs;
            }
        }
    }

    internal class BulgeBoneEffect : BoneEffect
    {
        private static readonly string[] _affectedBones = { "cf_j_kokan" };
        private static readonly Vector3 _maxScale = new Vector3(2, 3, 3.3f);
        private static readonly Vector3 _maxPosition = new Vector3(0, -0.040f, 0);
        private static readonly BoneModifierData _boneModifierData = new BoneModifierData(_maxScale, 1, _maxPosition, Vector3.zero);

        private readonly BulgeController _ctrl;
        private readonly GameObject _son;

        public BulgeBoneEffect(BulgeController ctrl)
        {
            if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));
            _ctrl = ctrl;
            // BodyTop/p_cf_body_00/cf_o_root/n_body/n_dankon
            // BodyTop/p_cf_body_00 can be disabled in kkp in some cases, somehow, so need a full scan
            _son = _ctrl.transform.FindChildDeep("n_dankon").gameObject;
        }

        private bool GetBulgeVisible()
        {
            //if (_ctrl == null || _son == null) return false;

            switch (_ctrl.EnableBulge)
            {
                case BulgeEnableLevel.Auto:
                default:
                    if (BulgePlugin.DuringH && !Manager.Config.EtcData.VisibleSon)
                        return false;
                    var status = _ctrl.ChaControl.fileStatus;
                    var bulgeVisible = status.visibleSonAlways && !_son.activeSelf;
                    return bulgeVisible;
                case BulgeEnableLevel.Always:
                    return !_son.activeSelf;
                case BulgeEnableLevel.Never:
                    return false;
            }
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            return GetBulgeVisible() ? _affectedBones : Enumerable.Empty<string>();
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, ChaFileDefine.CoordinateType coordinate)
        {
            if (GetBulgeVisible() && bone == "cf_j_kokan")
            {
                _boneModifierData.ScaleModifier = Vector3.Lerp(Vector3.one, _maxScale, _ctrl.BulgeSize);
                _boneModifierData.PositionModifier = Vector3.Lerp(Vector3.zero, _maxPosition, _ctrl.BulgeSize);
                return _boneModifierData;
            }

            return null;
        }
    }

    internal enum BulgeEnableLevel
    {
        Auto,
        Always,
        Never
    }
}