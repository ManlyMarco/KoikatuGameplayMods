using System;
using System.Collections.Generic;
using System.Linq;
using KKABMX.Core;
using KKAPI.Maker;
using KKAPI.Studio;
using UnityEngine;
using AIChara;

namespace KK_Pregnancy
{
    public class PregnancyBoneEffect : BoneEffect
    {
        private readonly PregnancyCharaController _controller;

        private static readonly Dictionary<string, BoneModifierData> _bellyFullValues = new Dictionary<string, BoneModifierData>
        {
                // Belly
                {"cf_J_Spine01_s"  , new BoneModifierData(new Vector3(1.30f, 1.15f, 1.90f), 1f, new Vector3( 0.00f, 0.00f , 0.50f), new Vector3( 3f, 0f, 0f))},
                {"cf_J_Spine02_s"  , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3( 0.00f, 0.00f , 0.10f), new Vector3(-05f, 0f, 0f))},
                {"cf_J_Kosi01_s"  , new BoneModifierData(new Vector3(1.00f, 1.55f, 1.70f), 1f, new Vector3( 0.00f, 0.20f , 0.90f), new Vector3( 7f, 0f, 0f))},
                {"cf_J_Kosi02_s"  , new BoneModifierData(new Vector3(1.17f, 1.00f, 1.00f), 1f, new Vector3( 0.00f, 0.00f , 0.00f), new Vector3(  0f, 0f, 0f))},
                // Skirt
                //TODO
                // {"cf_J_sk_00_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3( 0.00f,-0.02f , 0.14f), new Vector3(-10f, 0f, 0f))},
                // {"cf_J_sk_07_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3(-0.03f,-0.02f , 0.12f), new Vector3(-10f, 0f, 0f))},
                // {"cf_J_sk_01_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3( 0.03f,-0.02f , 0.12f), new Vector3(-10f, 0f, 0f))},
                // {"cf_J_sk_06_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3(-0.03f, 0.00f , 0.05f), new Vector3(  0f, 0f, 0f))},
                // {"cf_J_sk_02_00" , new BoneModifierData(new Vector3(1.00f, 1.00f, 1.00f), 1f, new Vector3( 0.03f, 0.00f , 0.05f), new Vector3(  0f, 0f, 0f))},
        };

        private static readonly Dictionary<string, BoneModifierData> _pregnancyFullValues = new Dictionary<string, BoneModifierData>
        {
                // Breasts
                {"cf_J_Mune00_s_L" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_J_Mune00_s_R" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_J_Mune_Nip01_s_L" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                {"cf_J_Mune_Nip01_s_R" , new BoneModifierData(new Vector3(1.2f , 1.2f , 1.2f) , 1f)},
                // Butt
                {"cf_J_Siri_s_L"   , new BoneModifierData(new Vector3(1.5f , 1.5f   , 1.75f) , 1f, new Vector3(-0.5f , 0f, -0.2f), Vector3.zero)},
                {"cf_J_Siri_s_R"   , new BoneModifierData(new Vector3(1.5f , 1.5f   , 1.75f) , 1f, new Vector3(0.5f , 0f, -0.2f), Vector3.zero)},
                // Legs
                {"cf_J_LegUp01_s_L", new BoneModifierData(new Vector3(1.1f, 1f   , 1.1f), 1f)},
                {"cf_J_LegUp01_s_R", new BoneModifierData(new Vector3(1.1f, 1f   , 1.1f), 1f)},
                {"cf_J_LegUp02_s_L", new BoneModifierData(new Vector3(1.1f, 1f   , 1.1f), 1f)},
                {"cf_J_LegUp02_s_R", new BoneModifierData(new Vector3(1.1f, 1f   , 1.1f), 1f)},
                {"cf_J_LegUp03_s_L", new BoneModifierData(new Vector3(1.1f, 1f   , 1.1f), 1f)},
                {"cf_J_LegUp03_s_R", new BoneModifierData(new Vector3(1.1f, 1f   , 1.1f), 1f)},
                //TODO what is equivalent to thigh?
        };

        private static readonly IEnumerable<string> _affectedBoneNames = _bellyFullValues.Keys.Concat(_pregnancyFullValues.Keys).ToArray();

        public PregnancyBoneEffect(PregnancyCharaController controller)
        {
            _controller = controller;
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            if (_controller.Data.IsPregnant || MakerAPI.InsideMaker || StudioAPI.InsideStudio || PregnancyGameController.InsideHScene)
                return _affectedBoneNames;

            return Enumerable.Empty<string>();
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate)
        {
            var isPregnant = _controller.Data.IsPregnant;
            if (isPregnant)
            {
                if (_pregnancyFullValues.TryGetValue(bone, out var mod))
                {
                    var prEffect = GetPregnancyEffectPercent();
                    return LerpModifier(mod, prEffect);
                }
            }

            if (isPregnant || _controller.IsInflated)
            {
                if (_bellyFullValues.TryGetValue(bone, out var mod))
                {
                    var prEffect = GetPregnancyEffectPercent();
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
            // also reduce belly size in the 2nd week based on the time step to avoid huge bellies causing a large belly to appear out of nowhere very early
            var progressionSpeed = Mathf.Ceil(PregnancyPlugin.PregnancyProgressionSpeed.Value / 2f);
            return Mathf.Clamp01((_controller.Data.Week - progressionSpeed) / (PregnancyData.LeaveSchoolWeek - progressionSpeed));
        }
    }
}