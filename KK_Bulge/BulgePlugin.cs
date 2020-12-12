using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using ExtensibleSaveFormat;
using KKABMX.Core;
using KKABMX.GUI;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
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

        internal const float DefaultBulgeSize = 0.65f;
        internal const int DefaultBulgeState = 0;

        private void Awake()
        {
            CharacterApi.RegisterExtraBehaviour<BulgeController>(GUID);

            if (!StudioAPI.InsideStudio)
            {
                MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            }
            else
            {
                var category = StudioAPI.GetOrCreateCurrentStateCategory(null);
                category.AddControl(new CurrentStateCategoryDropdown(
                    "Enable crotch bulge", new[] { "Auto", "Always", "Never" },
                    c => c.charInfo.GetComponent<BulgeController>().EnableBulge)).Value.Subscribe(val =>
                {
                    foreach (var controller in StudioAPI.GetSelectedControllers<BulgeController>())
                    {
                        controller.EnableBulge = val;
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

        private void MakerAPI_MakerBaseLoaded(object sender, RegisterCustomControlsEvent e)
        {
            var cat = InterfaceData.BodyGenitals;

            e.AddControl(new MakerRadioButtons(cat, this, "Enable crotch bulge", DefaultBulgeState, "Auto", "Always", "Never"))
                .BindToFunctionController<BulgeController, int>(controller => controller.EnableBulge, (controller, value) => controller.EnableBulge = value);
            e.AddControl(new MakerText("Auto will enable the bulge only if the character has a shlong (either male or added with UncensorSelector).\nThe effect is applied only when wearing clothes.", cat, this) { TextColor = MakerText.ExplanationGray });
            e.AddControl(new MakerSlider(cat, "Bulge size", 0, 1, DefaultBulgeSize, this))
                .BindToFunctionController<BulgeController, float>(controller => controller.BulgeSize, (controller, value) => controller.BulgeSize = value);
        }
    }

    internal class BulgeController : CharaCustomFunctionController
    {
        private BulgeBoneEffect _bulgeBoneEffect;

        public int EnableBulge { get; set; }
        public float BulgeSize { get; set; }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (EnableBulge != BulgePlugin.DefaultBulgeState || !Mathf.Approximately(BulgeSize, BulgePlugin.DefaultBulgeSize))
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

            EnableBulge = BulgePlugin.DefaultBulgeState;
            BulgeSize = BulgePlugin.DefaultBulgeSize;
            var data = GetExtendedData();
            if (data != null)
            {
                if (data.data.TryGetValue(nameof(EnableBulge), out var eb)) EnableBulge = (int)eb;
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
        private GameObject _son;

        public BulgeBoneEffect(BulgeController ctrl)
        {
            if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));
            _ctrl = ctrl;
            _son = _ctrl.ChaControl.GetReferenceInfo(ChaReference.RefObjKey.S_Son);
        }

        private bool GetBulgeVisible()
        {
            //if (_ctrl == null || _son == null) return false;

            switch (_ctrl.EnableBulge)
            {
                case 0:
                default:
                    var status = _ctrl.ChaControl.fileStatus;
                    var bulgeVisible = status.visibleSonAlways && !_son.activeSelf;
                    return bulgeVisible;
                case 1:
                    return !_son.activeSelf;
                case 2:
                    return false;
            }
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            return GetBulgeVisible() ? _affectedBones : Enumerable.Empty<string>();
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin,
            ChaFileDefine.CoordinateType coordinate)
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
}