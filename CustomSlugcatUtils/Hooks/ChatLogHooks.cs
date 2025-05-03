using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreSlugcats;
using RWCustom;

namespace CustomSlugcatUtils.Hooks
{

    static class ChatLogHooks
    {
 

        
        private static void LoadChatLogID()
        {
            PathMaps.Clear();
            foreach (var path in ModManager.ActiveMods.Select(i => i.basePath))
            {
                if (!Directory.Exists($"{path}/text/chatlogs"))
                    continue;

                foreach (var chatData in new DirectoryInfo($"{path}/text/chatlogs").GetDirectories())
                {

                    if (PathMaps.Any(i => i.Key.value == chatData.Name))
                    {
                        Plugin.LogError("Custom Chatlog",$"Already contains ChatLogID : {chatData.Name}");
                        continue;
                    }
                    Plugin.Log($"Add custom chatlog: ID:{chatData.Name}, Path:{chatData.FullName}");
                    PathMaps.Add(new ChatlogData.ChatlogID(chatData.Name, !ChatlogData.ChatlogID.values.entries.Contains(chatData.Name)), chatData.FullName);

                }

            }

        }

        public static readonly Dictionary<ChatlogData.ChatlogID, string> PathMaps = new ();

        public static void OnModsInit()
        {
            On.MoreSlugcats.ChatlogData.HasUnique += ChatlogData_HasUnique;
            On.MoreSlugcats.ChatlogData.getChatlog_ChatlogID += ChatlogData_getChatlog_ChatlogID;

            On.MoreSlugcats.ChatLogDisplay.NewMessage_string_float_float_int += ChatLogDisplay_NewMessage_string_float_float_int;
            On.MoreSlugcats.ChatLogDisplay.ctor += ChatLogDisplay_ctor;
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            LoadChatLogID();
            Plugin.Log("ChatLog Hooks Loaded");

        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            if(self.IsStorySession)
                LoadChatLogID();
        }

        private static void ChatLogDisplay_ctor(On.MoreSlugcats.ChatLogDisplay.orig_ctor orig, ChatLogDisplay self, HUD.HUD hud, string[] chatLog)
        {
            orig(self, hud, chatLog);
            for (int i = 0; i < chatLog.Length; i++)
            {
                if (chatLog[i].Length > 8 && chatLog[i].StartsWith("<#") && chatLog[i][8] == '>')
                {
                    try
                    {
                        self.label[i].color = Custom.hexToColor(chatLog[i].Substring(2, 6));
                    }
                    catch
                    {
                        Plugin.LogError("Custom Chatlog Format Error",$"Unexpected Color format {chatLog[i].Substring(2, 6)}");
                    }
                }

            }
        }

        private static void ChatLogDisplay_NewMessage_string_float_float_int(On.MoreSlugcats.ChatLogDisplay.orig_NewMessage_string_float_float_int orig, MoreSlugcats.ChatLogDisplay self, string text, float xOrientation, float yPos, int extraLinger)
        {
            if (text.StartsWith("<#") && text[8] == '>')
                text = text.Substring(9);

            orig(self, text, xOrientation, yPos, extraLinger);
        }


        private static bool ChatlogData_HasUnique(On.MoreSlugcats.ChatlogData.orig_HasUnique orig, MoreSlugcats.ChatlogData.ChatlogID id)
        {
            return id != null && PathMaps.ContainsKey(id) || orig(id);
        }

        private static string[] ChatlogData_getChatlog_ChatlogID(On.MoreSlugcats.ChatlogData.orig_getChatlog_ChatlogID orig, ChatlogData.ChatlogID id)
        {
            if (id != null && PathMaps.TryGetValue(id,out var dirPath))
            {
                string path = $"{dirPath}/{LocalizationTranslator.LangShort(Custom.rainWorld.inGameTranslator.currentLanguage)}.txt";
                
                if (!File.Exists(AssetManager.ResolveFilePath(path)))
                    path = $"{dirPath}/{LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English)}.txt";
                
                if (!File.Exists(AssetManager.ResolveFilePath(path)))
                {
                    foreach (var lan in InGameTranslator.LanguageID.values.entries.Where(i =>
                                     i != Custom.rainWorld.inGameTranslator.currentLanguage.value &&
                                     i != InGameTranslator.LanguageID.English.value)
                                 .Select(i => new InGameTranslator.LanguageID(i)))
                    {
                        path = $"{dirPath}/{LocalizationTranslator.LangShort(lan)}.txt";
                        if (File.Exists(AssetManager.ResolveFilePath(path)))
                            break;
                    }
                }
                
                if (!File.Exists(AssetManager.ResolveFilePath(path)))
                {
                    var info = new DirectoryInfo(dirPath);
                    var files = info.GetFiles("*.txt");
                    if (files.Length > 0)
                    {
                       path = files[0].FullName;
                       Plugin.LogWarning("Custom Chatlog",$"File incorrect file name:{files[0].Name}, At path:{path}");
                    }
                }
                if (!File.Exists(AssetManager.ResolveFilePath(path)))
                {
                    Plugin.LogError("Custom Chatlog No file Error", $"Can't find chatlog file At {dirPath}");
                    return new[] { $"Can't find file at {dirPath} folder" };
                }

                var lines = File.ReadAllLines(AssetManager.ResolveFilePath(path));
                return lines;

            }
            return orig(id);
        }
    }


}
