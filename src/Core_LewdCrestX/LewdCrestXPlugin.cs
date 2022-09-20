using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
using KoiSkinOverlayX;
using Studio;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace KK_LewdCrestX
{
    [BepInPlugin(GUID, "LewdCrestX", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(KKABMX_Core.GUID, "4.0")]
    [BepInDependency(KoiSkinOverlayMgr.GUID, "5.2")]
    [BepInDependency("KK_Pregnancy", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Marco.SkinEffects", BepInDependency.DependencyFlags.SoftDependency)]
    public partial class LewdCrestXPlugin : BaseUnityPlugin
    {
        public const string GUID = "LewdCrestX";
        public const string Version = "1.4";

        public static Dictionary<CrestType, CrestInfo> CrestInfos { get; } = new Dictionary<CrestType, CrestInfo>();

        internal static new ManualLogSource Logger;
        internal static AssetBundle Bundle;
        internal static Type SkinEffectsType;

        private ConfigEntry<bool> _confUnlockStoryMaker;
        private MakerText _descTxtControl;

        private Harmony _hi;

        private void Start()
        {
            Logger = base.Logger;

            _confUnlockStoryMaker = Config.Bind("Gameplay", "Allow changing crests in story mode character maker",
                false,
                "If false, to change crests inside story mode you have to invite the character to the club and use the crest icon in clubroom.");

            CharacterApi.RegisterExtraBehaviour<LewdCrestXController>(GUID);

            if (StudioAPI.InsideStudio)
            {
                LoadAssets();
                CreateStudioControls();
            }
            else
            {
                //todo hook only when entering story mode?
                _hi = new Harmony(GUID);
                _hi.PatchAll(typeof(CharacterHooks));
                AccessPointHooks.Apply(_hi);
                _hi.PatchAll(typeof(AccessPointHooks));
                _hi.PatchAll(typeof(TalkHooks));
                _hi.PatchAll(typeof(HsceneHooks));

                if (!PreggersHooks.TryPatchPreggers(_hi))
                {
                    ImplementedCrestTypes.Remove(CrestType.breedgasm);
                    ImplementedCrestTypes.Remove(CrestType.lactation);
                }
#if KK
                var effType = Type.GetType("KK_SkinEffects.SkinEffectsController, KK_SkinEffects", false);
#elif KKS
                var effType = Type.GetType("KK_SkinEffects.SkinEffectsController, KKS_SkinEffects", false);
#endif
                if (effType != null)
                    SkinEffectsType = effType;
                else
                    Logger.LogWarning("Could not find KK_SkinEffects.SkinEffectsController, some features might not work until you install KK_SkinEffects (please report this if you do have latest version of KK_SkinEffects installed)");

                LoadAssets();

                GameAPI.RegisterExtraBehaviour<LewdCrestXGameController>(GUID);

                MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
                MakerAPI.MakerFinishedLoading += MakerAPIOnMakerFinishedLoading;
            }
        }

        private static void LoadAssets()
        {
            var resource = ResourceUtils.GetEmbeddedResource("crests");
            Bundle = AssetBundle.LoadFromMemory(resource);
            DontDestroyOnLoad(Bundle);

            var textAsset = Bundle.LoadAsset<TextAsset>("crestinfo");
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
                    Bundle));
            // ReSharper restore PossibleNullReferenceException

            var sb = new System.Text.StringBuilder(300);
            sb.Append("Loaded crests:");
            foreach (var crestInfo in crestInfos)
            {
                sb.Append(' ');
                sb.Append(crestInfo.Id);
                CrestInfos.Add(crestInfo.Id, crestInfo);
            }
            Logger.LogDebug(sb.ToString());

            //todo use custom material in the future? might allow glow effects, showing through clothes, adjusting crest location
            //var mat = new Material(Shader.Find("Standard"));
            //ChaControl.rendBody.materials = ChaControl.rendBody.materials.Where(x => x != mat).AddItem(mat).ToArray();
        }

        private static void CreateStudioControls()
        {
            var currentStateCategory = StudioAPI.GetOrCreateCurrentStateCategory(null);

            var list = CrestInterfaceList.Create(false, false);
            int ReadValue(OCIChar c)
            {
                var crest = c.GetChaControl().GetComponent<LewdCrestXController>().CurrentCrest;
                return list.GetIndex(crest);
            }
            void SetValue(int i)
            {
                var crest = list.GetType(i);
                foreach (var controller in StudioAPI.GetSelectedControllers<LewdCrestXController>())
                    controller.CurrentCrest = crest;
            }
            currentStateCategory.AddControl(new CurrentStateCategoryDropdown("Lewd Crest", list.GetInterfaceNames(), ReadValue)).Value.Subscribe(SetValue);

            currentStateCategory.AddControl(new CurrentStateCategorySwitch("Lewd Crest visible",
                c => c.GetChaControl().GetComponent<LewdCrestXController>().HideCrestGraphic)).Value.Subscribe(
                b => StudioAPI.GetSelectedControllers<LewdCrestXController>().Do(ctrl => ctrl.HideCrestGraphic = b));
        }

        private void MakerAPIOnMakerFinishedLoading(object sender, EventArgs e)
        {
            _descTxtControl.ControlObject.GetComponent<LayoutElement>().minHeight = 80;
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            var category = new MakerCategory(MakerConstants.Parameter.ADK.CategoryName, "Crest");
            e.AddSubCategory(category);

            if (!_confUnlockStoryMaker.Value && MakerAPI.IsInsideClassMaker())
            {
                _descTxtControl = e.AddControl(new MakerText(
                    "To change crests inside story mode you have to invite the character to the club and use the crest icon in clubroom. You can also disable this limitation in plugin settings.",
                    category, this));
            }
            else
            {
                e.AddControl(new MakerText("Crests with the [+] tag will change gameplay in story mode.", category, this) { TextColor = MakerText.ExplanationGray });

                var list = CrestInterfaceList.Create(false, true);

                var dropdownControl = e.AddControl(new MakerDropdown("Crest type", list.GetInterfaceNames(), category, 0, this));
                dropdownControl.BindToFunctionController<LewdCrestXController, int>(
                    controller => list.GetIndex(controller.CurrentCrest),
                    (controller, value) => controller.CurrentCrest = list.GetType(value));

                e.AddControl(new MakerToggle(category, "Hide crest graphic", this))
                    .BindToFunctionController<LewdCrestXController, bool>(controller => controller.HideCrestGraphic, (controller, value) => controller.HideCrestGraphic = value);

                _descTxtControl = e.AddControl(new MakerText("Description", category, this));
                var implementedTxtControl = e.AddControl(new MakerText("", category, this));
                e.AddControl(new MakerText("The crests were created by novaksus on pixiv", category, this) { TextColor = MakerText.ExplanationGray });
                dropdownControl.ValueChanged.Subscribe(value =>
                {
                    if (value <= 0)
                    {
                        _descTxtControl.Text = "No crest selected, no effects applied";
                        implementedTxtControl.Text = "";
                    }
                    else
                    {
                        var crestInfo = list.GetInfo(value);
                        _descTxtControl.Text = crestInfo.Description;
                        implementedTxtControl.Text = crestInfo.Implemented
                            ? "This crest will affect gameplay in story mode as described"
                            : "This crest is only for looks (it might be implemented in the future with modified lore)";
                    }
                });
            }
        }
    }
}