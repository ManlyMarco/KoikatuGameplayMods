using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Illusion.Game;
using UnityEngine;
using Random = UnityEngine.Random;
#if AI
    using AIChara;
#endif

namespace KK_Pregnancy
{
    public class LactationController : MonoBehaviour
    {
        private const float MinimumMilk = 0.4f;
        private const float MoreMilk = 0.65f;
        private readonly List<CharaData> _charas = new List<CharaData>();
        private float _orgasmDelayTimer;
        private HSceneProc _proc;
        private float _touchingDelayTimer;

        private void Start()
        {
            if (!PregnancyPlugin.LactationEnabled.Value)
            {
                enabled = false;
                return;
            }

            _proc = FindObjectOfType<HSceneProc>();

            var lstFemale = (List<ChaControl>)AccessTools.Field(typeof(HSceneProc), "lstFemale").GetValue(_proc);

            _charas.Add(new CharaData(lstFemale[0], _proc.particle, _proc.hand));
            if (lstFemale.Count > 1 && lstFemale[1] != null)
                _charas.Add(new CharaData(lstFemale[1], _proc.particle1, _proc.hand1));

            if (_charas.TrueForAll(x => x.MaxMilk < MinimumMilk))
                enabled = false;
        }

        private void Update()
        {
            // figure out when to fire
            var animatorStateInfo = _charas[0].ChaControl.getAnimatorStateInfo(0);
            var hFlag = _proc.flags;
            var orgasmloop = hFlag.mode == HFlag.EMode.aibu
                ? animatorStateInfo.IsName("Orgasm_Start")
                : animatorStateInfo.IsName("OLoop") || animatorStateInfo.IsName("A_OLoop");

            if (orgasmloop)
            {
                _orgasmDelayTimer = Random.value / 8; //0.1f;
            }
            else if (_orgasmDelayTimer > 0)
            {
                _orgasmDelayTimer -= Time.deltaTime;
                if (_orgasmDelayTimer <= 0)
                    OnOrgasm();
            }
            else
            {
                if (hFlag.drag && hFlag.speedItem > hFlag.speedUpItemClac.y * 0.8f)
                {
                    if (_touchingDelayTimer <= 0)
                    {
                        foreach (var charaData in _charas)
                        {
                            if (charaData.IsTouchingMune(true, true))
                            {
                                charaData.FireParticles(false);
                                _touchingDelayTimer = 1f;
                            }
                        }
                    }
                    else
                    {
                        _touchingDelayTimer -= Time.deltaTime;
                    }
                }
                else
                {
                    _touchingDelayTimer = 0;
                }
            }

            // Regenerate milk over time
            foreach (var charaData in _charas)
            {
                if (PregnancyPlugin.LactationFillTime.Value == 0)
                {
                    charaData.CurrentMilk = charaData.MaxMilk;
                }
                else
                {
                    // Fully fill in 60 seconds * x
                    var change = Time.deltaTime / (60f * PregnancyPlugin.LactationFillTime.Value);
                    charaData.CurrentMilk = Mathf.Min(charaData.CurrentMilk + change * charaData.MaxMilk, charaData.MaxMilk);
                }
            }
        }

        private void OnOrgasm()
        {
            foreach (var charaData in _charas)
                charaData.FireParticles(true);
        }

        private class CharaData
        {
            private readonly HParticleCtrl _particleCtrl;
            private readonly HandCtrl _procHand;

            public readonly ChaControl ChaControl;

            // Range from 0 to 1
            public readonly float MaxMilk;
            private HParticleCtrl.ParticleInfo _partHeavyL;

            private HParticleCtrl.ParticleInfo _partHeavyR;
            private HParticleCtrl.ParticleInfo _partLightL;
            private HParticleCtrl.ParticleInfo _partLightR;

            private int _singleTriggerCount;

            // Range from 0 to MaxMilk
            public float CurrentMilk;

