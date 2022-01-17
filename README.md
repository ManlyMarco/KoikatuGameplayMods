# Introduction
This repository contains a random assortment of plugins that modify the gameplay of Koikatu!

## How to download
Go to [releases](https://github.com/ManlyMarco/KoikatuGameplayMods/releases) and look for the latest release of the plugin you want. You can also compile it from source (instructions below).

## How to install
Almost all plugins are installed in the same way. If there are any extra steps needed they will be added to the plugin descriptions below.
1. Make sure you have at least BepInEx 5.1 and latest BepisPlugins and KKAPI.
2. Download the latest release of the plugin you want.
3. Extract the archive into your game directory. The plugin .dll should end up inside your BepInEx\plugins directory.
4. Check if there are no warnings on game startup, if the plugin has settings it should appear in plugin settings.

## Compiling
Simply clone this repository to your drive and use the free version of Visual Studio 2019 for C# to compile it. Hit build and all necessary dependencies should be automatically downloaded. Check the following links for useful tutorials. If you are having trouble or want to try to make your own plugin/mod, feel free to ask for help in modding channels of either the [Koikatsu](https://discord.gg/hevygx6) or [IllusionSoft](https://discord.gg/F3bDEFE) Discord servers.
- https://help.github.com/en/github/creating-cloning-and-archiving-repositories/cloning-a-repository
- https://docs.microsoft.com/en-us/visualstudio/get-started/csharp/?view=vs-2019
- https://docs.microsoft.com/en-us/visualstudio/ide/troubleshooting-broken-references?view=vs-2019

## Specific plugin descriptions

# Koikatu Gameplay Mod
Plugin that tweaks and (hopefully) improves Koikatu gameplay.

### Some of the features
- Character limit at school is raised from the stock 38 to 99.
- Virgin H scenes can now be quit early. The girl stays a virgin in that case.
- It's possible to force raw even when denied. Can cause anger.
- Lewdness drops or raises depending on what happened in the H scene.
- Everyone's levdness drops naturally, needs to be built back up.
- Stats slowly fall, need to be worked on even after maxxing.
- Fast travel with F3 now costs time.
- Prevent hiding of player body when touching in H scenes.

**Warning:** Setting the character limit above 38 requires a large amount of RAM and will extend loading times. To load 99 characters the game will need approximately 10GB of free RAM (depending on the characters used).
**Warning:** Compatibility with kPlug is shaky at best. You might be able to get both plugins to work at the same time but I can't check the compatibility and I'm unable to fix any compatibility issues that aren't caused by issues in this plugin.

![preview](https://user-images.githubusercontent.com/39247311/50426454-0c860a00-088e-11e9-85d0-493db814cc48.png)

# KK_Pregnancy / Pregnancy mod
Plugin that adds pregnancy gameplay to the main game with related maker customization options. It can also show and change the characters' menstruation schedules.

### How does it work?
Busting a nut inside on a dangerous day has a chance to make the girl pregnant. After a few days a pregnant status icon will appear on character lists and the girl will stop having dangerous days. Over the next x weeks (configurable in plugin settings) the belly will grow, and then the girl stop coming to school for some time (she will eventually return to normal).

The plugin can be configured in plugin settings and per-character in female chara maker. The default conception chance (fertility) is around 30% and can be changed per-character from main-menu maker (pregnancy settings cannot be changed from class maker to prevent cheating). Conception can be completely disabled per-character and globally.

Because the pregnant effect is made by manipulating bones, there are some limitations and potential issues. On some characters the effect might look bad, especially if they use many ABMX sliders around the belly area. Clothes that use skirt bones (skirts, dresses, etc.) can look glitchy, which is sadly unavoidable without tweaking each character manually in maker. Clothes that stick close to the body will work best.

![preview](https://user-images.githubusercontent.com/39247311/60744379-f8764000-9f75-11e9-886b-be5e74448258.png)

# OrthographicCamera
Plugin that allows using of the orthographic (parallel projection) camera mode. Works in both Studio and main game/maker. (This is the effect that is used in isometric games like for example Diablo 2 and Fallout 2)

To toggle between perspective (normal) and orthographic camera mode press the I key (can be changed in settings) and then use your mouse scroll wheel to zoom in and out.

![preview](https://user-images.githubusercontent.com/39247311/59981520-dd661080-9604-11e9-9b2b-eefbd1a1a66b.png)

# StudioCameraObjectTweaks
Plugin that tweaks to how camera objects work in studio (the ones created by pressing the camera button). It makes the camera objects spawn at the position of the current camera view, instead of at the cursor, and it hides the spawned cameras by default (configurable in plugin settings).

# KK_MobAdder
This plugin adds random mob characters across maps in story mode in order to make it feel less vacant. The mobs have simple animations, no annoying sounds and don't move from their spots - they are purely for decoration.

**Notes:**
- You can disable the mobs and change their color from the game's settings screen, look for mob settings near the bottom (the same settings that control mobs on the train map). You need to reenter the current map to see the change.
- The amount of spawned mobs varies by time of the day and location. It can be adjusted in plugin settings, and by editing the included spread.csv file if you want to fine-tune it.
- You can add more mob spawn points if you want to. Go to plugin settings and assign both of the plugin's hotkeys, check their descriptions as well. If you add a bunch of spawn points please consider sharing your positions.csv file so everyone can benefit!

![Preview](https://user-images.githubusercontent.com/39247311/77672415-2cbce100-6f89-11ea-8351-63a1465dcc0e.png)

# KK_NightDarkener
Plugin that darkens the map in Free H scenes set at night to something slightly more realistic. Configurable, can go down to horror levels of darkness.

Inspired by PHmod44_KK_Dark_Map_Ver mod, thanks to HCM06 for bringing it up.

![Preview](https://user-images.githubusercontent.com/39247311/55674510-07395200-58b6-11e9-8b85-d15f8fab54fa.png)

# KK_WarpToCharacters
This plugin adds a "warp to this character" button to the character roster in roaming mode (press middle mouse button and select the clipboard icon from the menu). You can also warp to the next story event (it will appear at the top center).

This plugin is a replacement/upgrade for KK_MoveMapFromCharaList. Compared to its precedesor, this plugin warps the player right next to the target, has a nicer icon that only appears if characters are on a different map than the player, and uses BepInEx5 instead of IPA.

# KK_Bulge
Automatically adds crotch bulges to characters with fun sticks when they are wearing clothes. Can be configured globally, and per-character for when you want the bulge size to be just right.

# KK_LewdCrestX
A plugin for Koikatsu that lets you give characters lewd crests in character maker and in story mode. Many crests have actual gameplay effects, while others are only for flavor. All crests can be used in character maker and studio. There are 32 different crests at the moment.

Inspired by the original KK_LewdCrest plugin by picolet21. Crests were drawn and described by novaksus.

### How to use
1. Either install KK HF Patch v3.7 (or later), or manually update these plugins (to specified version or newer): BepInEx 5.4.4, BepisPlugins r16, KKAPI 1.15, KKABMX 4.0, OverlayMods 5.2, KK_Pregnancy 2.3.1, and optionally AutoTranslator (can be not installed at all).
2. Download the latest release.
3. Extract the archive into your game directory. The plugin .dll should end up inside your BepInEx\plugins directory.
4. Check if there are no warnings on game startup. A new "Crest" category should appear in character maker in the same tab as character name (check preview screenshots). To give characters crests in story mode, invite them to the club and look for a new action icon in the clubroom. If you applied a crest to the character in maker then it will already be applied when you add the character to story mode. To give crests in studio, select character and scroll down in the "anim / Current State" menu (check preview pictures).

# MoreShopItems
This plugin adds new things to buy in the shop. Each item has a different effect or use.

New items should appear in the main game shop (on the Harbor map). Some items are only available during the Night period).

### How to use
1. Install latest versions of BepInEx, BepisPlugins, KKSAPI. At least KKSAPI v1.31.1 is required as of v1.0.
2. Download the latest release.
3. Extract the archive into your game directory. The plugin .dll should end up inside your BepInEx\plugins directory.
4. Check if there are no warnings on game startup. Load into the main game and go to the shop, you should see new items (some will only appear in night shop).
