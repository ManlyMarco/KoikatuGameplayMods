using System;
using ExtensibleSaveFormat;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KoiSkinOverlayX;
using UnityEngine;

namespace KK_LewdCrestX
{
    public class LewdCrestXController : CharaCustomFunctionController
    {
        private CrestType _currentCrest;
        private KoiSkinOverlayController _overlayCtrl;
        private BoneController _boneCtrl;

        internal SaveData.Heroine Heroine { get; private set; }

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