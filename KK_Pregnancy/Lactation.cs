using System.Collections.Generic;
using HarmonyLib;
using Illusion.Game;
using UnityEngine;

namespace KK_Pregnancy
{
    public class LactationController : MonoBehaviour
    {
        private readonly List<CharaData> _charas = new List<CharaData>();
        private float _orgasmDelayTimer;

        private HSceneProc _proc;

        private void Start()
        {
            _proc = FindObjectOfType<HSceneProc>();

            var lstFemale = (List<ChaControl>)AccessTools.Field(typeof(HSceneProc), "lstFemale").GetValue(_proc);

            _charas.Add(new CharaData(lstFemale[0], _proc.particle));
            if (lstFemale.Count > 1 && lstFemale[1] != null)
                _charas.Add(new CharaData(lstFemale[1], _proc.particle));

            if (_charas.TrueForAll(x => x.MaxMilk < 10))
                enabled = false;

            // todo Is this safe only run once?
            InitializeParticles(_charas);
        }

        private void Update()
        {
            // figure out when to fire
            var animatorStateInfo = _charas[0].ChaControl.getAnimatorStateInfo(0);
            var oloop = _proc.flags.mode == HFlag.EMode.aibu
                ? animatorStateInfo.IsName("Orgasm_Start")
                : animatorStateInfo.IsName("OLoop") || animatorStateInfo.IsName("A_OLoop");

            if (oloop)
            {
                _orgasmDelayTimer = 0.1f;
            }
            else if (_orgasmDelayTimer > 0)
            {
                _orgasmDelayTimer -= Time.deltaTime;
                if (_orgasmDelayTimer <= 0)
                    OnOrgasm();
            }

            // Regenerate milk over time
            foreach (var charaData in _charas)
            {
                // Fully fill in 60 seconds * x
                var change = Time.deltaTime / (60f * 5f);
                // Slower recharge if there isn't much of it
                change *= charaData.MaxMilk;
                charaData.CurrentMilk = Mathf.Min(charaData.CurrentMilk + change, charaData.MaxMilk);
            }
        }

        private void OnOrgasm()
        {
            foreach (var charaData in _charas)
            {
                var chaControl = charaData.ChaControl;

                var clothesState = chaControl.fileStatus.clothesState;
                // Only trigger when the top clothes are not present or removed
                if ((!chaControl.IsClothesStateKind(0) || clothesState[0] != 0) &&
                    (!chaControl.IsClothesStateKind(2) || clothesState[2] != 0))
                {
                    PregnancyPlugin.Logger.LogDebug(
                        $"OnOrgasm > CurrentMilk level for chara {chaControl.chaFile.parameter.fullname}: {Mathf.RoundToInt(charaData.CurrentMilk * 100)}%");

                    if (charaData.CurrentMilk >= 0.65f)
                    {
                        charaData.ParticleCtrl.Play(33);
                        charaData.ParticleCtrl.Play(43);

                        charaData.CurrentMilk -= 0.35f;
                        PlaySoundEffect(chaControl, ChaReference.RefObjKey.a_n_bust_f);

                        var currentState = chaControl.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp);
                        if (currentState < 2) // Has 3 states, value is max 2
                            chaControl.SetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp, (byte)(currentState + 1));
                    }
                    else if (charaData.CurrentMilk >= 0.4f)
                    {
                        charaData.ParticleCtrl.Play(35);
                        charaData.ParticleCtrl.Play(45);

                        charaData.CurrentMilk -= 0.25f;
                        PlaySoundEffect(chaControl, ChaReference.RefObjKey.a_n_bust_f);

                        var currentState = chaControl.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp);
                        if (currentState < 1) // Only go up to the 1st level since quantity is lower
                            chaControl.SetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp, (byte)(currentState + 1));
                    }
                }
                else
                {
                    PregnancyPlugin.Logger.LogDebug(
                        $"OnOrgasm > Blocked by clothes for chara {chaControl.chaFile.parameter.fullname}");
                }
            }
        }

        private static void PlaySoundEffect(ChaControl chaControl, ChaReference.RefObjKey reference)
        {
            var soundEffectSetting = new Utils.Sound.Setting(Manager.Sound.Type.GameSE3D)
            {
                assetBundleName = "sound/data/se/h/00/00_00.unity3d",
                assetName = "khse_06"
            };
            // Alternative sound effect, much longer
            //assetBundleName = @"sound/data/se/h/12/12_00.unity3d";
            //assetName = "hse_siofuki";

            var soundTr = Utils.Sound.Play(soundEffectSetting);
            var chaRef = chaControl.GetReferenceInfo(reference);
            if (soundTr && chaRef)
                soundTr.SetParent(chaRef.transform, false);
        }

        private static void InitializeParticles(List<CharaData> charas)
        {
            for (var i = 0; i < charas.Count; i++)
            {
                var charaData = charas[i];
                if (charaData == null) continue;

                var particleDic = (Dictionary<int, HParticleCtrl.ParticleInfo>)AccessTools
                    .Field(typeof(HParticleCtrl), "dicParticle").GetValue(charaData.ParticleCtrl);
                if (particleDic.ContainsKey(33)) return; // Already added

                PregnancyPlugin.Logger.LogDebug("Adding particles to heroine #" + i);

                particleDic.Add(33, new HParticleCtrl.ParticleInfo
                {
                    assetPath = @"h/common/00_00.unity3d",
                    file = "LiquidSiru",
                    numParent = 1,
                    nameParent = "a_n_nip_R",
                    pos = new Vector3(0f, 0f, 0.05f),
                    rot = new Vector3(-25f, 0, 0f)
                });

                particleDic.Add(35, new HParticleCtrl.ParticleInfo
                {
                    assetPath = @"h/common/00_00.unity3d",
                    file = "LiquidSio",
                    numParent = 1,
                    nameParent = "a_n_nip_R",
                    pos = new Vector3(0, 0f, 0.05f),
                    rot = new Vector3(-20, 0, 0)
                });

                particleDic.Add(43, new HParticleCtrl.ParticleInfo
                {
                    assetPath = @"h/common/00_00.unity3d",
                    file = "LiquidSiru",
                    numParent = 1,
                    nameParent = "a_n_nip_L",
                    pos = new Vector3(0, 0f, 0.05f),
                    rot = new Vector3(-25f, 0, 0f)
                });

                particleDic.Add(45, new HParticleCtrl.ParticleInfo
                {
                    assetPath = @"h/common/00_00.unity3d",
                    file = "LiquidSio",
                    numParent = 1,
                    nameParent = "a_n_nip_L",
                    pos = new Vector3(0, 0f, 0.05f),
                    rot = new Vector3(-20, 0, 0)
                });

                charaData.ParticleCtrl.Load(charaData.ChaControl.objBodyBone, 1);
            }
        }

        private class CharaData
        {
            public readonly ChaControl ChaControl;
            public readonly PregnancyCharaController Controller;
            public readonly HParticleCtrl ParticleCtrl;

            // Range from 0 to MaxMilk
            public float CurrentMilk;
            // Range from 0 to 1
            public readonly float MaxMilk;

            public CharaData(ChaControl chaControl, HParticleCtrl particleCtrl)
            {
                ChaControl = chaControl;
                ParticleCtrl = particleCtrl;

                Controller = chaControl.GetComponent<PregnancyCharaController>();
                MaxMilk = Mathf.Clamp01(Controller.Week / 40f);
                CurrentMilk = MaxMilk;
            }
        }
    }
}