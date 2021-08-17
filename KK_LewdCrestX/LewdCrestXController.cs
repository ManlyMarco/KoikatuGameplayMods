using System;
using ExtensibleSaveFormat;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KoiSkinOverlayX;

namespace KK_LewdCrestX
{
    public class LewdCrestXController : CharaCustomFunctionController
    {
        internal SaveData.Heroine Heroine { get; set; }

        private KoiSkinOverlayController _overlayCtrl;
        private BoneController _boneCtrl;
        private bool _pauseUpdates;

        private CrestType _currentCrest;
        private bool _hideCrestGraphic;

        public CrestType CurrentCrest
        {
            get => _currentCrest;
            set
            {
                if (_currentCrest != value)
                {
                    _currentCrest = value;
                    if (!_pauseUpdates) ApplyCrestTexture();
                }
            }
        }

        public bool HideCrestGraphic
        {
            get => _hideCrestGraphic;
            set
            {
                if (_hideCrestGraphic != value)
                {
                    _hideCrestGraphic = value;
                    if (!_pauseUpdates) ApplyCrestTexture();
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
            SaveData();
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            ReadData();

            if (CurrentCrest == CrestType.liberated)
            {
                // Need to reload clothes for the hooks to take effect, since this controller loads too late to be seen from the hooks (NotBraShortsOverride)
                if (KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame || GameAPI.InsideHScene)
                {
                    var bra = ChaControl.objClothes[2];
                    var pan = ChaControl.objClothes[3];
                    // Avoid unnecessary reloads
                    if (bra != null && bra.activeSelf || pan != null && pan.activeSelf)
                        ChaControl.ChangeClothes(true);
                }
            }

            Heroine = ChaControl.GetHeroine();
        }

        public void SaveData()
        {
            var data = new PluginData { version = 1 };

            this.SaveToData(data, nameof(CurrentCrest), CrestType.None);
            this.SaveToData(data, nameof(HideCrestGraphic), false);

            SetExtendedData(data.data.Count > 0 ? data : null);
        }

        public void ReadData()
        {
            var data = GetExtendedData();

            _pauseUpdates = true;
            this.ReadFromData(data, nameof(CurrentCrest), CrestType.None);
            this.ReadFromData(data, nameof(HideCrestGraphic), false);//todo implement in ui and test savingloading
            _pauseUpdates = false;

            ApplyCrestTexture();
        }

        private void ApplyCrestTexture()
        {
            if (_overlayCtrl == null)
                _overlayCtrl = GetComponent<KoiSkinOverlayController>() ?? throw new Exception("Missing KoiSkinOverlayController");

            var any = _overlayCtrl.AdditionalTextures.RemoveAll(texture => ReferenceEquals(texture.Tag, this)) > 0;

            if (CurrentCrest > CrestType.None && !HideCrestGraphic)
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