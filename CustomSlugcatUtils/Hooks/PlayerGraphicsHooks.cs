using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;

namespace CustomSlugcatUtils.Hooks
{


    internal static partial class PlayerGraphicsHooks
    {
        public static void OnModsInit()
        {
            LoadAssets();
            modules = new ConditionalWeakTable<PlayerGraphics, PlayerGraphicsModule>();
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;

            On.Menu.MainMenu.ctor += MainMenu_ctor;
            Plugin.Log("Skin Hooks Loaded");
        }

        private static bool postPostLoaded;
        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);
            if (!postPostLoaded)
            {
                CoopHooks.LoadAssets();

                On.RoomCamera.SpriteLeaser.Update += SpriteLeaser_Update;
                postPostLoaded = true;
                Plugin.Log("Post-Skin Hooks Loaded");
            }

        }

        private static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(self,timeStacker,rCam,camPos);
            if (self.drawableObject is PlayerGraphics graphics)
            {
                if (modules.TryGetValue(graphics, out var _))
                {
                    if (self.sprites[11].element.name != "pixel")
                        self.sprites[11].scale = 1;
                }
            }
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self,
            PhysicalObject ow)
        {
            orig(self, ow);
            if (!modules.TryGetValue(self, out _) && customSkins.Contains(self.player.slugcatStats.name))
            {
                modules.Add(self, new PlayerGraphicsModule(self));
            }
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig,
            PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (modules.TryGetValue(self, out var module))
                module.InitSprites(sLeaser, rCam);
        }




        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (modules.TryGetValue(self, out var module))
                module.DrawSprites(sLeaser, rCam, timeStacker, camPos);


        }

        private static ConditionalWeakTable<PlayerGraphics, PlayerGraphicsModule> modules;
    }

    class PlayerGraphicsModule
    {

        public PlayerGraphicsModule(PlayerGraphics self)
        {
            playerRef = new WeakReference<PlayerGraphics>(self);
        }

        public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!playerRef.TryGetTarget(out var self))
                return;
            startIndex = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, startIndex + 0);
            if(Futile.atlasManager.DoesContainElementWithName($"{self.player.slugcatStats.name}TailTexture"))
                sLeaser.sprites[2].element = Futile.atlasManager.GetElementWithName($"{self.player.slugcatStats.name}TailTexture");
        }


        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!playerRef.TryGetTarget(out var self))
                return;

            if (self.player.flipDirection == 1)
            {
                sLeaser.sprites[5].MoveBehindOtherNode(sLeaser.sprites[3]);
                sLeaser.sprites[6].MoveBehindOtherNode(sLeaser.sprites[0]);
            }
            else
            {
                sLeaser.sprites[5].MoveBehindOtherNode(sLeaser.sprites[0]);
                sLeaser.sprites[6].MoveBehindOtherNode(sLeaser.sprites[3]);
            }

            Vector2 drawPos0 = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 drawPos1 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            Vector2 headPos = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
            var moveDeg = Mathf.Clamp(Custom.AimFromOneVectorToAnother(Vector2.zero, (headPos - drawPos1).normalized), -30f, 30f);

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                var str = sLeaser.sprites[i].element.name.StartsWith(self.player.slugcatStats.name.value)
                    ? sLeaser.sprites[i].element.name.Replace(self.player.slugcatStats.name.value, "")
                    : sLeaser.sprites[i].element.name;
                str = str.Replace("Left", "").Replace("Right", "")
                    .Replace("PFace", "Face").Replace("HeadC", "HeadA")
                    .Replace("HeadD", "HeadB");
                var sprite = sLeaser.sprites[i];

                if (i == 11 && !str.StartsWith("pixel"))
                    sprite.scale = 1;
                if (str.StartsWith("PlayerArm") ||
                    str.StartsWith("Face") ||
                    str.StartsWith("Head") ||
                    str.StartsWith("Legs") ||
                    str.StartsWith("Hips") ||
                    str.StartsWith("Body") ||
                    str.StartsWith("OnTopOfTerrainHand") ||
                    str.StartsWith("pixel"))
                {
                    if(str.StartsWith("pixel") && i != 11)
                        continue;

                    UpdateSprite(str, sprite, i, moveDeg, self);
                }
            }

            var tail = sLeaser.sprites[2] as TriangleMesh;

            float start = 0f;
            float end = 1f;
            const bool isAsym = true;
            if (isAsym)
            {
                end = 3f;
                Vector2 pos = self.legs.pos;
                Vector2 pos2 = self.tail[0].pos;
                float value = pos2.x - pos.x;
                float t = Mathf.InverseLerp(-15f, 15f, value);
                start = Mathf.Lerp(0f, tail.element.uvTopRight.y - tail.element.uvTopRight.y / end, t);
            }
            for (int i = tail.vertices.Length - 1; i >= 0; i--)
            {
                var step = (i / 2) / (float)(tail.vertices.Length / 2);
                var uv = new Vector2(step, i % 2);
                uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
                uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y + start, tail.element.uvTopRight.y / end + start, uv.y);
                tail.UVvertices[i] = uv;
            }

            {
                Vector2 vector4 = (drawPos1 * 3f + drawPos0) / 4f;
                float d = 1f - 0.2f * self.malnourished;
                float d2 = 6f;
                for (int i = 0; i < self.tail.Length; i++)
                {
                    Vector2 vector5 = Vector2.Lerp(self.tail[i].lastPos, self.tail[i].pos, timeStacker);

                    Vector2 normalized = (vector5 - vector4).normalized;
                    Vector2 a = Custom.PerpendicularVector(normalized);
                    float d3 = Vector2.Distance(vector5, vector4) / 5f;
                    if (i == 0)
                        d3 = 0f;

                    tail.MoveVertice(i * 4, vector4 - a * d2 * d + normalized * d3 - camPos);
                    tail.MoveVertice(i * 4 + 1, vector4 + a * d2 * d + normalized * d3 - camPos);
                    if (i < self.tail.Length - 1)
                    {


                        tail.MoveVertice(i * 4 + 2,
                            vector5 - a * (self.tail[i].stretched * self.tail[i].rad) * d - normalized * d3 - camPos);
                        tail.MoveVertice(i * 4 + 3,
                            vector5 + a * (self.tail[i].stretched * self.tail[i].rad) * d - normalized * d3 - camPos);
                    }
                    else
                    {
                        tail.MoveVertice(i * 4 + 2, vector5 - camPos);
                    }

                    d2 = (self.tail[i].stretched * self.tail[i].rad);
                    vector4 = vector5;

                }
            }
        }


        private void UpdateSprite(string name, FSprite sprite, int i, float moveDeg, PlayerGraphics self)
        {
            var complexName = name;

            switch (i)
            {
                case 5 or 7:
                    complexName = "Left" + name;
                    break;
                case 6 or 8:
                    complexName = "Right" + name;
                    break;
                case 9 or 3 or 4:
                    complexName = (sprite.scaleX < 0 ? "Left" : "Right") + name;
                    break;
                case 11:
                    complexName = name;
                    break;
                default:
                    {
                        if (self.player.bodyMode == Player.BodyModeIndex.Stand)
                        {
                            if (self.player.bodyChunks[1].vel.x < -3)
                                complexName = "Left" + name;
                            else if (self.player.bodyChunks[1].vel.x > 3)
                                complexName = "Right" + name;
                        }
                        else if (self.player.bodyMode == Player.BodyModeIndex.Crawl ||
                                 self.player.bodyMode == Player.BodyModeIndex.CorridorClimb ||
                                 self.player.bodyMode == Player.BodyModeIndex.ClimbIntoShortCut)
                        {
                            if (moveDeg >= 30f) complexName = "Right" + name;
                            else if (moveDeg <= -30f) complexName = "Left" + name;
                        }

                        break;
                    }
            }

            if (Futile.atlasManager.DoesContainElementWithName(self.player.slugcatStats.name + complexName))
            {
                sprite.element = Futile.atlasManager.GetElementWithName(self.player.slugcatStats.name + complexName);
                if (name == "pixel")
                    sprite.scale = 1;
            }

            else if (Futile.atlasManager.DoesContainElementWithName(self.player.slugcatStats.name + name))
            {
                sprite.element = Futile.atlasManager.GetElementWithName(self.player.slugcatStats.name + name);
                if (name == "pixel")
                    sprite.scale = 1;
            }


        }


        private int startIndex = Int32.MaxValue;
        private WeakReference<PlayerGraphics> playerRef;
    }


    internal static partial class PlayerGraphicsHooks
    {
        private static void LoadAssets()
        {
            On.FAtlasManager.AddAtlas += FAtlasManager_AddAtlas;
            foreach (var modDir in ModManager.ActiveMods.Select(i => i.basePath))
            {
                if(!Directory.Exists($"{modDir}/atlas"))
                    continue;
                foreach (var dir in new DirectoryInfo($"{modDir}/atlas").GetDirectories("skin_*"))
                {
                    currentName = dir.Name.Replace("skin_", "");
                    foreach (var file in dir.GetFiles("*.txt"))
                    {
                        try
                        {
                            if (File.Exists(file.FullName.Replace("txt", "png")) ||
                                File.Exists(file.FullName.Replace("txt", "PNG")))
                            {
                                Futile.atlasManager.LoadAtlas(Path.Combine("atlas", dir.Name,
                                    file.Name.Replace(".txt", "")));
                                Plugin.Log("Custom Skin", $"Load {file} for ID:{currentName}");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            Plugin.LogError("Custom Skin", $"Try load atlas at:{file.FullName} failed!\n{e}");
                        }
                    }

                    customSkins.Add(new SlugcatStats.Name(currentName));
                }
            }

            On.FAtlasManager.AddAtlas -= FAtlasManager_AddAtlas;

        }

        private static string currentName;

        private static void FAtlasManager_AddAtlas(On.FAtlasManager.orig_AddAtlas orig, FAtlasManager self, FAtlas atlas)
        {
            foreach (var obj in atlas.elements)
                obj.name = $"{currentName}{obj.name}";
            orig(self, atlas);
        }

        private static HashSet<SlugcatStats.Name> customSkins = new();  
    }
}
