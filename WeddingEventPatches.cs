using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;

namespace WeddingTweaks
{
    public partial class ModEntry
    {

        private static List<int[]> allWeddingPositions = new List<int[]>
        {
            new int[]{26,63,1},
            new int[]{29,63,3},
            new int[]{25,63,1},
            new int[]{30,63,3}
        };
        public static bool startingLoadActors = false;
        private static MethodInfo? addActorMethod = null;

        public static void AddActor(Event @event, string name, int x, int y, int facingDirection, GameLocation location)
        {
            if (addActorMethod == null)
            {
                addActorMethod = @event.GetType().GetMethod("addActor", BindingFlags.NonPublic | BindingFlags.Instance);
                if (addActorMethod == null)
                {
                    // Could not find the addActor method
                    SMonitor.Log($"Event.addActor method not found");
                    return;
                }
            }

            // Signature: addActor(string name, int x, int y, int facingDirection, GameLocation location)
            addActorMethod.Invoke(@event, new object[] { name, x, y, facingDirection, location });
        }

        [HarmonyPatch(typeof(Event), "setUpCharacters")]
        public static class Event_setUpCharacters_Patch
        {
            public static void Postfix(Event __instance, GameLocation location)
            {
                try
                {
                    if (!__instance.isWedding)
                        return;

                    SMonitor.Log($"In wedding for farmer {__instance.farmer.Name}");
                    List<string> spouses = Misc.GetSpouses(__instance.farmer, 0).Keys.ToList();
                    Misc.ShuffleList(ref spouses);
                    bool addSpouses = Config.AllSpousesJoinWeddings && spouses.Count > 0 && polyamorySweetLoveAPI is not null;

              
                    var weddingPositions = new List<int[]>();
                    for (int i = 0; i < allWeddingPositions.Count; i++)
                    {
                      
                        weddingPositions.Add(allWeddingPositions[i]);
                    }

                    if (addSpouses)
                    {
                        foreach (string spouse in spouses)
                        {
                            var actor = __instance.actors.FirstOrDefault(p => p.Name == spouse);
                            if (actor == null)
                            {
                                // Probably a modded spouse, find and add to the wedding
                                SMonitor.Log($"Adding {spouse} to event");
                                AddActor(__instance, spouse, 0, 0, 0, location);
                                
                                // Search for actor again now that it should have been added to the event
                                actor = __instance.actors.FirstOrDefault(p => p.Name == spouse);
                                if (actor == null)
                                {
                                    SMonitor.Log($"Failed to add {spouse} to wedding event", LogLevel.Warn);
                                    continue;
                                }
                            }
                            
                            int idx = spouses.IndexOf(actor.Name);

                            Vector2 pos;
                            if (idx < weddingPositions.Count)
                            {
                                pos = new Vector2(weddingPositions[idx][0] * Game1.tileSize, weddingPositions[idx][1] * Game1.tileSize);
                            }
                            else
                            {
                                int x = 25 + ((idx - 4) % 6);
                                int y = 62 - ((idx - 4) / 6);
                                pos = new Vector2(x * Game1.tileSize, y * Game1.tileSize);
                            }
                            actor.position.Value = pos;
                            if (Config.AllSpousesWearMarriageClothesAtWeddings)
                            {
                                bool flipped = false;
                                int frame = 37;
                                if (pos.Y < 62 * Game1.tileSize)
                                {
                                    if (pos.X == 25 * Game1.tileSize)
                                    {
                                        flipped = true;
                                    }
                                    else if (pos.X < 30 * Game1.tileSize)
                                    {
                                        frame = 36;
                                    }

                                }
                                else if (pos.X < 28 * Game1.tileSize)
                                {
                                    flipped = true;
                                }
                                if (actor.Gender == 0)
                                {
                                    frame += 12;
                                }
                                if (npcWeddingDict.TryGetValue(actor.Name, out WeddingData data) && data.witnessFrame >= 0)
                                {
                                    frame = data.witnessFrame + 1;
                                    if (pos.X < 30 * Game1.tileSize && pos.X > 25 * Game1.tileSize)
                                    {
                                        frame--;
                                    }
                                }
                                actor.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                            {
                                new FarmerSprite.AnimationFrame(frame, 0, false, flipped, null, true)
                            });
                            }
                            else
                                Misc.facePlayerEndBehavior(actor, location);
                            continue;
                        }
                    }
                }

                catch (Exception ex)
                {
                    SMonitor.Log($"Failed in {nameof(Event_setUpCharacters_Patch)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.LoadActors))]
        public static class Event_command_loadActors_Patch
        {
            public static void Prefix()
            {
                try
                {
                    startingLoadActors = true;
                }
                catch (Exception ex)
                {
                    SMonitor.Log($"Failed in {nameof(Event_command_loadActors_Patch)}:\n{ex}", LogLevel.Error);
                }
            }

            public static void Postfix()
            {
                try
                {
                    startingLoadActors = false;
                    Game1Patches.lastGotCharacter = null;

                }
                catch (Exception ex)
                {
                    SMonitor.Log($"Failed in {nameof(Event_command_loadActors_Patch)}:\n{ex}", LogLevel.Error);
                }
            }
        }


    }
}