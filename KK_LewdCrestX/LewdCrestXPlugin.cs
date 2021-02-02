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
    public class LewdCrestXPlugin : BaseUnityPlugin
    {
        public const string GUID = "LewdCrestX";
        public const string Version = "1.0";

        internal static new ManualLogSource Logger;
        internal static AssetBundle Bundle;
        private ConfigEntry<bool> _confUnlockStoryMaker;
        private MakerText _descTxtControl;
        private Harmony _hi;

        public static Dictionary<CrestType, CrestInfo> CrestInfos { get; } = new Dictionary<CrestType, CrestInfo>();

        private void Start()
        {
            Logger = base.Logger;

            _confUnlockStoryMaker = Config.Bind("Gameplay", "Allow changing crests in story mode character maker",
                false,
                "If false, to change crests inside story mode you have to invite the character to the club and use the crest icon in clubroom.");

            LoadAssets();

            CharacterApi.RegisterExtraBehaviour<LewdCrestXController>(GUID);

            _hi = new Harmony(GUID);
            _hi.PatchAll(typeof(CharacterHooks));
            PreggersHooks.TryPatchPreggers(_hi);

            if (StudioAPI.InsideStudio)
            {
                CreateStudioControls();
            }
            else
            {
                //todo hook only when entering story mode?
                _hi.PatchAll(typeof(ActionIconHooks));
                _hi.PatchAll(typeof(TalkHooks));
                _hi.PatchAll(typeof(HsceneHooks));

                GameAPI.RegisterExtraBehaviour<LewdCrestXGameController>(GUID);

                MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
                MakerAPI.MakerFinishedLoading += MakerAPIOnMakerFinishedLoading;
            }
        }

        private static void CreateStudioControls()
        {
            var crestInfos = CrestInfos.Values.OrderBy(x => x.Name).ToList();
            var crests = new[] { "No crest" }.Concat(crestInfos.Select(x => x.Name)).ToArray();

            int ReadValue(OCIChar c)
            {
                var crest = c.GetChaControl().GetComponent<LewdCrestXController>().CurrentCrest;
                return crestInfos.FindIndex(x => x.Id == crest) + 1;
            }

            void SetValue(int i)
            {
                var crest = i <= 0 ? CrestType.None : crestInfos[i - 1].Id;
                foreach (var controller in StudioAPI.GetSelectedControllers<LewdCrestXController>())
                    controller.CurrentCrest = crest;
            }

            StudioAPI.GetOrCreateCurrentStateCategory(null).AddControl(
                new CurrentStateCategoryDropdown("Lewd Crest", crests, ReadValue)).Value.Subscribe(SetValue);
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
                    bool.Parse(x.Element("Implemented").Value),
                    Bundle));
            // ReSharper restore PossibleNullReferenceException
            foreach (var crestInfo in crestInfos)
            {
                Logger.LogDebug("Added implemented crest - " + crestInfo.Id);
                CrestInfos.Add(crestInfo.Id, crestInfo);
            }

            //todo use custom material in the future? might allow glow effects, showing through clothes, adjusting crest location
            //var mat = new Material(Shader.Find("Standard"));
            //ChaControl.rendBody.materials = ChaControl.rendBody.materials.Where(x => x != mat).AddItem(mat).ToArray();
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

                var infos = CrestInfos.Values.ToList();
                var crests = new[] { "None" }.Concat(infos.Select(x => x.Implemented ? "[+] " + x.Name : x.Name)).ToArray();

                var dropdownControl = e.AddControl(new MakerDropdown("Crest type", crests, category, 0, this));
                dropdownControl.BindToFunctionController<LewdCrestXController, int>(
                    controller => infos.FindIndex(info => info.Id == controller.CurrentCrest) + 1,
                    (controller, value) => controller.CurrentCrest = value <= 0 ? CrestType.None : infos[value - 1].Id);

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
                        var crestInfo = infos[value - 1];
                        _descTxtControl.Text = crestInfo.Description;
                        implementedTxtControl.Text = crestInfo.Implemented
                            ? "This crest will affect gameplay in story mode as described"
                            : "This crest is only for looks (it might be implemented in the future with modified lore)";
                    }
                });
            }
        }

        public static LewdCrestXController GetController(SaveData.Heroine heroine)
        {
            return GetController(heroine?.chaCtrl);
        }

        public static LewdCrestXController GetController(ChaControl chaCtrl)
        {
            return chaCtrl != null ? chaCtrl.GetComponent<LewdCrestXController>() : null;
        }

        public static CrestType GetCurrentCrest(SaveData.Heroine heroine)
        {
            return GetCurrentCrest(heroine?.chaCtrl);
        }

        public static CrestType GetCurrentCrest(ChaControl chaCtrl)
        {
            var ctrl = GetController(chaCtrl);
            return ctrl == null ? CrestType.None : ctrl.CurrentCrest;
        }
    }
}