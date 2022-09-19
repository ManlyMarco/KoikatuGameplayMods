﻿using System.Collections.Generic;
using System.Linq;
using KKABMX.Core;
using KKAPI.Maker;
using KKAPI.Studio;
using UnityEngine;

namespace KK_Pregnancy
{
    public class PregnancyBoneEffect : BoneEffect
    {
        private static readonly Dictionary<string, BoneModifierData> _bellyFullValues = new Dictionary<string, BoneModifierData>
        {
                // Belly                                :scale                                :position                           :rotation
                {"cf_s_spine01"  , new BoneModifierData(new Vector3(1.62f, 1.50f, 1.90f), 1f, new Vector3( 0.00f, 0.00f , 0.05f), new Vector3( 10f, 0f, 0f))},
                {"cf_s_spine02"  , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3( 0.00f, 0.00f , 0.01f), new Vector3(-05f, 0f, 0f))},
                {"cf_s_waist01"  , new BoneModifierData(new Vector3(1.25f, 1.55f, 1.70f), 1f, new Vector3( 0.00f, 0.02f , 0.12f), new Vector3( 15f, 0f, 0f))},
                {"cf_s_waist02"  , new BoneModifierData(new Vector3(1.17f, 1.00f, 1.00f), 1f, new Vector3( 0.00f, 0.00f , 0.00f), new Vector3(  0f, 0f, 0f))},
                // Skirt
                {"cf_d_sk_00_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3( 0.00f,-0.02f , 0.14f), new Vector3(-10f, 0f, 0f))},
                {"cf_d_sk_07_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3(-0.03f,-0.02f , 0.12f), new Vector3(-10f, 0f, 0f))},
                {"cf_d_sk_01_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3( 0.03f,-0.02f , 0.12f), new Vector3(-10f, 0f, 0f))},
                {"cf_d_sk_06_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3(-0.03f, 0.00f , 0.05f), new Vector3(  0f, 0f, 0f))},
                {"cf_d_sk_02_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3( 0.03f, 0.00f , 0.05f), new Vector3(  0f, 0f, 0f))},
        };

        private static readonly Dictionary<string, BoneModifierData> _pregnancyFullValues = new Dictionary<string, BoneModifierData>
        {
                // Breasts
                {"cf_d_bust01_L" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_d_bust01_R" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_d_bnip01_L" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_d_bnip01_R" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                // Butt
                {"cf_s_siri_L"   , new BoneModifierData(new Vector3(1.1f , 1f   , 1.2f) , 1f)},
                {"cf_s_siri_R"   , new BoneModifierData(new Vector3(1.1f , 1f   , 1.2f) , 1f)},
                // Legs
                {"cf_s_thigh00_L", new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_thigh00_R", new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_thigh01_L", new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_thigh01_R", new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_thigh02_L", new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_thigh02_R", new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_thigh03_L", new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_thigh03_R", new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_leg01_L"  , new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_leg01_R"  , new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_leg02_L"  , new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
                {"cf_s_leg02_R"  , new BoneModifierData(new Vector3(1.04f, 1f   , 1.04f), 1f)},
        };

        private static readonly IEnumerable<string> _affectedBoneNames = _bellyFullValues.Keys.Concat(_pregnancyFullValues.Keys).ToArray();
        
        private readonly PregnancyCharaController _controller;

        private float _pregnancyEffectPercent;

        public PregnancyBoneEffect(PregnancyCharaController controller)
        {
            _controller = controller;
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            if (_controller.Data.IsPregnant || MakerAPI.InsideMaker || StudioAPI.InsideStudio || PregnancyGameController.InsideHScene)
            {
                _pregnancyEffectPercent = GetPregnancyEffectPercent();
                return _affectedBoneNames;
            }

            return Enumerable.Empty<string>();
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, ChaFileDefine.CoordinateType coordinate)
        {
            var isPregnant = _controller.Data.IsPregnant;
            if (isPregnant)
            {
                if (_pregnancyFullValues.TryGetValue(bone, out var mod))
                {
                    var prEffect = _pregnancyEffectPercent;
                    return LerpModifier(mod, prEffect);
                }
            }

            if (isPregnant || _controller.IsInflated)
            {
                if (_bellyFullValues.TryGetValue(bone, out var mod))
                {
                    var prEffect = _pregnancyEffectPercent;
                    var infEffect = _controller.GetInflationEffectPercent() + prEffect / 2;

                    var bellySize = Mathf.Max(prEffect, infEffect);

                    return LerpModifier(mod, bellySize);
                }
            }

            return null;
        }

        private static BoneModifierData LerpModifier(BoneModifierData mod, float bellySize)
        {
            return new BoneModifierData(
                new Vector3(
                    Mathf.Lerp(1f, mod.ScaleModifier.x, bellySize),
                    Mathf.Lerp(1f, mod.ScaleModifier.y, bellySize),
                    Mathf.Lerp(1f, mod.ScaleModifier.z, bellySize)),
                Mathf.Lerp(1f, mod.LengthModifier, bellySize),
                new Vector3(
                    Mathf.Lerp(0f, mod.PositionModifier.x, bellySize),
                    Mathf.Lerp(0f, mod.PositionModifier.y, bellySize),
                    Mathf.Lerp(0f, mod.PositionModifier.z, bellySize)),
                new Vector3(
                    Mathf.Lerp(0f, mod.RotationModifier.x, bellySize),
                    Mathf.Lerp(0f, mod.RotationModifier.y, bellySize),
                    Mathf.Lerp(0f, mod.RotationModifier.z, bellySize)));
        }

        /// <summary>
        /// 0-1
        /// </summary>
        public float GetPregnancyEffectPercent()
        {
            if (_controller.Data.Week > PregnancyData.LeaveSchoolWeek) return 0;
            // Don't show any effect at week 1 since it begins right after winning a child lottery
            return Mathf.Clamp01((_controller.Data.Week - 1f) / (PregnancyData.LeaveSchoolWeek - 1f));
        }
    }
}