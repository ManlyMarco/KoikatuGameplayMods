using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading;
using ADV;
using BepInEx.Configuration;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using KK_Pregnancy.Features;
using KKAPI.MainGame;
using KKAPI.Studio;
using KKAPI.Utilities;
using SaveData;
using UnityEngine;

namespace KK_Pregnancy
{
    /// <summary>
    /// Play custom events at talk scene start like afterpill question or letting know about conception
    /// also add related items
    /// </summary>
    [UsedImplicitly]
    public class CustomEventsFeature : IFeature
    {
        public const int AfterpillStoreId = Constants.AfterpillItemID;

        private static ConfigEntry<bool> _disablePillEvents;
        private static int _installed;
        public bool Install(Harmony instance, ConfigFile config)
        {
            if (StudioAPI.InsideStudio || Interlocked.Increment(ref _installed) != 1) return false;

            _disablePillEvents = config.Bind("General", "Disable afterpill events", false, "Disable events where heroine asks to use an after pill. Instead, automatically use a pill if player owns one, and do nothing otherwise.");

            StoreApi.RegisterShopItem(
                itemId: AfterpillStoreId,
                itemName: "Afterpill",
                explaination: "Prevents pregnancy when used within a few days of insemination. Always smart to keep a couple of these for emergencies.",
                shopType: StoreApi.ShopType.NightOnly,
                itemBackground: StoreApi.ShopBackground.Pink,
                itemCategory: 8,
                stock: 5,
                resetsDaily: false,
                cost: 50,
                sort: 461);

            instance.PatchMoveNext(
                original: AccessTools.Method(typeof(TalkScene), nameof(TalkScene.Introduction)),
                transpiler: new HarmonyMethod(typeof(CustomEventsFeature), nameof(IntroductionTpl)));

            // todo make heroine want to talk to player when theres a custom event waiting

            return true;
        }

