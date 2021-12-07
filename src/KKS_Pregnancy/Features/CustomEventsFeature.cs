using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using ActionGame;
using ADV;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.MainGame;
using KKAPI.Studio;
using KKAPI.Utilities;
using SaveData;
using UnityEngine;

namespace KK_Pregnancy
{
    /// <summary>
    /// Play custom events at talk scene start like afterpill question or lettink know about conception
    /// also add related items
    /// </summary>
    [UsedImplicitly]
    public class CustomEventsFeature : IFeature
    {
        public const int AfterpillStoreId = Constants.AfterpillID;
        public const int AfterpillEventID = Constants.AfterpillID;
        public const int PregTalkEventID = Constants.PregTalkEventID;

        private static int _installed;
        public bool Install(Harmony instance, ConfigFile config)
        {
            if (StudioAPI.InsideStudio || Interlocked.Increment(ref _installed) != 1) return false;

            StoreApi.RegisterShopItem(AfterpillStoreId, "Afterpill",
                "Prevents pregnancy when used within a few days of insemination. Always smart to keep a couple of these for emergencies.",
                StoreApi.ShopType.NightOnly, StoreApi.ShopBackground.Pink, 8, 3, false, 50, 461);
            
            instance.PatchMoveNext(AccessTools.Method(typeof(TalkScene), nameof(TalkScene.Introduction)), transpiler: new HarmonyMethod(typeof(CustomEventsFeature), nameof(IntroductionTpl)));
            
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
            var preg = heroine.GetPregnancyData();
            if (preg.GameplayEnabled)
            {
                if (preg.Week > 1)
                {
                    if (!heroine.talkEvent.Contains(PregTalkEventID))
                    {
                        // todo trigger after any h scene if mc came inside, not only if preg started
                        // add more conditions? only trigger event if they pass? or just show it every time
                        // todo give a pregnancy topic for later use?

                        var scene = GameObject.FindObjectOfType<TalkScene>();
                        scene.StartADV(GetPregEvent(heroine, preg), scene.cancellation.Token).GetAwaiter().GetResult();

                        var vars = ActionScene.instance.AdvScene.Scenario.Vars;
                        ApplyStatChangeVars(heroine, preg, vars);

                        // Don't talk about this again
                        // Use same list as base game events to keep track if the event was viewed
                        heroine.talkEvent.Add(PregTalkEventID);
                    }
                }
                else
                {
                    // If the week is 0 or 1, that means it's a new unconfirmed pergenancy, so reset the event and have it fire again
                    heroine.talkEvent.Remove(PregTalkEventID);

                    if (preg.Week == 1)
                    {
                        if (!heroine.talkEvent.Contains(AfterpillEventID))
                        {
                            var scene = GameObject.FindObjectOfType<TalkScene>();
                            scene.StartADV(GetPillEvent(heroine, preg), scene.cancellation.Token).GetAwaiter().GetResult();

                            var vars = ActionScene.instance.AdvScene.Scenario.Vars;
                            ApplyStatChangeVars(heroine, preg, vars);

                            // Don't talk about this again
                            heroine.talkEvent.Add(AfterpillEventID);
                        }
                    }
                    else
                    {
                        // If the week is 0, it means the event can be played again after a new pregnancy starts
                        heroine.talkEvent.Remove(AfterpillEventID);
                    }
                }
            }
        }

        private static void ApplyStatChangeVars(Heroine heroine, PregnancyData preg, Dictionary<string, ValData> vars)
        {
            if (vars.TryGetVarValue("PillUsed", out bool used) && used)
                PregnancyGameController.StopPregnancy(heroine);

            if (vars.TryGetVarValue("PillGiven", out bool consume) && consume)
                StoreApi.SetItemAmountBought(AfterpillStoreId, Mathf.Clamp(StoreApi.GetItemAmountBought(AfterpillStoreId) - 1, 0, 99));

            if (vars.TryGetVarValue<int>("FavorChange", out var favor))
                heroine.favor = Mathf.Clamp(heroine.favor + favor, 0, 150);

            if (vars.TryGetVarValue<int>("LewdChange", out var lewd))
                heroine.lewdness = Mathf.Clamp(heroine.lewdness + lewd, 0, 100);

            if (vars.TryGetVarValue<int>("MoneyChange", out var money))
                Manager.Game.saveData.player.koikatsuPoint += money;
        }