            public CharaData(ChaControl chaControl, HParticleCtrl particleCtrl, HandCtrl procHand)
            {
                ChaControl = chaControl ? chaControl : throw new ArgumentNullException(nameof(chaControl));
                _procHand = procHand ? procHand : throw new ArgumentNullException(nameof(procHand));
                _particleCtrl = particleCtrl ? particleCtrl : throw new ArgumentNullException(nameof(particleCtrl));

                var controller = chaControl.GetComponent<PregnancyCharaController>();
                MaxMilk = GetMilkAmount(controller);
                CurrentMilk = MaxMilk;
            }

            private static float GetMilkAmount(PregnancyCharaController controller)
            {
                if (controller == null) return 0;
                if (PregnancyPlugin.LactationForceMaxCapacity.Value) return 1;
                var data = controller.Data;
                if (data.AlwaysLactates) return 1;
                // Gradually increase
                if (data.IsPregnant) return Mathf.Clamp01(data.Week / 40f);
                // Gradually decrease after pregnancy finishes
                if (data.PregnancyCount > 0) return 1 - Mathf.Clamp01(data.WeeksSinceLastPregnancy / (40f / PregnancyPlugin.PregnancyProgressionSpeed.Value));
                return 0;
            }

            public bool IsTouchingMune(bool l, bool r)
            {
                for (var i = 0; i < 3; i++)
                {
                    var useItemStickArea = _procHand.GetUseItemStickArea(i);
                    if (useItemStickArea == HandCtrl.AibuColliderKind.muneL && l ||
                        useItemStickArea == HandCtrl.AibuColliderKind.muneR && r) return true;
                }

                return false;
            }

            public void FireParticles(bool orgasm)
            {
                var clothesState = ChaControl.fileStatus.clothesState;
                // Only trigger when the top clothes are not present or removed
                if ((!ChaControl.IsClothesStateKind(0) || clothesState[0] != 0) &&
                    (!ChaControl.IsClothesStateKind(2) || clothesState[2] != 0))
                {
                    //PregnancyPlugin.Logger.LogDebug(
                    //    $"OnOrgasm > CurrentMilk level for chara {ChaControl.chaFile.parameter.fullname}: {Mathf.RoundToInt(CurrentMilk * 100)}%");

                    InitializeParticles();

                    if (CurrentMilk >= MoreMilk)
                    {
                        if (orgasm)
                            OnOrgasm(true);
                        else
                            OnSingle(true);
                    }
                    else if (CurrentMilk >= MinimumMilk)
                    {
                        if (orgasm)
                            OnOrgasm(false);
                        else
                            OnSingle(false);
                    }
                }
            }

            private void OnSingle(bool large)
            {
                if (IsTouchingMune(true, false))
                {
                    _partLightL.particle.Simulate(0f);
                    _partLightL.particle.Play();
                    CurrentMilk -= 0.025f;
                }

                if (IsTouchingMune(false, true))
                {
                    _partLightR.particle.Simulate(0f);
                    _partLightR.particle.Play();
                    CurrentMilk -= 0.025f;
                }

                ChaControl.StartCoroutine(OnSingleCo());

                if (_singleTriggerCount++ == 4)
                {
                    var currentState = ChaControl.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp);
                    if (currentState < 1) // Only go up to the 1st level since quantity is lower
                        ChaControl.SetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp, (byte)(currentState + 1));
                }
            }

            private IEnumerator OnSingleCo()
            {
                yield return new WaitForSeconds(0.3f);
                //_partHeavyL.particle.Stop();
                //_partHeavyR.particle.Stop();
                _partLightL.particle.Stop();
                _partLightR.particle.Stop();
            }

