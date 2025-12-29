using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JollyCoop.JollyMenu;
using Mono.Cecil.Cil;
using On.JollyCoop.JollyHUD;
using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using UnityEngine;
using JollyMeter = JollyCoop.JollyHUD.JollyMeter;

namespace CustomSlugcatUtils.Hooks
{
    internal static class CoopHooks
    {
        private static readonly PlayerFeature<Color> OverrideIconColor = new("override_icon_color", JsonUtils.ToColor);
        private static readonly PlayerFeature<Color> OverridePupColor = new("override_coop_color", JsonUtils.ToColor);

        private static readonly string[] IconName = new[] { "_icon_kill", "_icon" };
        
        public static void LoadAssets()
        {
            foreach (var id in SlugcatStats.Name.values.entries)
            {
                foreach (var icon in IconName)
                {
                    if (File.Exists(AssetManager.ResolveFilePath($"atlas/{id}{icon}.png")) &&
                        !Futile.atlasManager.DoesContainElementWithName($"atlas/{id}{icon}"))
                    {
                        Futile.atlasManager.LoadImage($"atlas/{id}{icon}");
                        Plugin.Log($"Load icon: {id}{icon}");
                    }
                }
            }
        }

        private static SlugcatStats.Name GetRealSlugcatCharacter(AbstractCreature abstrPlayer)
        {
            SlugcatStats.Name name;
            if (abstrPlayer.realizedCreature is Player player)
            {
                name = player.slugcatStats.name;
            }
            else
            {
                name = (abstrPlayer.state as PlayerState).slugcatCharacter;
            }
            return GetRealSlugcatCharacter(name);
        }
        private static SlugcatStats.Name GetRealSlugcatCharacter(SlugcatStats.Name name)
        {
            if (name.value.StartsWith("JollyPlayer"))
            {
                int id = int.Parse(name.value.Replace("JollyPlayer", "")) - 1;
                SlugcatStats.Name realChar = RWCustom.Custom.rainWorld.options.jollyPlayerOptionsArray[id].playerClass;
                return realChar;
            }
            return name;
        }
        
        public static void OnModsInit()
        {
            IL.JollyCoop.JollyMenu.JollyPlayerSelector.ctor += JollyPlayerSelector_ctor;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += JollyPlayerSelector_GetPupButtonOffName;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.GrafUpdate += JollyPlayerSelector_GrafUpdate;

            IL.Menu.FastTravelScreen.SpawnSlugcatButtons += FastTravelScreen_SpawnSlugcatButtons;
            IL.Menu.MultiplayerMenu.PopulateSafariSlugcatButtons += MultiplayerMenu_PopulateSafariSlugcatButtons;
            On.CreatureSymbol.SymbolDataFromCreature += CreatureSymbol_SymbolDataFromCreature;
            On.CreatureSymbol.SpriteNameOfCreature += CreatureSymbol_SpriteNameOfCreature;      
            On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.ctor += PlayerIcon_ctor;
            IL.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.Update += PlayerIcon_Update;
            On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump.ctor += JollyDeathBump_ctor;
            On.HUD.PlayerSpecificMultiplayerHud.PlayerDeathBump.ctor += PlayerDeathBump_ctor;

        }

        private static void PlayerDeathBump_ctor(On.HUD.PlayerSpecificMultiplayerHud.PlayerDeathBump.orig_ctor orig, HUD.PlayerSpecificMultiplayerHud.PlayerDeathBump self, HUD.PlayerSpecificMultiplayerHud owner)
        {
            orig.Invoke(self, owner);
            SlugcatStats.Name slugChar = GetRealSlugcatCharacter(owner.abstractPlayer);
            if (Futile.atlasManager.DoesContainElementWithName($"atlas/{slugChar}_icon_kill"))
            {
                self.symbolSprite.element = Futile.atlasManager.GetElementWithName(
                    $"atlas/{slugChar}_icon_kill");
            }
        }

        private static void PlayerIcon_Update(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchStfld<JollyMeter.PlayerIcon>("color"));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<JollyMeter.PlayerIcon>>(self =>
                {
                    if (SlugBaseCharacter.TryGet(self.playerState.slugcatCharacter,
                            out var character) && 
                        OverrideIconColor.TryGet(character, out var icon))
                        self.color = icon;
                });

