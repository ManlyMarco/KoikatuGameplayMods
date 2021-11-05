using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
using UniRx;
using UnityEngine;

namespace KK_Pregnancy
{
    public static partial class PregnancyGui
    {
        private static PregnancyPlugin _pluginInstance;

        internal static void Init(Harmony hi, PregnancyPlugin instance)
        {
            _pluginInstance = instance;

            if (StudioAPI.InsideStudio)
            {
                RegisterStudioControls();
            }
            else
            {
                MakerAPI.RegisterCustomSubCategories += MakerAPI_MakerBaseLoaded;

                Sprite LoadIcon(string resourceFileName)
                {
                    var iconTex = new Texture2D(2, 2, TextureFormat.DXT5, false);
                    Object.DontDestroyOnLoad(iconTex);
                    iconTex.LoadImage(ResourceUtils.GetEmbeddedResource(resourceFileName));

                    var sprite = Sprite.Create(iconTex, new Rect(0f, 0f, iconTex.width, iconTex.height),
                        new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
                    Object.DontDestroyOnLoad(sprite);
                    return sprite;
                }
                var pregSprite = LoadIcon("pregnant.png");
                var riskySprite = LoadIcon("risky.png");
                var safeSprite = LoadIcon("safe.png");
                var unknownSprite = LoadIcon("unknown.png");
                var leaveSprite = LoadIcon("leave.png");

                StatusIcons.Init(hi, unknownSprite, pregSprite, safeSprite, riskySprite, leaveSprite);

                HSceneMenstrIconOverride.Init(hi, unknownSprite, pregSprite, safeSprite, riskySprite, leaveSprite);
            }
        }

        private static void RegisterStudioControls()
        {
            var cat = StudioAPI.GetOrCreateCurrentStateCategory(null);
            cat.AddControl(new CurrentStateCategorySlider("Pregnancy", c =>
                {
                    if (c.charInfo == null) return 0;
                    var controller = c.charInfo.GetComponent<PregnancyCharaController>();
                    if (controller == null) return 0;
                    return controller.Data.Week;
                }, 0, 40))
                .Value.Subscribe(f => { foreach (var ctrl in StudioAPI.GetSelectedControllers<PregnancyCharaController>()) ctrl.Data.Week = Mathf.RoundToInt(f); });
        }

        private static void MakerAPI_MakerBaseLoaded(object sender, RegisterSubCategoriesEvent e)
        {
            var female = MakerAPI.GetMakerSex() != 0;

            // This category is inaccessible from class maker
            var cat = new MakerCategory(MakerConstants.Parameter.Character.CategoryName, "Pregnancy"); //MakerConstants.Parameter.Character;

            e.AddSubCategory(cat);

            var hintColor = new Color(0.7f, 0.7f, 0.7f);

            var gameplayToggle = e.AddControl(new MakerToggle(cat, "Enable pregnancy progression", true, _pluginInstance));
            gameplayToggle.BindToFunctionController<PregnancyCharaController, bool>(controller => controller.Data.GameplayEnabled, (controller, value) => controller.Data.GameplayEnabled = value);
            e.AddControl(new MakerText(female ?
                "If off, the character can't get pregnant and current pregnancy will stop progressing." :
                "If on, belly will progressively get bigger. No other effects.",
                cat, _pluginInstance)
            { TextColor = hintColor });

            if (female)
            {
                var fertilityToggle = e.AddControl(new MakerSlider(cat, "Fertility", 0f, 1f, PregnancyData.DefaultFertility, _pluginInstance));
                fertilityToggle.BindToFunctionController<PregnancyCharaController, float>(controller => controller.Data.Fertility, (controller, value) => controller.Data.Fertility = value);

                e.AddControl(new MakerText("How likely this character is to get pregnant.", cat, _pluginInstance) { TextColor = hintColor });
            }

            var weeksSlider = e.AddControl(new MakerSlider(cat, "Week of pregnancy", 0f, PregnancyData.LeaveSchoolWeek - 1f, 0f, _pluginInstance));
            weeksSlider.ValueToString = f => Mathf.RoundToInt(f).ToString();
            weeksSlider.StringToValue = s => int.Parse(s);
            weeksSlider.BindToFunctionController<PregnancyCharaController, float>(controller => controller.Data.Week, (controller, value) => controller.Data.Week = Mathf.RoundToInt(value));

            e.AddControl(new MakerText(female ?
                "If the character is pregnant when added to the game, the pregnancy will continue from this point." :
                "The only way for male characters to get pregnant is to manually set this slider above 0.",
                cat, _pluginInstance)
            { TextColor = hintColor });

            if (female)
            {
                var scheduleToggle = e.AddControl(new MakerRadioButtons(cat, _pluginInstance, "Menstruation schedule", "Default", "More risky", "Always safe", "Always risky"));
                scheduleToggle.BindToFunctionController<PregnancyCharaController, int>(controller => (int)controller.Data.MenstruationSchedule, (controller, value) => controller.Data.MenstruationSchedule = (MenstruationSchedule)value);

                e.AddControl(new MakerText("Changes how many risky days the character has in a cycle. Default is more safe days than risky days.", cat, _pluginInstance) { TextColor = hintColor });
            }

            if (female)
            {
                var lactatToggle = e.AddControl(new MakerToggle(cat, "Always lactates", _pluginInstance));
                lactatToggle.BindToFunctionController<PregnancyCharaController, bool>(controller => controller.Data.AlwaysLactates, (controller, value) => controller.Data.AlwaysLactates = value);

                e.AddControl(new MakerText("Makes the character always have milk, even when not pregnant.", cat, _pluginInstance) { TextColor = hintColor });
            }
        }
    }
}