        private static List<Program.Transfer> GetPregEvent(Heroine heroine, PregnancyData data)
        {
            // todo different text based on personality and stats (different if first vs second vs next)
            var list = EventApi.CreateNewEvent();
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Hey, uhh, I need to tell you something important."));
            list.Add(Program.Transfer.Text(EventApi.Player, "What's wrong?"));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "How do I say this... Well... My period's very late."));
            list.Add(Program.Transfer.Text(EventApi.Player, "Hmm? Wait, does that mean..."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Yeah, most likely. And you're the only one it could be with."));
            list.Add(Program.Transfer.Text(EventApi.Narrator, "She's pregnant? That's... not entirely unexpected. I'm worried about the future, but for now I think I'm just happy."));
            list.Add(Program.Transfer.Text(EventApi.Player, "I see, that's great news! Don't worry about anything, I'll be sure to take responsibility."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Yeah, I'm happy too. In that case I'll be in your care from now on."));

            list.Add(Program.Transfer.VAR("System.Int32", "FavorChange", "50"));
            list.Add(Program.Transfer.VAR("System.Int32", "MoneyChange", "200"));
            return list;
        }

        private static List<Program.Transfer> GetPillEvent(Heroine heroine, PregnancyData data)
        {
            // todo some characters don't mention it? only show event if the pill is available?
            // easiest is to always show but disable pill option if none bought (some characters already have one so you don't need to have it?)

            var list = EventApi.CreateNewEvent();

            // todo branch to not allow selecting the option and set PillConsumed approperiately
            list.Add(Program.Transfer.VAR("System.Boolean", "PillBought", StoreApi.GetItemAmountBought(AfterpillStoreId) > 0 ? "true" : "false"));

            list.Add(Program.Transfer.Text(EventApi.Narrator, "I'm woried she could get pregnant from what I did before, I should do something about this."));
            list.Add(Program.Transfer.Text(EventApi.Player, "Hey, there's something I've been worrying about."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Hmm?"));
            list.Add(Program.Transfer.Text(EventApi.Player, "Here."));
            list.Add(Program.Transfer.Text(EventApi.Narrator, "I hand her the box with the afterpill."));
            list.Add(Program.Transfer.Text(EventApi.Player, "I might be overthinking this, but I think it's better to be safe than sorry."));
            list.Add(Program.Transfer.Text(EventApi.Narrator, "She takes a look at the box, her eyes scan the printed text."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Oh."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Umm, did we really do anything that could be dangerous?"));
            list.Add(Program.Transfer.Text(EventApi.Player, "Maybe, maybe not, I just want to be extra safe. I don't want to cause you any trouble."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "It wouldn't really trobule..."));
            list.Add(Program.Transfer.Text(EventApi.Narrator, "Our eyes meet, I try to look as serious as possible."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "*sigh* Alright, I understand, I'll take it. Do tell me if you change your mind though."));

            list.Add(Program.Transfer.VAR("System.Boolean", "PillUsed", "true"));
            list.Add(Program.Transfer.VAR("System.Boolean", "PillGiven", "true"));
            list.Add(Program.Transfer.VAR("System.Int32", "FavorChange", "-30"));
            list.Add(Program.Transfer.VAR("System.Int32", "LewdChange", "-20"));
            // todo make this depend on if the character is left happy?
            list.Add(Program.Transfer.VAR("System.Int32", "MoneyChange", "20"));
            list.Add(Program.Transfer.Close());
            return list;
        }
    }
}