            private void OnOrgasm(bool large)
            {
                void PlaySoundEffect(ChaControl chaControl, ChaReference.RefObjKey reference)
                {
                    var soundEffectSetting = new Utils.Sound.Setting(Manager.Sound.Type.GameSE3D)
                    {
#if KK
                        assetBundleName = "sound/data/se/h/00/00_00.unity3d",
                        assetName = "khse_06"
                        // Alternative sound effect, much longer
                        //assetBundleName = @"sound/data/se/h/12/12_00.unity3d";
                        //assetName = "hse_siofuki";
#else
                        bundle = "sound/data/se/h/00/00_00.unity3d",
                        asset = "khse_06"
#endif
                    };

                    var soundSource = Utils.Sound.Play(soundEffectSetting);
                    var chaRef = chaControl.GetReferenceInfo(reference);
                    if (soundSource && chaRef)
                        soundSource.transform.SetParent(chaRef.transform, false);
                }

                PlaySoundEffect(ChaControl, ChaReference.RefObjKey.a_n_bust_f);

                var currentState = ChaControl.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp);
                if (large)
                {
                    _partHeavyL.particle.Simulate(0f);
                    _partHeavyL.particle.Play();
                    _partHeavyR.particle.Simulate(0f);
                    _partHeavyR.particle.Play();

                    CurrentMilk -= 0.35f;
                    if (currentState < 2) // Has 3 states, value is max 2
                        ChaControl.SetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp, (byte)(currentState + 1));
                }
                else
                {
                    _partLightL.particle.Simulate(0f);
                    _partLightL.particle.Play();
                    _partLightR.particle.Simulate(0f);
                    _partLightR.particle.Play();

                    CurrentMilk -= 0.25f;
                    if (currentState < 1) // Only go up to the 1st level since quantity is lower
                        ChaControl.SetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp, (byte)(currentState + 1));
                }
            }

            private void InitializeParticles()
            {
                // todo what happens when switching characters?
                if (_partHeavyR != null) return;

                PregnancyPlugin.Logger.LogDebug("Adding particles to heroine: " + ChaControl.fileParam.fullname);

                _partHeavyR = new HParticleCtrl.ParticleInfo
                {
                    file = "LiquidSiru",
                    numParent = 1,
                    nameParent = "a_n_nip_R",
                    //pos = new Vector3(0f, 0f, 0.05f),
                    rot = new Vector3(-25f, 0, 0f)
                };
                _partLightR = new HParticleCtrl.ParticleInfo
                {
                    file = "LiquidSio",
                    numParent = 1,
                    nameParent = "a_n_nip_R",
                    //pos = new Vector3(0, 0f, 0.05f),
                    rot = new Vector3(-20, 0, 0)
                };
                _partHeavyL = new HParticleCtrl.ParticleInfo
                {
                    file = "LiquidSiru",
                    numParent = 1,
                    nameParent = "a_n_nip_L",
                    //pos = new Vector3(0, 0f, 0.05f),
                    rot = new Vector3(-25f, 0, 0f)
                };
                _partLightL = new HParticleCtrl.ParticleInfo
                {
                    file = "LiquidSio",
                    numParent = 1,
                    nameParent = "a_n_nip_L",
                    //pos = new Vector3(0, 0f, 0.05f),
                    rot = new Vector3(-20, 0, 0)
                };
#if KK
                _partHeavyR.assetPath = @"h/common/00_00.unity3d";
                _partLightR.assetPath = @"h/common/00_00.unity3d";
                _partHeavyL.assetPath = @"h/common/00_00.unity3d";
                _partLightL.assetPath = @"h/common/00_00.unity3d";
#elif KKS
                _partHeavyR.assetPath = @"h/common/01.unity3d";
                _partLightR.assetPath = @"h/common/01.unity3d";
                _partHeavyL.assetPath = @"h/common/01.unity3d";
                _partLightL.assetPath = @"h/common/01.unity3d";
                // Manifests are needed or the game will crash after changing H positions
                _partHeavyR.manifest = "add01";
                _partLightR.manifest = "add01";
                _partHeavyL.manifest = "add01";
                _partLightL.manifest = "add01";
#endif

                // Load the particles
                var particleDic = _particleCtrl.dicParticle;
                particleDic[691] = _partHeavyR;
                particleDic[692] = _partLightR;
                particleDic[693] = _partHeavyL;
                particleDic[694] = _partLightL;
                _particleCtrl.Load(ChaControl.objBodyBone, 1);

#if KKS
                // Need to unload the bundles we just loaded or things that try to load them later like hpointmove can crash
                // Need to unload both manifest null and add01 for some reason or the bundle won't fully unload
                AssetBundleManager.UnloadAssetBundle(@"h/common/01.unity3d", true, null, false);
                AssetBundleManager.UnloadAssetBundle(@"h/common/01.unity3d", true, "add01", false);
#endif
            }
        }
    }
}