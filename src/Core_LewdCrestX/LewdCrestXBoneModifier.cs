using System.Collections.Generic;
using System.Linq;
using KKABMX.Core;
using UnityEngine;

namespace KK_LewdCrestX
{
    internal sealed class LewdCrestXBoneModifier : BoneEffect
    {
        private readonly LewdCrestXController _controller;

        // todo might cause issues if in future abmx holds on to the bone modifiers since we reuse them for all characters
        private static readonly Dictionary<string, KeyValuePair<Vector3, BoneModifierData>> _vibrancyBoneModifiers;
        private static readonly string[] _vibrancyBones;
        private static readonly Dictionary<string, BoneModifierData> _lactationModifiers;
        private static readonly string[] _lactationBones;
        private static readonly Dictionary<string, BoneModifierData> _reductionModifiers;
        private static readonly string[] _reductionBones;
        
        private float _previousVibRatio;

        public LewdCrestXBoneModifier(LewdCrestXController controller)
        {
            _controller = controller;
        }

        static LewdCrestXBoneModifier()
        {
            var vibDict = new Dictionary<string, Vector3>
            {
                {"cf_d_bust01_L", new Vector3(1.25f, 1.25f, 1.25f)},
                {"cf_d_bust01_R", new Vector3(1.25f, 1.25f, 1.25f)},
                {"cf_d_bnip01_L", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_d_bnip01_R", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_s_bnip02_L", new Vector3(1.25f, 1.25f, 1.25f)},
                {"cf_s_bnip02_R", new Vector3(1.25f, 1.25f, 1.25f)},
                {"cf_s_siri_L", new Vector3(1.25f, 1.25f, 1.25f)},
                {"cf_s_siri_R", new Vector3(1.25f, 1.25f, 1.25f)},
                {"cf_s_waist01", new Vector3(0.9f, 0.9f, 0.9f)},
                {"cf_s_waist02", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_s_thigh01_L", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_s_thigh01_R", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_s_thigh02_L", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_s_thigh02_R", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_s_thigh03_L", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_s_thigh03_R", new Vector3(1.15f, 1.15f, 1.15f)},
                {"cf_n_height", new Vector3(1.08f, 1.08f, 1.08f)},
            };
            _vibrancyBones = vibDict.Keys.ToArray();
            _vibrancyBoneModifiers = vibDict.ToDictionary(
                x => x.Key,
                x => new KeyValuePair<Vector3, BoneModifierData>(x.Value, new BoneModifierData(x.Value, 1)));
            
            _lactationModifiers = new Dictionary<string, BoneModifierData>
            {
                {"cf_d_bust01_L" , new BoneModifierData(new Vector3(1.25f , 1.25f , 1.25f) , 1f)},
                {"cf_d_bust01_R" , new BoneModifierData(new Vector3(1.25f , 1.25f , 1.25f) , 1f)},
                {"cf_d_bnip01_L" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_d_bnip01_R" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
            };
            _lactationBones = _lactationModifiers.Keys.ToArray();

            _reductionModifiers = new Dictionary<string, BoneModifierData>
            {
                {"cf_d_bust01_L" , new BoneModifierData(new Vector3(0.8f , 0.8f , 0.8f) , 1f)},
                {"cf_d_bust01_R" , new BoneModifierData(new Vector3(0.8f , 0.8f , 0.8f) , 1f)},
                {"cf_d_bust02_L" , new BoneModifierData(new Vector3(0.7f , 0.7f , 0.7f) , 1f)},
                {"cf_d_bust02_R" , new BoneModifierData(new Vector3(0.7f , 0.7f , 0.7f) , 1f)},
                {"cf_s_siri_L" , new BoneModifierData(new Vector3(0.8f , 0.8f , 0.8f) , 1f)},
                {"cf_s_siri_R" , new BoneModifierData(new Vector3(0.8f , 0.8f , 0.8f) , 1f)},
                {"cf_n_height", new BoneModifierData(new Vector3(0.85f, 0.85f, 0.85f), 1f)},
                {"cf_s_waist01", new BoneModifierData( new Vector3(0.9f, 0.9f, 0.9f), 1f)},
                {"cf_s_waist02", new BoneModifierData( new Vector3(0.9f, 0.9f, 0.9f), 1f)},
                {"cf_s_thigh01_L",new BoneModifierData(new Vector3(0.9f, 0.9f, 0.9f), 1f)},
                {"cf_s_thigh01_R",new BoneModifierData(new Vector3(0.9f, 0.9f, 0.9f), 1f)},
                {"cf_s_thigh02_L",new BoneModifierData(new Vector3(0.9f, 0.9f, 0.9f), 1f)},
                {"cf_s_thigh02_R",new BoneModifierData(new Vector3(0.9f, 0.9f, 0.9f), 1f)},
                {"cf_s_thigh03_L",new BoneModifierData(new Vector3(0.9f, 0.9f, 0.9f), 1f)},
                {"cf_s_thigh03_R",new BoneModifierData(new Vector3(0.9f, 0.9f, 0.9f), 1f)},
            };
            _reductionBones = _reductionModifiers.Keys.ToArray();
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            switch (_controller.CurrentCrest)
            {
                case CrestType.vibrancy:
                    return _vibrancyBones;
                case CrestType.lactation:
                    return _lactationBones;
                case CrestType.reduction:
                    return _reductionBones;
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
                case CrestType.reduction:
                    return _reductionModifiers.TryGetValue(bone, out var reductionMod) ? reductionMod : null;
                default:
                    return null;
            }
        }
    }
}