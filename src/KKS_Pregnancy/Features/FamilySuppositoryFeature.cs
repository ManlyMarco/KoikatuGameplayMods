using System.Collections.Generic;
using System.Threading;
using ActionGame.Communication;
using ADV;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI.MainGame;
using KKAPI.Studio;
using KKAPI.Utilities;

namespace KK_Pregnancy
{
    // todo rethink how this is presented as and how it works
    [UsedImplicitly]
    public class FamilySuppositoryFeature : IFeature
    {
        public const int SuppositoryStoreId = Constants.FamilySuppositoryID;
        public const int SuppositoryTopicId = Constants.FamilySuppositoryID;

        private static int _installed;

        public bool Install(Harmony instance, ConfigFile config)
        {
            if (StudioAPI.InsideStudio || Interlocked.Increment(ref _installed) != 1) return false;

            var insertCat = StoreApi.RegisterShopItemCategory(ResourceUtils.GetEmbeddedResource("icon_insertable.png").LoadTexture());

            StoreApi.RegisterShopItem(SuppositoryStoreId, "Family Making Tampons",
                "Cum delivery devices disguised as a pack of tampons. Thanks to multiple patented technologies the conception rate is close to 100%. Perfect as a gift. (Doesn't work on infertile characters)",
                StoreApi.ShopType.NightOnly, StoreApi.ShopBackground.Pink, insertCat, 3, true, 100, 461, onBought: item => TopicApi.AddTopicToInventory(SuppositoryTopicId));

            TopicApi.RegisterTopic(SuppositoryTopicId, "Family Making Tampons", TopicApi.TopicCategory.Love, TopicApi.TopicRarity.Rarity5, GetAdvScript, GetAdvResult);

            return true;
        }

        private static List<Program.Transfer> GetAdvScript(TalkScene scene, int topicno, int personality, bool isnpc)
        {
            // todo variations for rejectng, already pregnant (don't need them anymore)
            var list = EventApi.CreateNewEvent();
            list.Add(Program.Transfer.Text(EventApi.Narrator, "I activated these earlier with my cum, I wonder if I should try giving this pack to her as a gift..."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Hmm? What's wrong?"));
            list.Add(Program.Transfer.Text(EventApi.Player, "Umm, actually I bought you a gift, but it might be a bit weird."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Haha, don't worry you're plenty weird anyways. So, what's the gift?"));
            list.Add(Program.Transfer.Text(EventApi.Narrator, "I take out the box and offer it to her. There was a replacement seal included in the box so it looks unopened."));
            list.Add(Program.Transfer.Text(EventApi.Player, "Here."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Huh? Are those..?"));
            list.Add(Program.Transfer.Text(EventApi.Narrator, "She stares at the box in silence for a good while."));
            list.Add(Program.Transfer.Text(EventApi.Player, "It's too weird after all. Sorry, can you forget about-"));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Ah, no it's fine! I was just surprised since I didn't know guys knew about these things."));
            list.Add(Program.Transfer.Text(EventApi.Player, "Yeah, I guess that's true."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "I use similar looking ones so they should be fine, I'll take them. You won't have any use for them anyways, haha."));
            list.Add(Program.Transfer.Text(EventApi.Player, "Really? Here you go then."));
            list.Add(Program.Transfer.Text(EventApi.Narrator, "She carefully takes the box from my hand."));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Thanks!"));
            list.Add(Program.Transfer.Text(EventApi.Heroine, "Ahh, just so you know, you shouldn't give these to other girls! You got lucky with me, but others might think it's sexual harassment and kick you in the groin."));
            list.Add(Program.Transfer.Text(EventApi.Player, "I-I'll keep that in mind."));
            list.Add(Program.Transfer.VAR("System.Boolean", "Success", "true"));
            list.Add(Program.Transfer.VAR("System.Int32", "FavorChange", "20"));
            list.Add(Program.Transfer.VAR("System.Int32", "LewdChange", "10"));
            list.Add(Program.Transfer.Close());
            return list;
        }

        private static ChangeValueTopicInfo GetAdvResult(TalkScene scene, int topicno, int personality, bool isnpc, Dictionary<string, ValData> advvars)
        {
            if (advvars.TryGetVarValue<bool>("Success", out var passed) && passed)
            {
                var data = scene.targetHeroine.GetPregnancyData();
                if (data.GameplayEnabled && !data.IsPregnant)
                    PregnancyGameController.StartPregnancy(scene.targetHeroine);
            }

            advvars.TryGetVarValue<int>("FavorChange", out var favor);
            advvars.TryGetVarValue<int>("LewdChange", out var lewd);
            return new ChangeValueTopicInfo(favor, lewd);
        }
    }
}