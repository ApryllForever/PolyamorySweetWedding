using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System;

namespace WeddingTweaks
{
    public partial class ModEntry
    {
        /*
        [HarmonyPatch(typeof(NPC), nameof(NPC.checkAction))]
        public static class NPC_checkAction_Patch
        {
            public static void Postfix(NPC __instance, Farmer who, ref bool __result, GameLocation l)
            {
                if (!Config.EnableMod || __result || !who.IsLocalPlayer || !who.isEngaged() || npcWitnessAsked.Contains(__instance.Name) || who.modData.ContainsKey(witnessKey) || !who.friendshipData.TryGetValue(__instance.Name, out Friendship f) || f.IsEngaged() )
                    return;

                SMonitor.Log($"Asking about witness for {__instance.Name}");
                var responses = l.createYesNoResponses();
                responses[0].responseKey = __instance.Name;
                l.createQuestionDialogue(string.Format(SHelper.Translation.Get("ask-x-to-witness"), __instance.Name), responses, "WeddingWitness", null);
                __result = true;
            }
        }
        */
        [HarmonyPatch(typeof(NPC), "engagementResponse")]
        public static class NPC_engagementResponse_Patch
        {

            public static void Postfix(NPC __instance, Farmer who, bool asRoommate = false)
            {
                if (asRoommate)
                {
                    SMonitor.Log($"{__instance.Name} is roomate");
                    return;
                }
                if (!who.friendshipData.ContainsKey(__instance.Name))
                {
                    SMonitor.Log($"{who.Name} has no friendship data for {__instance.Name}", LogLevel.Error);
                    return;
                }
                Friendship friendship = who.friendshipData[__instance.Name];
                WorldDate weddingDate = new WorldDate(Game1.Date);
                weddingDate.TotalDays += Math.Max(1, Config.DaysUntilMarriage);
                while (!Game1.canHaveWeddingOnDay(weddingDate.DayOfMonth, weddingDate.Season))
                {
                    weddingDate.TotalDays++;
                }
                friendship.WeddingDate = weddingDate;
            }
        }
    }
}