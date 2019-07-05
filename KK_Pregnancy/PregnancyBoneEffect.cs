using System.Collections.Generic;
using System.Linq;
using KKABMX.Core;
using KKAPI.Maker;
using UnityEngine;

namespace KK_Pregnancy
{
    public class PregnancyBoneEffect : BoneEffect
    {
        private readonly PregnancyCharaController _controller;

        // todo use flat len offsets whenever they get implemented
        private readonly Dictionary<string, BoneModifierData> _pregnancyFullValues = new Dictionary<string, BoneModifierData>
        {
            // Belly
            {"cf_s_spine01", new BoneModifierData(new Vector3(1.62f, 1.5f, 1.9f), -6.15f)},
            {"cf_s_waist01", new BoneModifierData(new Vector3(1.13f, 1.55f, 1.7f), -3.6f)},
            {"cf_s_waist02", new BoneModifierData(new Vector3(1.17f, 1f, 1f), 1f)},
            // Skirt Front
            {"cf_d_sk_00_00", new BoneModifierData(new Vector3(1.35f, 1f, 1f), 2.15F)},
            // Skirt Front sides
            {"cf_d_sk_07_00", new BoneModifierData(new Vector3(1.5f, 1f, 1f), 1.65F)},
            {"cf_d_sk_01_00", new BoneModifierData(new Vector3(1.5f, 1f, 1f), 1.65F)},
            // Skirt Back
            {"cf_d_sk_04_00", new BoneModifierData(new Vector3(1f, 1f, 1f), 1.1f)},
            // Skirt Back sides
            {"cf_d_sk_05_00", new BoneModifierData(new Vector3(1f, 1f, 1f), 1.12F)},
            {"cf_d_sk_03_00", new BoneModifierData(new Vector3(1f, 1f, 1f), 1.12F)},
            // Breasts
            {"cf_d_bust01_L", new BoneModifierData(new Vector3(1.15f, 1.15f, 1.15f), 1f)},
            {"cf_d_bust01_R", new BoneModifierData(new Vector3(1.15f, 1.15f, 1.15f), 1f)},
            // Nipples because yes
            {"cf_d_bnip01_L", new BoneModifierData(new Vector3(1.1f, 1.1f, 1.1f), 1f)},
            {"cf_d_bnip01_R", new BoneModifierData(new Vector3(1.1f, 1.1f, 1.1f), 1f)},
            // More butt
            {"cf_s_siri_L", new BoneModifierData(new Vector3(1.03f, 1f, 1.1f), 1f)},
            {"cf_s_siri_R", new BoneModifierData(new Vector3(1.03f, 1f, 1.1f), 1f)},
            // Stronger legs
            {"cf_s_thigh00_L", new BoneModifierData(new Vector3(1.02f, 1f, 1.03f), 1f)},
            {"cf_s_thigh00_R", new BoneModifierData(new Vector3(1.02f, 1f, 1.03f), 1f)},
            {"cf_s_thigh01_L", new BoneModifierData(new Vector3(1.02f, 1f, 1.02f), 1f)},
            {"cf_s_thigh01_R", new BoneModifierData(new Vector3(1.02f, 1f, 1.02f), 1f)},
            {"cf_s_thigh02_L", new BoneModifierData(new Vector3(1.02f, 1f, 1.02f), 1f)},
            {"cf_s_thigh02_R", new BoneModifierData(new Vector3(1.02f, 1f, 1.02f), 1f)},
            {"cf_s_thigh03_L", new BoneModifierData(new Vector3(1.02f, 1f, 1.02f), 1f)},
            {"cf_s_thigh03_R", new BoneModifierData(new Vector3(1.02f, 1f, 1.02f), 1f)},
            {"cf_s_leg01_L", new BoneModifierData(new Vector3(1.02f, 1f, 1.02f), 1f)},
            {"cf_s_leg01_R", new BoneModifierData(new Vector3(1.02f, 1f, 1.02f), 1f)},
            {"cf_s_leg02_L", new BoneModifierData(new Vector3(1.03f, 1f, 1.02f), 1f)},
            {"cf_s_leg02_R", new BoneModifierData(new Vector3(1.03f, 1f, 1.02f), 1f)},
        };

        public PregnancyBoneEffect(PregnancyCharaController controller)
        {
            _controller = controller;
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            if (_controller.IsDuringPregnancy()
                || MakerAPI.InsideMaker
                //|| StudioAPI.InsideStudio todo needed after adding studio slider
                )
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