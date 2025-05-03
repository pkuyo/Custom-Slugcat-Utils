using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using UnityEngine;

namespace CustomSlugcatUtils.Hooks
{
    internal static class SessionHooks
    {

        private static readonly GameFeature<Color> PlayerGuideColor = new("guide_overseer_color",JsonUtils.ToColor);

        private static readonly GameFeature<string[]> StoryRegionPriority = new("guide_region_priority", JsonUtils.ToStrings);

        private static readonly GameFeature<SlugcatStats.Name> StoryRegionPriorityOverride = new("guide_region_priority_override", JsonUtils.ToExtEnum<SlugcatStats.Name>);

        private static readonly GameFeature<int> OverseerSymbol = new("guide_overseer_symbol", JsonUtils.ToInt);
        
        private static readonly GameFeature<float> ProgressionShowTendency = new("guide_progression_show_tendency", JsonUtils.ToFloat);
        
        private static readonly GameFeature<string> OverseerSymbolCustom = new("guide_overseer_symbol_custom", JsonUtils.ToString);

        private static readonly PlayerFeature<float> SpearDamage = new("spear_damage", JsonUtils.ToFloat);

        private static readonly GameFeature<RoomGuideData[]> StoryRoomInRegion = new("guide_room_in_region", (any) =>
        {
            var re = new List<RoomGuideData>();
            var list = any.AsList();
            if (list.Get(0).TryString() is { } str)
            {
                re.Add(new RoomGuideData() { region = str, room = list.GetString(1) });

            }
            else
            {
                foreach (var item in list)
                {
                    var obj = item.AsList();
                    re.Add(new RoomGuideData() { region = obj.GetString(0), room = obj.GetString(1) });
                }
            }
     
            return re.ToArray();
        });

        private static readonly GameFeature<IntVector2> SpawnPos = new("spawn_pos", (any =>
        {
            var list = any.AsList();
            return new IntVector2(list.GetInt(0), list.GetInt(1));
        }));


        public class RoomGuideData
        {
            public string region;
            public string room;
        }

        public static void OnModsInit()
        {
            On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate +=
                RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate;
            _ = new Hook(typeof(OverseerGraphics).GetProperty(nameof(OverseerGraphics.MainColor))!.GetGetMethod(),
                (Func<OverseerGraphics, Color> orig, OverseerGraphics self) =>
                {
                    if (PlayerGuideColor.TryGet(self.overseer.abstractCreature.world.game, out var color))
                        return color;
                    return orig(self);
                });
            On.OverseersWorldAI.DirectionFinder.StoryRoomInRegion += DirectionFinder_StoryRoomInRegion;
            On.OverseersWorldAI.DirectionFinder.StoryRegionPrioritys += DirectionFinder_StoryRegionPrioritys;
            On.OverseersWorldAI.DynamicGuideSymbolUpdate += OverseersWorldAI_DynamicGuideSymbolUpdate;
            On.OverseerHolograms.OverseerHologram.OverseerGuidanceSymbol += OverseerHologram_OverseerGuidanceSymbol;
            IL.OverseerCommunicationModule.ReevaluateConcern += OverseerCommunicationModule_ReevaluateConcern;
            On.Player.ThrownSpear += Player_ThrownSpear;

        }

        private static void OverseerCommunicationModule_ReevaluateConcern(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(i => i.MatchLdarg(0),
                i => i.MatchLdsfld<OverseerCommunicationModule.PlayerConcern>("None"),
                i => i.MatchStfld<OverseerCommunicationModule>("currentConcern"));
            c.GotoPrev(MoveType.After, _ => true);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((OverseerCommunicationModule self) =>
            {
                if (self.room.world.overseersWorldAI.directionFinder != null &&
                    self.room.world.overseersWorldAI.directionFinder.done && ProgressionShowTendency.TryGet(self.room.world.game,out var f))
                    self.progressionShowTendency = f;
            });

        }


        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self,spear);
            if (SpearDamage.TryGet(self, out var damage))
                spear.spearDamageBonus = damage;
        }

        private static string OverseerHologram_OverseerGuidanceSymbol(On.OverseerHolograms.OverseerHologram.orig_OverseerGuidanceSymbol orig, int selector)
        {
            var re = orig(selector);
            if (selector == -1 && Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                OverseerSymbolCustom.TryGet(game, out var name))
            {
                if (!Futile.atlasManager.DoesContainElementWithName(name))
                    Futile.atlasManager.LoadImage(name);
                return name;
            }
            return re;
        }

        private static void OverseersWorldAI_DynamicGuideSymbolUpdate(On.OverseersWorldAI.orig_DynamicGuideSymbolUpdate orig, OverseersWorldAI self)
        {
            orig(self);
            if (OverseerSymbol.TryGet(self.world.game, out var index))
                self.world.game.GetStorySession.saveState.miscWorldSaveData.playerGuideState.guideSymbol = index;
            if (OverseerSymbolCustom.TryGet(self.world.game, out _))
                self.world.game.GetStorySession.saveState.miscWorldSaveData.playerGuideState.guideSymbol = -1;
        }

        private static List<string> DirectionFinder_StoryRegionPrioritys(On.OverseersWorldAI.DirectionFinder.orig_StoryRegionPrioritys orig,
            OverseersWorldAI.DirectionFinder self, SlugcatStats.Name saveStateNumber, string currentRegion, bool metMoon, bool metPebbles)
        {
            var re = orig(self,saveStateNumber,currentRegion,metMoon,metPebbles);
            if (StoryRegionPriority.TryGet(self.world.game, out var data))
                return data.ToList();
            else if(StoryRegionPriorityOverride.TryGet(self.world.game,out var name))
                return orig(self,name,currentRegion, metMoon,metPebbles);
            return re;
        }

        private static string DirectionFinder_StoryRoomInRegion(On.OverseersWorldAI.DirectionFinder.orig_StoryRoomInRegion orig, OverseersWorldAI.DirectionFinder self, string currentRegion, bool metMoon)
        {
            var re = orig(self,currentRegion,metMoon);
            if (StoryRoomInRegion.TryGet(self.world.game, out var data))
            {
                var current = data.FirstOrDefault(i => i.region == currentRegion);
                if (current != null)
                {
                    self.showGateSymbol = false;
                    return current.room;
                }
            }

            return re;
        }

        private static AbstractCreature RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate(
            On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self,
            bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
        {
            if (SpawnPos.TryGet(self, out var pos) && self.session is StoryGameSession session &&
                session.saveState.cycleNumber == 0)
            {
                location.x = pos.x;
                location.y = pos.y;
            }

            return orig(self, player1, player2, player3, player4, location);
        }

    }
}
