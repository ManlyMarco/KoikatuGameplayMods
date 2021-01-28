using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Utilities;
using KoiSkinOverlayX;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KK_LewdCrestX
{
    public sealed class CrestInfo
    {
        private readonly AssetBundle _bundle;
        public readonly string Description;
        public readonly CrestType Id;
        public readonly string Name;
        private Texture2D _tex;

        public CrestInfo(string id, string name, string description, AssetBundle bundle)
        {
            Id = (CrestType)Enum.Parse(typeof(CrestType), id);
            Name = name;
            Description = description;
            _bundle = bundle;
        }

        public Texture2D GetTexture()
        {
            if (_tex == null && Id > CrestType.None)
            {
                _tex = _bundle.LoadAsset<Texture2D>(Id.ToString()) ??
                       throw new Exception("Crest tex asset not found - " + Id);
                Object.DontDestroyOnLoad(_tex);
            }

            return _tex;
        }
    }

    [BepInPlugin(GUID, "LewdCrestX", Version)]
    public class LewdCrestXPlugin : BaseUnityPlugin
    {
        public const string GUID = "LewdCrestX";
        public const string Version = "1.0";

        internal static new ManualLogSource Logger;
        private MakerText _descControl;

        public static Dictionary<CrestType, CrestInfo> CrestInfos { get; } = new Dictionary<CrestType, CrestInfo>();

        private void Start()
        {
            Logger = base.Logger;

            //var mat = new Material(Shader.Find("Standard"));
            //ChaControl.rendBody.materials = ChaControl.rendBody.materials.Where(x => x != mat).AddItem(mat).ToArray();

            var resource = ResourceUtils.GetEmbeddedResource("crests");
            var bundle = AssetBundle.LoadFromMemory(resource);
            DontDestroyOnLoad(bundle);
            var textAsset = bundle.LoadAsset<TextAsset>("crestinfo");
            var infoText = textAsset.text;
            Destroy(textAsset);

            var xd = XDocument.Parse(infoText);
            // ReSharper disable PossibleNullReferenceException
            var infoElements = xd.Root.Elements("Crest");
            var crestInfos = infoElements
                .Select(x => new CrestInfo(
                    x.Element("ID").Value,
                    x.Element("Name").Value,
                    x.Element("Description").Value,
                    bundle));
            // ReSharper restore PossibleNullReferenceException
            foreach (var crestInfo in crestInfos)
                CrestInfos.Add(crestInfo.Id, crestInfo);

            CharacterApi.RegisterExtraBehaviour<LewdCrestXController>(GUID);

            if (StudioAPI.InsideStudio)
            {
                //todo
            }
            else
            {
                MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
                MakerAPI.MakerFinishedLoading += MakerAPIOnMakerFinishedLoading;
            }
        }

        private void MakerAPIOnMakerFinishedLoading(object sender, EventArgs e)
        {
            _descControl.ControlObject.GetComponent<LayoutElement>().minHeight = 80;
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            var category = new MakerCategory(MakerConstants.Parameter.ADK.CategoryName, "Crest");
            e.AddSubCategory(category);

            var infos = CrestInfos.Values.ToList();
            var crests = new[] { "None" }.Concat(infos.Select(x => x.Name)).ToArray();

            var dropdown = e.AddControl(new MakerDropdown("Crest type", crests, category, 0, this));
            dropdown.BindToFunctionController<LewdCrestXController, int>(
                controller => infos.FindIndex(info => info.Id == controller.CurrentCrest) + 1,
                (controller, value) => controller.CurrentCrest = value <= 0 ? CrestType.None : infos[value - 1].Id);

            _descControl = e.AddControl(new MakerText("Description", category, this));
            dropdown.ValueChanged.Subscribe(value => _descControl.Text = value <= 0 ? "No crest selected, no effects applied" : infos[value - 1].Description);
        }
    }

    public class LewdCrestXController : CharaCustomFunctionController
    {
        private CrestType _currentCrest;
        private KoiSkinOverlayController _overlayCtrl;

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