        private static IEnumerable<CodeInstruction> IntroductionTpl(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Ldc_I4_3),
                    new CodeMatch(OpCodes.Ldc_R4),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ChaControl), nameof(ChaControl.ChangeLookNeckPtn))))
                .ThrowIfInvalid("ChangeLookNeckPtn not found")
                .Insert(new CodeInstruction(OpCodes.Dup), CodeInstruction.Call(typeof(CustomEventsFeature), nameof(IntroductionEventHook)))
                .Instructions();
        }

        private static void IntroductionEventHook(Heroine heroine)
        {
            try
            {
                var preg = heroine.GetPregnancyData();
                if (!preg.GameplayEnabled) return;

                if (preg.CanAskForAfterpill && preg.Week <= 1)
                {
                    RunIntroEvent(heroine, preg, true);
                }
                else if (preg.CanTellAboutPregnancy && preg.Week >= 6)
                {
                    RunIntroEvent(heroine, preg, false);
                }
            }
            catch (Exception e)
            {
                PregnancyPlugin.Logger.LogError(e);
            }
        }

        private static void RunIntroEvent(Heroine heroine, PregnancyData preg, bool isPillEvent)
        {
            // If pill events are disabled, silently use a pill if it has been bought, or otherwise do absolutely nothing to prevent the impending doom
            if (_disablePillEvents.Value && isPillEvent)
            {
                if (StoreApi.GetItemAmountBought(AfterpillStoreId) >= 1)
                {
                    ApplyStatChangesAfterEvent(heroine, preg, isPillEvent, new Dictionary<string, ValData> { { "PillUsed", new ValData(true) } });
                }
                return;
            }

            var loadedEvt = GetEvent(heroine, preg, isPillEvent);
            if (loadedEvt == null)
            {
                PregnancyPlugin.Logger.LogError("Unexpected null GetEvent result");
                return;
            }

            // Init needed first since the custom event starts empty
            var evt = EventApi.CreateNewEvent(setPlayerParam: true);
            heroine.SetADVParam(evt);
            var freePill = _personalityHasPills.TryGetValue(heroine.personality, out var val) && val;
            evt.Add(Program.Transfer.VAR("bool", "PillForFree", freePill.ToString(CultureInfo.InvariantCulture)));
            evt.Add(Program.Transfer.VAR("bool", "PlayerHasPill", (StoreApi.GetItemAmountBought(AfterpillStoreId) >= 1).ToString(CultureInfo.InvariantCulture)));
            // Give favor by default, gets overriden if the event specifies any other value
            evt.Add(Program.Transfer.VAR("int", "FavorChange", "30"));

            evt.AddRange(loadedEvt);

            var scene = TalkScene.instance;
            scene.StartADV(evt, scene.cancellation.Token)
                .ContinueWith(() => Program.ADVProcessingCheck(scene.cancellation.Token))
                .ContinueWith(() =>
                {
                    var vars = ActionScene.instance.AdvScene.Scenario.Vars;
                    ApplyStatChangesAfterEvent(heroine, preg, isPillEvent, vars);

                    // Fix mouth getting permanently locked by the events
                    heroine.chaCtrl.ChangeMouthFixed(false);
                })
                .Forget();
        }

        private static void ApplyStatChangesAfterEvent(Heroine heroine, PregnancyData preg, bool isPillEvent, Dictionary<string, ValData> vars)
        {
            PregnancyGameController.ApplyToAllDatas((chara, data) =>
            {
                if (chara == heroine)
                {
                    if (isPillEvent) data.CanAskForAfterpill = false;
                    else data.CanTellAboutPregnancy = false;
                    return true;
                }

                return false;
            });

            ApplyStatChangeVars(heroine, preg, vars);
        }

        private static void ApplyStatChangeVars(Heroine heroine, PregnancyData preg, Dictionary<string, ValData> vars)
        {
            if (vars.TryGetVarValue("PillUsed", out bool used) && used)
            {
                PregnancyGameController.ForceStopPregnancyDelayed(heroine);

                var freePill = _personalityHasPills.TryGetValue(heroine.personality, out var val) && val;
                if (!freePill)
                    StoreApi.SetItemAmountBought(AfterpillStoreId, Mathf.Clamp(StoreApi.GetItemAmountBought(AfterpillStoreId) - 1, 0, 99));
            }

            if (vars.TryGetVarValue<int>("FavorChange", out var favor))
                heroine.favor = Mathf.Clamp(heroine.favor + favor, 0, 150);

            if (vars.TryGetVarValue<int>("LewdChange", out var lewd))
                heroine.lewdness = Mathf.Clamp(heroine.lewdness + lewd, 0, 100);

            if (vars.TryGetVarValue<int>("MoneyChange", out var money))
                Manager.Game.saveData.player.koikatsuPoint += money;
        }

        private static List<Program.Transfer> GetEvent(Heroine heroine, PregnancyData data, bool isPill)
        {
            var personalityNo = _personalityHasPills.ContainsKey(heroine.personality) ? heroine.personality : 99;

            var evtName = $"c{personalityNo:D2}_";
            if (isPill)
                evtName += "AS_";
            else if (data.PregnancyCount <= 1)
                evtName += "PREG_";
            else
                evtName += "XPREG_";

            var pattern = evtName + @"[\w\d ]+.csv$";

            var containingAssembly = typeof(CustomEventsFeature).Assembly;
            var evtResourceName = containingAssembly.GetManifestResourceNames().SingleOrDefault(fname => Regex.IsMatch(fname, pattern));
            if (evtResourceName == null) throw new IOException($"Pattern {pattern} did not match any resources inside assembly {containingAssembly}");

            using (var resourceStream = containingAssembly.GetManifestResourceStream(evtResourceName))
            using (var reader = new StreamReader(resourceStream ?? throw new InvalidOperationException($"{evtResourceName} not found in assembly {containingAssembly}")))
            {
                var lines = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var paramList = CsvHelper.ReadFomCsv(lines);
                return paramList.Select(x => new Program.Transfer(x)).ToList();
            }
        }

        private static readonly IReadOnlyDictionary<int, bool> _personalityHasPills = new Dictionary<int, bool>
        {
            { 04, false },
            { 07, false },
            { 08, false },
            { 11, false },
            { 13, false },
            { 14, false },
            { 15, false },
            { 18, false },
            { 19, false },
            { 20, false },
            { 22, false },
            { 24, false },
            { 25, false },
            { 27, true  },
            { 29, true  },
            { 32, false },
            { 33, true  },
            { 34, true  },
            { 36, false },
            { 37, false },
            { 99, false },
        };
    }
}