                c.GotoNext(MoveType.After, i => i.MatchLdstr("Multiplayer_Death"));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<string, JollyMeter.PlayerIcon, string>>((str, self) =>
                {
                    SlugcatStats.Name slugChar = GetRealSlugcatCharacter(self.player);
                    if (Futile.atlasManager.DoesContainElementWithName(
                            $"atlas/{slugChar}_icon_kill"))
                        return $"atlas/{slugChar}_icon_kill";
                    return str;
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void JollyDeathBump_ctor(JollyPlayerSpecificHud.JollyDeathBump.orig_ctor orig, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump self, JollyCoop.JollyHUD.JollyPlayerSpecificHud jollyhud)
        {
            orig(self, jollyhud);
            SlugcatStats.Name slugChar = GetRealSlugcatCharacter(jollyhud.abstractPlayer);
            if (Futile.atlasManager.DoesContainElementWithName(
                    $"atlas/{slugChar}_icon_kill"))
            {
                self.symbolSprite.element = Futile.atlasManager.GetElementWithName(
                    $"atlas/{slugChar}_icon_kill");
                if (SlugBaseCharacter.TryGet(slugChar,
                        out var character) && 
                    OverrideIconColor.TryGet(character, out var icon))
                    self.symbolSprite.color = icon;
            }
                
        }


  
        
        private static void JollyPlayerSelector_ctor(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After, i => i.MatchLdstr("pup_on")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<string, JollyPlayerSelector, string>>((re, self) =>
                {
                    var id = (self.JollyOptions(self.index).playerClass ?? SlugcatStats.Name.White).value;
                    if (File.Exists(AssetManager.ResolveFilePath($"Illustrations/{id}_on.png")))
                        return $"{id}_on";
                    return re;
                });
            }
        }

        private static void JollyPlayerSelector_GrafUpdate(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GrafUpdate orig, JollyCoop.JollyMenu.JollyPlayerSelector self, float timeStacker)
        {
            orig(self, timeStacker);
            if (SlugBaseCharacter.TryGet(self.JollyOptions(self.index).playerClass ?? SlugcatStats.Name.White,out var character) &&
                OverridePupColor.TryGet(character, out var icon))
                self.pupButton.symbol.sprite.color = self.FadePortraitSprite(icon, timeStacker);

        }
        
        private static void FastTravelScreen_SpawnSlugcatButtons(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchLdstr("Kill_Slugcat"));
            var index = il.Body.Variables.First(i => i.VariableType.FullName.Contains("SlugcatStats")).Index;
            c.Emit(OpCodes.Ldloc_S, (byte)index);
            c.EmitDelegate<Func<string, SlugcatStats.Name, string>>((s, id) =>
                Futile.atlasManager.DoesContainElementWithName($"atlas/{GetRealSlugcatCharacter(id)}_icon") ? $"atlas/{GetRealSlugcatCharacter(id)}_icon" : s);

            c.GotoNext(MoveType.After, i => i.MatchCall<PlayerGraphics>("DefaultSlugcatColor"));
            c.Emit(OpCodes.Ldloc_S, (byte)index);
            c.EmitDelegate<Func<Color, SlugcatStats.Name, Color>>((s, name) =>
                SlugBaseCharacter.TryGet(name , out var character) && 
                OverrideIconColor.TryGet(character, out var icon) 
                ? icon : s);
        }
        
        private static void PlayerIcon_ctor(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_ctor orig, JollyCoop.JollyHUD.JollyMeter.PlayerIcon self, JollyCoop.JollyHUD.JollyMeter meter, AbstractCreature associatedPlayer, Color color)
        {
            if (associatedPlayer.state is PlayerState state1 && SlugBaseCharacter.TryGet(state1.slugcatCharacter, out var character) &&
                OverrideIconColor.TryGet(character, out var col))
                color = col;
            
            orig(self, meter, associatedPlayer, color);
            if (associatedPlayer.state is PlayerState state &&
                Futile.atlasManager.DoesContainElementWithName($"atlas/{GetRealSlugcatCharacter(associatedPlayer)}_icon"))
            {
                self.iconSprite.element =
                    Futile.atlasManager.GetElementWithName($"atlas/{GetRealSlugcatCharacter(associatedPlayer)}_icon");
         
            }
        }

        private static void MultiplayerMenu_PopulateSafariSlugcatButtons(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchLdstr("Kill_Slugcat"));
            var index = il.Body.Variables.First(i => i.VariableType.FullName.Contains("SlugcatStats")).Index;
            c.Emit(OpCodes.Ldloc_S, (byte)index);
            c.EmitDelegate<Func<string, SlugcatStats.Name, string>>((s, id) =>
                Futile.atlasManager.DoesContainElementWithName($"atlas/{GetRealSlugcatCharacter(id)}_icon") ? $"atlas/{GetRealSlugcatCharacter(id)}_icon" : s);
        }

        private static string JollyPlayerSelector_GetPupButtonOffName(
            On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig,
            JollyCoop.JollyMenu.JollyPlayerSelector self)
        {
            var re = orig(self);
            var id = (self.JollyOptions(self.index).playerClass ?? SlugcatStats.Name.White).value;

            if (self.pupButton != null)
                self.pupButton.symbolNameOn = File.Exists(AssetManager.ResolveFilePath($"Illustrations/{id}_on.png"))
                    ? $"{id}_on"
                    : "pup_on";
            return File.Exists(AssetManager.ResolveFilePath($"Illustrations/{id}_off.png")) ? $"{id}_off" : re;


        }
        private static string CreatureSymbol_SpriteNameOfCreature(On.CreatureSymbol.orig_SpriteNameOfCreature orig, IconSymbol.IconSymbolData iconData)
        {
            if (iconData.intData == -1 && iconData.critType != null && iconData.critType.value.StartsWith("CSU_atlas"))
                return iconData.critType.value.Replace("CSU_","");
            return orig(iconData);
        }

        private static IconSymbol.IconSymbolData CreatureSymbol_SymbolDataFromCreature(On.CreatureSymbol.orig_SymbolDataFromCreature orig, AbstractCreature creature)
        {
            if (creature.state is PlayerState state && Futile.atlasManager.DoesContainElementWithName($"atlas/{GetRealSlugcatCharacter(creature)}_icon"))
                return new IconSymbol.IconSymbolData(new CreatureTemplate.Type($"CSU_atlas/{GetRealSlugcatCharacter(creature)}_icon"), AbstractPhysicalObject.AbstractObjectType.Creature, -1);
            return orig(creature);
        }
    }
}
