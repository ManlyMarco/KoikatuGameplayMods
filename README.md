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

**Warning:** Setting the character limit above 38 requires a large amount of RAM and will extend loading times. To load 99 characters the game will need approximately 10GB of free RAM (depending on the characters used).

### Requirements
Works under BepInEx with Harmony. Needs latest BepisPlugins - universal ConfigurationManager is used for mod settings.

![preview](https://user-images.githubusercontent.com/39247311/50426454-0c860a00-088e-11e9-85d0-493db814cc48.png)

# KK_Pregnancy / Pregnancy mod
Plugin that adds pregnancy gameplay to the main game with related maker customization options. It can also show and change the characters' menstruation schedules.

### How does it work?
Busting a nut inside on a dangerous day has a chance to make the girl pregnant. After a few days a pregnant status icon will appear on character lists and the girl will stop having dangerous days. Over the next x weeks (configurable in plugin settings) the belly will grow, and then the girl stop coming to school for some time (she will eventually return to normal).

The plugin can be configured in plugin settings and per-character in female chara maker. The default conception chance (fertility) is around 30% and can be changed per-character from main-menu maker (pregnancy settings cannot be changed from class maker to prevent cheating). Conception can be completely disabled per-character and globally.

Because the pregnant effect is made by manipulating bones, there are some limitations and potential issues. On some characters the effect might look bad, especially if they use many ABMX sliders around the belly area. Clothes that use skirt bones (skirts, dresses, etc.) can look glitchy, which is sadly unavoidable without tweaking each character manually in maker. Clothes that stick close to the body will work best.

### How to install
1. Needs BepInEx 5.x and latest BepisPlugins, KKAPI and KKABMX. Update these as needed.
2. Download the latest release from [releases](https://github.com/ManlyMarco/Koikatu-Gameplay-Mod/releases).
3. To install copy the dll to your BepInEx\plugins directory.
4. Check if there are no warnings on game startup, Pregnancy options should appear in plugin settings and in female chara maker.

![preview](https://user-images.githubusercontent.com/39247311/60744379-f8764000-9f75-11e9-886b-be5e74448258.png)

# KK_NightDarkener
Plugin that darkens the night scenes to something slightly more realistic. Configurable, can go down to horror levels of darkness.

Needs BepInEx and BepisPlugins, to install copy the dll to your BepInEx directory.

Inspired by PHmod44_KK_Dark_Map_Ver mod, thanks to HCM06 for bringing it up.

### Requirements
Works under BepInEx with Harmony. Needs latest BepisPlugins - universal ConfigurationManager is used for mod settings.

![Preview](https://user-images.githubusercontent.com/39247311/55674510-07395200-58b6-11e9-8b85-d15f8fab54fa.png)

# KK_OrthographicCamera
Plugin that allows using of the orthographic (parallel projection) camera mode. Works in both Studio and main game/maker. (This is the effect that is used in isometric games like for example Diablo 2 and Fallout 2)

### How to use
- Needs BepInEx v5.x
- Place the .dll inside your `BepInEx\plugins` folder.
- To toggle between perspective (normal) and orthographic camera mode press the I key (can be changed in settings) and then use your mouse scroll wheel to zoom in and out.

![preview](https://user-images.githubusercontent.com/39247311/59981520-dd661080-9604-11e9-9b2b-eefbd1a1a66b.png)
