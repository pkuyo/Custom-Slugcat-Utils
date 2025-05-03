using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CustomSlugcatUtils.Tools;
using HUD;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using Newtonsoft.Json;
using RWCustom;
using SlugBase.SaveData;
using UnityEngine;
using static Conversation;
using static CustomSlugcatUtils.Hooks.OracleHooks;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
namespace CustomSlugcatUtils.Hooks
{

    public class CustomSpecialEvent : Conversation.SpecialEvent
    {
        public CustomSpecialEvent(Conversation owner, int initialWait, string eventName, string[] arg) : base(owner, initialWait, eventName)
        {
            this.args = arg;
        }

        public override void Activate()
        {
            base.Activate();
            CustomOracle.OnEventTriggerInternal(owner.interfaceOwner, this);
        }

        public string[] args;
    }

    public static class CustomOracle
    {
        public delegate void EventTriggerHandler(IOwnAConversation owner, CustomSpecialEvent eventData);

        public static event EventTriggerHandler OnEventTrigger;

        public static void RegisterCondition(string name, Func<RainWorldGame, string[], bool> func)
        {
            OracleHooks.RegisterCondition(name, func);
        }

        internal static void OnEventTriggerInternal(IOwnAConversation owner, CustomSpecialEvent eventData)
            => OnEventTrigger?.Invoke(owner, eventData);
    }

    internal class CustomOracleBehavior : SSOracleBehavior.ConversationBehavior
    {
        public static readonly SubBehavID CustomBehavior = new($"{Plugin.ModId}.{nameof(CustomBehavior)}", true);
        public static readonly Conversation.ID PlaceHolder = new($"{Plugin.ModId}.{nameof(PlaceHolder)}", true);

        public static readonly SSOracleBehavior.Action CustomAction = new($"{Plugin.ModId}.{nameof(CustomAction)}", true);

        public CustomOracleBehavior(SSOracleBehavior owner) : base(owner, CustomBehavior, PlaceHolder)
        {
        }


        private bool requestNewConv;
        private int waitCounter;

        public override void Update()
        {
            base.Update();
            if (owner.oracle.room.game.cameras.All(i => i.room != owner.oracle.room))
            {
                return;
            }
            if (requestNewConv)
            {
                if (waitCounter <= 0)
                {
                    if (TryGetModule(owner, out var module))
                    {
                        owner.InitateConversation(PlaceHolder, this);
                        owner.conversation.LoadTextFromCustomFile(module.oracleData.folderPath,
                        module.CurrentEvent.eventName, module.CurrentEvent.random ? '^' : null);
                        requestNewConv = false;
                    }
                    else
                        Plugin.LogError("Custom Oracle", $"Can't find module for Oracle:{owner.oracle.ID}");

                }
                else
                    waitCounter--;
                return;
            }


            if (owner.conversation?.slatedForDeletion ?? true)
            {
                Plugin.Log("Custom Oracle", "End Conv");
                owner.conversation = null;

                if (OracleHooks.TryGetModule(owner, out var module))
                {
                    if (module.CurrentEvent.loop)
                    {
                        requestNewConv = true;
                        waitCounter = Mathf.RoundToInt(Random.Range(module.CurrentEvent.minWait, module.CurrentEvent.maxWait) * 40);
                    }
                    else if(!module.CurrentEvent.readPearl)
                    {
                        module.currentEventIndex++;
                        if (module.currentEventIndex == module.behaviorData.events.Length)
                        {
                            Plugin.LogError("Custom Oracle", "Out of event Range, Use ThrowOut Action!");
                            owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                        }
                        else
                        {
                            owner.NewAction(GetAction(module.CurrentEvent.eventName));
                            Plugin.Log("Custom Oracle", $"Enter next event:{module.CurrentEvent.eventName}, index:{module.currentEventIndex}");

                        }
                    }
                }
                else
                    Plugin.LogError("Custom Oracle", $"Can't find module for Oracle:{owner.oracle.ID}");


            }
        }

        public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
        {
            base.NewAction(oldAction, newAction);
            if (newAction == CustomAction && TryGetModule(owner, out var module) && oldAction != SSOracleBehavior.Action.General_GiveMark)
            {

                owner.InitateConversation(PlaceHolder, this);
                owner.conversation.LoadTextFromCustomFile(module.oracleData.folderPath,
                    module.CurrentEvent.eventName, module.CurrentEvent.random ? '^' : null);

            }
            
        }

        public override void Deactivate()
        {
            base.Deactivate();
            Plugin.Log("Custom Oracle", "Custom Behavior Deactivate");
        }

    }

    //基础功能
    internal static partial class OracleHooks
    {
     
        private static readonly Dictionary<string, Func<RainWorldGame, string[], bool>> RegistCondition = new();


        public static void LoadTextFromCustomFile(this Conversation conversation, string folderName, string fileName, char? randomStartPos = null, int index = -1)
        {
            index = index >= 0 ? index + 1 : index;
            Plugin.Log("Custom Oracle", $"Load custom text: {fileName}, randomStart: {randomStartPos != null && index == -1}{(index != -1 ? $", index: {index}" : "")}");
            if (fileName == null)
                return;
            var path = $"text/oracle/{folderName}/text_{LocalizationTranslator.LangShort(Custom.rainWorld.inGameTranslator.currentLanguage)}/{fileName}.txt";
            if (!File.Exists(AssetManager.ResolveFilePath(path)))
                path = $"text/oracle/{folderName}/text_{LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English)}/{fileName}.txt";
            
            if (!File.Exists(AssetManager.ResolveFilePath(path)))
            {
                foreach (var id in InGameTranslator.LanguageID.values.entries.Where(i =>
                                 i != Custom.rainWorld.inGameTranslator.currentLanguage.value &&
                                 i != InGameTranslator.LanguageID.English.value)
                             .Select(i => new InGameTranslator.LanguageID(i)))
                {
                    path = $"text/oracle/{folderName}/text_{LocalizationTranslator.LangShort(id)}/{fileName}.txt";
                    if (File.Exists(AssetManager.ResolveFilePath(path)))
                        break;
                }
            }
            
            if (!File.Exists(AssetManager.ResolveFilePath(path)))
            {
                conversation.events.Add(new TextEvent(conversation, 0, $"Can't find conversation file At {path}", 200));
                Plugin.LogError("Custom Oracle No file Error", $"Can't find conversation file At {path}");
                return;
            }

            var lines = File.ReadAllLines(AssetManager.ResolveFilePath(path))
                .Select(i => i.Substring(0, i.IndexOf("//") == -1 ? i.Length : i.IndexOf("//"))).
                Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();

            var igt = Custom.rainWorld.inGameTranslator;

            if (randomStartPos == null)
            {
                bool currentCondition = true;
                bool lastCondition = true;
                bool inCondition = false;
                for (int i = 0; i<lines.Length ;i++)
                {
                    var line = lines[i].Trim();
                    Plugin.LogDebug($"{folderName}-{fileName}: {line}");
                  
                    try
                    {
                        var split = line.Split('|');
                        if (split[0] == "IF")
                        {
                            if (inCondition)
                                Plugin.LogError("Custom Oracle", "!!!!USE CONDITION WITHIN CONDITION!!!");

                            LoadCondition(split, i, fileName, out currentCondition);
                            lastCondition = currentCondition;
                            inCondition = true;

                        }
                        else if (split[0] == "ELSE")
                        {
                            if (!inCondition)
                                Plugin.LogError("Custom Oracle", "!!!!USE ELSE WITHOUT CONDITION!!!");
                            if (lastCondition)
                                currentCondition = false;
                            else if (split.Length > 1)
                                LoadCondition(split, i, fileName, out currentCondition);
                            else
                                currentCondition = true;
                            lastCondition |= currentCondition;
                        }
                        else if (split[0] == "END")
                        {
                            lastCondition = false;
                            currentCondition = true;
                            inCondition = false;
                        }
                        else if (currentCondition)
                        {
                            LoadSingleLine(conversation, split, igt);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        ErrorTracker.TrackError("Custom Oracle Format Error", $"At File:{fileName}, line:{i}\n{line}");
                    }

                }
            }
            else
            {
                int count = lines.Count(i => i[0] == randomStartPos.Value);
                int randomConv = Random.Range(0, count) + 1;
                if (index > 0)
                    randomConv = index;
                int currentConv = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line[0] == randomStartPos.Value)
                    {
                        currentConv++;
                    }
                    if (currentConv == randomConv)
                    {
                        Plugin.LogDebug($"{folderName}-{fileName}: {line}");
                        var split = line.Split('|');
                        if (split.Length != 2 && split[0][0] == randomStartPos.Value)
                            split[0] = split[0].Substring(1);
                        try
                        {
                            LoadSingleLine(conversation, split, igt);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            ErrorTracker.TrackError("Custom Oracle Format Error", $"Format Error At File:{fileName}, line:{i}\n{line}");
                        }
                    }
                }
            }

        }


        public static void LoadTextFromCustomFile(this DialogBox box, string folderName, string fileName, char? randomStartPos = null, int index = -1)
        {
            index = index >= 0 ? index + 1 : index;
            Plugin.Log("Custom Oracle", $"Load dialog text: {fileName}, randomStart: {randomStartPos != null && index == -1}{(index != -1 ? $", index: {index}" : "")}");
            if (fileName == null)
                return;
            var path = $"text/oracle/{folderName}/text_{LocalizationTranslator.LangShort(box.hud.rainWorld.inGameTranslator.currentLanguage)}/{fileName}.txt";
            
            if (!File.Exists(AssetManager.ResolveFilePath(path)))
                path = $"text/oracle/{folderName}/text_{LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English)}/{fileName}.txt";
            
            if (!File.Exists(AssetManager.ResolveFilePath(path)))
            {
                foreach (var id in InGameTranslator.LanguageID.values.entries.Where(i =>
                                 i != box.hud.rainWorld.inGameTranslator.currentLanguage.value &&
                                 i != InGameTranslator.LanguageID.English.value)
                             .Select(i => new InGameTranslator.LanguageID(i)))
                {
                    path = $"text/oracle/{folderName}/text_{LocalizationTranslator.LangShort(id)}/{fileName}.txt";
                    if (File.Exists(AssetManager.ResolveFilePath(path)))
                        break;
                }
            }

            if (!File.Exists(AssetManager.ResolveFilePath(path)))
            {
                box.NewMessage($"Can't find dialog file At {path}", 200);;
                return;
            }

            var lines = File.ReadAllLines(AssetManager.ResolveFilePath(path))
                .Select(i => i.Substring(0, i.IndexOf("//") == -1 ? i.Length : i.IndexOf("//"))).
                Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();

            var igt = box.hud.rainWorld.inGameTranslator;

            bool isFirst = true;

            if (randomStartPos == null)
            {
                bool currentCondition = true;
                bool lastCondition = true;
                bool inCondition = false;
                for (int i = 0;i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    var split = line.Split('|');
                    Plugin.LogDebug($"{folderName}-{fileName}: {line}");

                    if (split[0] == "IF")
                    {
                        if (inCondition)
                            Plugin.LogError("Custom Oracle", "!!!!USE CONDITION WITHIN CONDITION!!!");

                        LoadCondition(split, i, fileName, out currentCondition);
                        lastCondition = currentCondition;
                        inCondition = true;

                    }
                    else if (split[0] == "ELSE")
                    {
                        if (!inCondition)
                            Plugin.LogError("Custom Oracle", "!!!!USE ELSE WITHOUT CONDITION!!!");
                        if (lastCondition)
                            currentCondition = false;
                        else if (split.Length > 1)
                            LoadCondition(split, i, fileName, out currentCondition);
                        else
                            currentCondition = true;
                        lastCondition |= currentCondition;
                    }
                    else if (split[0] == "END")
                    {
                        lastCondition = false;
                        currentCondition = true;
                        inCondition = false;
                    }
                    else if (currentCondition)
                    {
                        try
                        {
                            var extraLinger = split.Length == 1 ? 0 : int.Parse(split[2]);
                            if (isFirst)
                            {
                                box.Interrupt(igt.Translate(split[0]), extraLinger);
                                isFirst = false;
                            }
                            else
                                box.NewMessage(igt.Translate(split[0]), extraLinger);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            ErrorTracker.TrackError("Custom Oracle Format Error", $"At File:{fileName}, line:{i}\n{line}");
                        }
                    }
                }
            }
            else
            {
                int count = lines.Count(i => i[0] == randomStartPos.Value);
                int randomConv = Random.Range(0, count) + 1;
                if (index > 0)
                    randomConv = index;
                int currentConv = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line[0] == randomStartPos.Value)
                    {
                        currentConv++;
                    }
                    if (currentConv == randomConv)
                    {
                        try
                        {
                            Plugin.LogDebug($"{folderName}-{fileName}: {line}");
                            var split = line.Split('|');
                            if (split.Length != 2 && split[0][0] == randomStartPos.Value)
                                split[0] = split[0].Substring(1);
                            var extraLinger = split.Length == 1 ? 0 : int.Parse(split[2]);
                            if (isFirst)
                            {
                                box.Interrupt(igt.Translate(split[0]), extraLinger);
                                isFirst = false;
                            }
                            else
                                box.NewMessage(igt.Translate(split[0]), extraLinger);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            ErrorTracker.TrackError("Custom Oracle Format Error", $"At File:{fileName}, line:{i}\n{line}");
                        }
                    }
                }
            }

        }


        private static bool GetCondition(string con)
        {
            var conArgs = con.Split(' ');
            if (!RegistCondition.ContainsKey(conArgs[0]))
            {
                Plugin.LogError("Custom Oracle Condition Error", $"Unknown condition name : {conArgs[0]}");
                return false;
            }
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                try
                {
                    return RegistCondition[conArgs[0]].Invoke(game, conArgs.Length > 1 ? conArgs.Skip(1).ToArray() : Array.Empty<string>());
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ErrorTracker.TrackError("Custom Oracle Condition Error", $"Name:{conArgs[0]}\n{e}");
                }
            return false;

        }

        public static void RegisterCondition(string name, Func<RainWorldGame, string[], bool> func)
        {
            if (RegistCondition.ContainsKey(name))
            {
                Plugin.LogError("Custom Oracle", $"Already has this condition: {name}");
                return;
            }
            RegistCondition.Add(name, func);
        }


        private static void LoadSingleLine(Conversation conversation, string[] split, InGameTranslator igt)
        {

            if (split[0] == "SP")
                conversation.events.Add(new CustomSpecialEvent(conversation, 0, split[1], split.Length > 2 ? split.Skip(2).ToArray() : Array.Empty<string>()));
            else if (split.Length == 2 && split[0] == "WAIT")
                conversation.events.Add(new WaitEvent(conversation, int.Parse(split[1])));
            else if (split.Length == 3)
                conversation.events.Add(new TextEvent(conversation, int.Parse(split[1]),
                    igt.Translate(split[0]), int.Parse(split[2])));
            else
                conversation.events.Add(new TextEvent(conversation, 0,
                    igt.Translate(split[0]), 0));
        }
        private static void LoadCondition(string[] split,int line, string file, out bool currentCondition)
        {
            try
            {
                if (split.Length > 2)
                {
                    Func<bool, bool, bool> combFunc;
                    if (split[1] == "&")
                    {
                        combFunc = (a, b) => a && b;
                        currentCondition = true;
                    }
                    else
                    {
                        combFunc = (a, b) => a || b;
                        currentCondition = false;
                    }

                    for (int i = 2; i < split.Length; i++)
                        currentCondition = combFunc(currentCondition, GetCondition(split[i]));
                }
                else
                {
                    currentCondition = GetCondition(split[1]);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ErrorTracker.TrackError("Custom Oracle", $"Condition Format Error At FileName:{file}, line:{line}");
                currentCondition = false;
            }

        }
    }


    //SS,DM Hook
    internal static partial class OracleHooks
    {
        public static void OnModsInit()
        {
            On.OracleBehavior.ctor += OracleBehavior_ctor;
            On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
            On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
            IL.SSOracleBehavior.Update += SSOracleBehavior_Update;
            On.SSOracleBehavior.InterruptPearlMessagePlayerLeaving += SSOracleBehavior_InterruptPearlMessagePlayerLeaving;
            On.SSOracleBehavior.ResumePausedPearlConversation += SSOracleBehavior_ResumePausedPearlConversation;

            CustomOracle.OnEventTrigger += CustomOracle_OnEventTrigger;
            OnModsInitRot();
            OnModsInitSL();
            
            ReadOracleData();
        }

        private static void SSOracleBehavior_ResumePausedPearlConversation(On.SSOracleBehavior.orig_ResumePausedPearlConversation orig, SSOracleBehavior self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("PlayerResume", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);
                return;
            }
            orig(self);
        }

        private static void SSOracleBehavior_InterruptPearlMessagePlayerLeaving(On.SSOracleBehavior.orig_InterruptPearlMessagePlayerLeaving orig, SSOracleBehavior self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("PlayerLeft", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);
                return;
            }
            orig(self);
        }

        private static void SSOracleBehavior_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.Before,
                i => i.MatchBrfalse(out _),
                i => i.MatchLdarg(0),
                i => i.MatchLdsfld<MoreSlugcatsEnums.SSOracleBehaviorAction>("MeetWhite_ThirdCurious"),
                i => i.MatchStfld<SSOracleBehavior>("afterGiveMarkAction"));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, SSOracleBehavior, bool>>((re, self) => re && !TryGetModule(self, out _));
        }

        private static void CustomOracle_OnEventTrigger(IOwnAConversation owner, CustomSpecialEvent eventData)
        {
            try
            {
                if (owner is SSOracleBehavior ss)
                {
                    switch (eventData.eventName)
                    {
                        case "gravity":
                            ss.oracle.gravity = int.Parse(eventData.args[0]);
                            break;
                        case "locked":
                            ss.LockShortcuts();
                            break;
                        case "unlocked":
                            ss.UnlockShortcuts();
                            break;
                        case "work":
                            ss.getToWorking = int.Parse(eventData.args[0]);
                            break;
                        case "behavior":
                            if (ExtEnumBase.TryParse(typeof(SSOracleBehavior.MovementBehavior), eventData.args[0], true, out var re))
                                ss.movementBehavior = (SSOracleBehavior.MovementBehavior)re;
                            else
                                Plugin.LogError("Custom Oracle", $"Unknown movement behavior:{eventData.args[0]}");
                            break;
                        case "sound":
                            if (ExtEnumBase.TryParse(typeof(SoundID), eventData.args[0], true, out var soundId))
                            {
                                if (eventData.args.Length == 3)
                                    ss.oracle.room.PlaySound((SoundID)soundId, ss.oracle.firstChunk, false,
                                        float.Parse(eventData.args[1]), float.Parse(eventData.args[2]));
                                else
                                    ss.oracle.room.PlaySound((SoundID)soundId, ss.oracle.firstChunk);

                            }
                            else
                                Plugin.LogError("Custom Oracle", $"Unknown sound Id:{eventData.args[0]}");
                            break;
                        case "turnOff":
                            ss.TurnOffSSMusic(eventData.args.Length == 0 || bool.Parse(eventData.args[0]));
                            break;
                        case "move":
                            ss.SetNewDestination(new Vector2(float.Parse(eventData.args[0]), float.Parse(eventData.args[1])));
                            break;
                        default:
                            owner.SpecialEvent(eventData.eventName);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ErrorTracker.TrackError(e, "Custom Oracle: Special Event Error", $"At: {eventData.eventName}\n{e}");
            }
           

        }

        private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self,
            SSOracleBehavior.Action nextAction)
        {
            //StackTrace t = new StackTrace(true);
            Plugin.Log("Custom Oracle", $"Action:{nextAction}, {self.action}");
            if (Modules.TryGetValue(self, out _) && nextAction == CustomOracleBehavior.CustomAction)
            {

                Plugin.Log("Custom Oracle", $"Use Behavior: {CustomOracleBehavior.CustomBehavior}, action:{nextAction}");
                self.inActionCounter = 0;
                if (self.currSubBehavior.ID == CustomOracleBehavior.CustomBehavior)
                {
                    self.currSubBehavior.Activate(self.action, nextAction);
                    self.action = nextAction;
                    return;
                }

                SSOracleBehavior.SubBehavior subBehavior = null;
                foreach (var behavior in self.allSubBehaviors)
                {
                    if (behavior.ID == CustomOracleBehavior.CustomBehavior)
                    {
                        subBehavior = behavior;
                        break;
                    }
                }

                if (subBehavior == null)
                {
                    subBehavior = new CustomOracleBehavior(self);
                    self.allSubBehaviors.Add(subBehavior);
                }

                subBehavior.Activate(self.action, nextAction);
                self.action = nextAction;

                self.currSubBehavior.Deactivate();
                self.currSubBehavior = subBehavior;

            }
            else
            {
                orig(self, nextAction);
            }
        }
        private static void OracleBehavior_ctor(On.OracleBehavior.orig_ctor orig, OracleBehavior self, Oracle oracle)
        {
            orig(self, oracle);
            if(self.oracle.room.game.session is StoryGameSession session &&
                Datas.FirstOrDefault(i => i.oracleId == self.oracle.ID && i.slugcatId == session.saveStateNumber) is
                    { } oracleData && self is not SLOracleBehaviorNoMark)
            {
                Plugin.Log("Custom Oracle", $"Add Oracle Module for:{oracleData.slugcatId}-{oracleData.oracleId}");
                Modules.Add(self,new OracleBehaviorModule(self, oracleData));
            }
        }

        private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {
            bool seePeople = false;
            foreach (var player in self.oracle.room.game.Players)
                if (player.realizedCreature is Player)
                    seePeople = true;

            if (seePeople && Modules.TryGetValue(self, out var module) && module.SeePlayer())
                return;

            orig(self);
        }

        public static bool TryGetModule(OracleBehavior self, out OracleBehaviorModule module)
        {
            return Modules.TryGetValue(self, out module);
        }

        public static SSOracleBehavior.Action GetAction(string rawId)
        {
            if (SSOracleBehavior.Action.values.entries.Contains(rawId))
                return new SSOracleBehavior.Action(rawId);
            return CustomOracleBehavior.CustomAction;
        }



        private static readonly ConditionalWeakTable<OracleBehavior, OracleBehaviorModule> Modules = new();

    }

    internal static partial class OracleHooks
    {
        public static void OnModsInitRot()
        {
            On.MoreSlugcats.SSOracleRotBehavior.TalkToNoticedPlayer += SSOracleRotBehavior_TalkToNoticedPlayer;
            On.MoreSlugcats.SSOracleRotBehavior.TalkToDeadPlayer += SSOracleRotBehavior_TalkToDeadPlayer;
            On.MoreSlugcats.SSOracleRotBehavior.StoleHalcyon += SSOracleRotBehavior_StoleHalcyon;

            On.MoreSlugcats.CLOracleBehavior.InitateConversation += CLOracleBehavior_InitateConversation;
            On.MoreSlugcats.CLOracleBehavior.TalkToDeadPlayer += CLOracleBehavior_TalkToDeadPlayer;
            On.MoreSlugcats.CLOracleBehavior.InterruptRain += CLOracleBehavior_InterruptRain;
        }

      
        private static void CLOracleBehavior_InterruptRain(On.MoreSlugcats.CLOracleBehavior.orig_InterruptRain orig, CLOracleBehavior self)
        {
            if (!self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.halcyonStolen &&
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts <= 0 &&
                TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("Rain", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);
                return;
            }
            orig(self);
                
            
        }

        private static void CLOracleBehavior_TalkToDeadPlayer(On.MoreSlugcats.CLOracleBehavior.orig_TalkToDeadPlayer orig, CLOracleBehavior self)
        {
            //loop, wait无效
            if (!self.deadTalk && TryGetModule(self, out var module) && module.oracleData.otherConversations.TryGetValue("DeadPlayer", out var ev))
            {
                if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.halcyonStolen ||
                    self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts > 0)
                {
                    self.dialogBox.Interrupt(self.Translate("..."), 60);
                    return;
                }
                self.deadTalk = true;
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);
                return;
            }
            orig(self);
        }

        private static void CLOracleBehavior_InitateConversation(On.MoreSlugcats.CLOracleBehavior.orig_InitateConversation orig, CLOracleBehavior self)
        {
            //loop, wait无效
            if (TryGetModule(self, out var module))
            { 
                if (module.SeePlayer())
                    return;
            }


            orig(self);
        }

        private static void SSOracleRotBehavior_StoleHalcyon(On.MoreSlugcats.SSOracleRotBehavior.orig_StoleHalcyon orig, SSOracleRotBehavior self)
        {
            //loop, wait无效
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("HalcyonStolen", out var stolen))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, stolen.eventName,
                    stolen.random ? '^' : null);
                if (self.conversation != null)
                {
                    self.conversation.paused = true;
                    self.restartConversationAfterCurrentDialoge = true;
                }
                return;
            }

            orig(self);
        }

        private static void SSOracleRotBehavior_TalkToDeadPlayer(On.MoreSlugcats.SSOracleRotBehavior.orig_TalkToDeadPlayer orig, SSOracleRotBehavior self)
        {
            //Loop无效
            if (!self.deadTalk && TryGetModule(self, out var module) && module.oracleData.otherConversations.TryGetValue("DeadPlayer", out var ev))
            {
                self.deadTalk = true;
                self.InitateConversation(CustomOracleBehavior.PlaceHolder);
                self.conversation.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);
                return;
            }
            orig(self);
        }

        private static void SSOracleRotBehavior_TalkToNoticedPlayer(On.MoreSlugcats.SSOracleRotBehavior.orig_TalkToNoticedPlayer orig, SSOracleRotBehavior self)
        {
            //Loop无效
            if (TryGetModule(self, out var module) && module.SeePlayer())
                return;
            orig(self);
        }
    }

    internal static partial class OracleHooks
    {
        public static void OnModsInitSL()
        {
            On.SLOracleBehaviorHasMark.InterruptPlayerAnnoyingMessage += SLOracleBehaviorHasMark_InterruptPlayerAnnoyingMessage;
            On.SLOracleBehaviorHasMark.InterruptPlayerHoldNeuron += SLOracleBehaviorHasMark_InterruptPlayerHoldNeuron;
            On.SLOracleBehaviorHasMark.InterruptPlayerLeavingMessage += SLOracleBehaviorHasMark_InterruptPlayerLeavingMessage;
            On.SLOracleBehaviorHasMark.InterruptRain += SLOracleBehaviorHasMark_InterruptRain;
            On.SLOracleBehaviorHasMark.TalkToDeadPlayer += SLOracleBehaviorHasMark_TalkToDeadPlayer;
            On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
            On.SLOracleBehaviorHasMark.PlayerReleaseNeuron += SLOracleBehaviorHasMark_PlayerReleaseNeuron;
            On.SLOracleBehaviorHasMark.PlayerHoldingSSNeuronsGreeting += SLOracleBehaviorHasMark_PlayerHoldingSSNeuronsGreeting;
        }

        private static void SLOracleBehaviorHasMark_PlayerHoldingSSNeuronsGreeting(On.SLOracleBehaviorHasMark.orig_PlayerHoldingSSNeuronsGreeting orig, SLOracleBehaviorHasMark self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("HoldingSSNeuronsGreeting", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName,
                    ev.random ? '^' : null);
                return;
            }
            orig(self);
        }

        private static void SLOracleBehaviorHasMark_PlayerReleaseNeuron(On.SLOracleBehaviorHasMark.orig_PlayerReleaseNeuron orig, SLOracleBehaviorHasMark self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("ReleaseNeuron", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName,
                    ev.random ? '^' : null);
                return;
            }

            orig(self);
        }

        private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
        {
            if (TryGetModule(self, out var module) && module.SeePlayer())
            {
                self.State.playerEncounters++;
                self.State.playerEncountersWithMark++;
                return;
            }

            orig(self);
        }

        private static void SLOracleBehaviorHasMark_TalkToDeadPlayer(On.SLOracleBehaviorHasMark.orig_TalkToDeadPlayer orig, SLOracleBehaviorHasMark self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("DeadPlayer", out var ev) && !self.deadTalk)
            {
                self.deadTalk = true;
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);
                return;
            }
            orig(self);
        }

        private static void SLOracleBehaviorHasMark_InterruptRain(On.SLOracleBehaviorHasMark.orig_InterruptRain orig, SLOracleBehaviorHasMark self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("Rain", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);
                return;
            }
            orig(self);
        }

        private static void SLOracleBehaviorHasMark_InterruptPlayerLeavingMessage(On.SLOracleBehaviorHasMark.orig_InterruptPlayerLeavingMessage orig, SLOracleBehaviorHasMark self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("PlayerLeft", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);

                if (!ModManager.MMF)
                    self.State.InfluenceLike(-0.05f);
                
                self.State.leaves += 1;
                self.State.totalInterruptions += 1;
                self.State.increaseLikeOnSave = false;
                return;
            }
            orig(self);
        }

        private static void SLOracleBehaviorHasMark_InterruptPlayerHoldNeuron(On.SLOracleBehaviorHasMark.orig_InterruptPlayerHoldNeuron orig, SLOracleBehaviorHasMark self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("HoldNeuron", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath, ev.eventName, ev.random ? '^' : null);
                self.State.InfluenceLike((self.State.totalInterruptions > 5 || self.State.hasToldPlayerNotToEatNeurons)
                    ? 0.2f
                    : 0.1f);
                self.State.hasToldPlayerNotToEatNeurons = true;
                self.State.annoyances += 1;
                self.State.totalInterruptions += 1;
                self.State.increaseLikeOnSave = false;
                return;
            }
            orig(self);
        }

        private static void SLOracleBehaviorHasMark_InterruptPlayerAnnoyingMessage(On.SLOracleBehaviorHasMark.orig_InterruptPlayerAnnoyingMessage orig, SLOracleBehaviorHasMark self)
        {
            if (TryGetModule(self, out var module) &&
                module.oracleData.otherConversations.TryGetValue("Annoying", out var ev))
            {
                self.dialogBox.LoadTextFromCustomFile(module.oracleData.folderPath,ev.eventName, ev.random ? '^' : null);
                return;
            }
            orig(self);
        }
    }

    internal class OracleBehaviorModule
    {
        private readonly WeakReference<OracleBehavior> behaviorRef;

        public OracleData oracleData;

        public OracleBehaviorData behaviorData;

        public int currentEventIndex = 0;

        public OracleEventData CurrentEvent => behaviorData.events[currentEventIndex];

        public OracleBehaviorModule(OracleBehavior self, OracleData oracleData)
        {
            behaviorRef = new WeakReference<OracleBehavior>(self);
            this.oracleData = oracleData;
        }

        // true代表覆盖
        public bool SeePlayer()
        {
            if(!behaviorRef.TryGetTarget(out var self))
                return false;
            var slugbase = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData();
            var data = slugbase.ForceGet<OracleSaveData>(OracleSave);

            if (!oracleData.behaviors.TryGetValue(data.StepCount(self.oracle.ID), out behaviorData))
            {
                behaviorData = oracleData.normalBehavior;
                Plugin.Log("Custom Oracle","Use normal behavior");
            }
            else
            {
                Plugin.Log("Custom Oracle", $"Use behavior, Step:{data.GetCount(self.oracle.ID) - 1}");
            }
      
            slugbase.Set(OracleSave, data);
            currentEventIndex = 0;
            if (self is SSOracleBehavior ss)
            {
                ss.NewAction(GetAction(CurrentEvent.eventName));
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
            }
            else if (self is SSOracleRotBehavior rot)
            {
                rot.InitateConversation(CustomOracleBehavior.PlaceHolder);
                rot.conversation.LoadTextFromCustomFile(oracleData.slugcatId.value, CurrentEvent.eventName,
                    CurrentEvent.random ? '^' : null);
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
            }
            else if (self is CLOracleBehavior cl)
            {
                cl.dialogBox.LoadTextFromCustomFile(oracleData.slugcatId.value, CurrentEvent.eventName,
                    CurrentEvent.random ? '^' : null);
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;

            }
            else if (self is SLOracleBehaviorHasMark sl)
            {
                sl.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(CustomOracleBehavior.PlaceHolder, 
                    self, SLOracleBehaviorHasMark.MiscItemType.NA);
                sl.currentConversation.LoadTextFromCustomFile(oracleData.slugcatId.value, CurrentEvent.eventName,
                    CurrentEvent.random ? '^' : null);
            }
            return behaviorData != null;
        }

    }




    // 数据读取
    internal static partial class OracleHooks
    {
        public static void ReadOracleData()
        {
            var settings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error};
            foreach (var path in ModManager.ActiveMods.Select(i => i.basePath))
            {
                if (Directory.Exists(Path.Combine(path, "slugcatutils", "oracle")))
                {
                    foreach (var info in new DirectoryInfo(Path.Combine(path, "slugcatutils", "oracle")).GetFiles(
                                 "*.json"))
                    {
                        try
                        {
                            var data = JsonConvert.DeserializeObject<OracleData>(File.ReadAllText(info.FullName),settings);
                            if (Datas.Any(i => i.oracleId == data.oracleId && i.slugcatId == data.slugcatId))
                                Plugin.LogError("Custom Oracle", $"Save Id at:{data.oracleId}-{data.slugcatId}");
                            Datas.Add(data);
                            Plugin.Log("Custom Oracle",$"Add new oracle data. Slugcat:{data.slugcatId}, Oracle:{data.oracleId}, behaviorCount:{data.behaviors.Count}, Conversations:{data.otherConversations.Count}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            Plugin.LogError("Custom Oracle Json Error", $"File: {info.Name}\n{e}");
                        }
                    }
                }
            }
            RegisterCondition("HasMark", ((game, _) =>
            {
                if (game.session is StoryGameSession session &&
                    session.saveState.deathPersistentSaveData.theMark)
                    return true;
                return false;
            } ));


            #region SLConditions

            RegisterCondition("SLLeaves", ((game, args) =>
            {
                if (game.session is StoryGameSession session)
                {
                    return Compare(session.saveState.miscWorldSaveData.SLOracleState.leaves, float.Parse(args[1]),
                        args[0]);
                }
    
                return false;
            }));

            RegisterCondition("SLNeuronsLeft", ((game, args) =>
            {
                if (game.session is StoryGameSession session)
                {
                    return Compare(session.saveState.miscWorldSaveData.SLOracleState.neuronsLeft, float.Parse(args[1]),
                        args[0]);
                }

                return false;
            }));

            RegisterCondition("SLTotNeuronsGiven", ((game, args) =>
            {
                if (game.session is StoryGameSession session)
                {
                    return Compare(session.saveState.miscWorldSaveData.SLOracleState.totNeuronsGiven, float.Parse(args[1]),
                        args[0]);
                }

                return false;
            }));

            RegisterCondition("SLAnnoyances", ((game, args) =>
            {
                if (game.session is StoryGameSession session)
                {
                    return Compare(session.saveState.miscWorldSaveData.SLOracleState.annoyances, float.Parse(args[1]),
                        args[0]);
                }

                return false;
            }));

            RegisterCondition("SLLikesPlayer", ((game, args) =>
            {
                if (game.session is StoryGameSession session)
                {
                    return Compare(session.saveState.miscWorldSaveData.SLOracleState.likesPlayer, float.Parse(args[1]),
                        args[0]);
                }

                return false;
            }));

            RegisterCondition("SLTotInterrupt", ((game, args) =>
            {
                if (game.session is StoryGameSession session)
                {
                    return Compare(session.saveState.miscWorldSaveData.SLOracleState.totalInterruptions, float.Parse(args[1]),
                        args[0]);
                }

                return false;
            }));
            RegisterCondition("SLHasToldNeuron", ((game, _) =>
            {
                if (game.session is StoryGameSession session &&
                    session.saveState.miscWorldSaveData.SLOracleState.hasToldPlayerNotToEatNeurons)
                    return true;
                return false;
            }));
            #endregion

            #region SSCondition

            RegisterCondition("SSThrowOuts", ((game, args) =>
            {
                if (game.session is StoryGameSession session)
                   return Compare(session.saveState.miscWorldSaveData.SSaiThrowOuts, float.Parse(args[1]),
                        args[0]);
                return false;
            }));

            RegisterCondition("SSHalcyonStolen", ((game, _) =>
            {
                if (game.session is StoryGameSession session &&
                    session.saveState.miscWorldSaveData.halcyonStolen)
                    return true;
                return false;
            }));

            #endregion

        }

        public static bool Compare(float a, float b, string comp)
        {
            switch (comp)
            {
                case ">": return a > b;
                case "<": return a < b;
                case ">=": return a >= b;
                case "<=": return a <= b;
                case "==": return Math.Abs(a - b) < 0.01f;
                case "!=": return Math.Abs(a - b) > 0.01f;
            }

            return false;
        }

        private static readonly HashSet<OracleData> Datas = new();

        public const string OracleSave = $"{Plugin.ModId}_OracleSave";

        [JsonObject(MemberSerialization.OptIn)]
        public class OracleSaveData
        {
            [JsonProperty]
            Dictionary<string,int> meetOracleTimes = new ();
            
            public int GetCount(Oracle.OracleID id)
            {
                return meetOracleTimes[id.value];
            }
            public int StepCount(Oracle.OracleID id)
            {
                if (meetOracleTimes.TryGetValue(id.value, out var count))
                {
                    meetOracleTimes[id.value] = count + 1;
                    return count;
                }
                else
                {
                    meetOracleTimes.Add(id.value, 1);
                    return 0;
                }
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OracleData
    {
        [JsonProperty]
        private string Slugcat
        {
            set
            {
                slugcatId = new SlugcatStats.Name(value);
                folderPath = slugcatId.value;
            } 
        }

        [JsonProperty]
        private string Oracle
        {
            set => oracleId = new Oracle.OracleID(value);
        }

        [JsonProperty]
        private OracleBehaviorData[] Behaviors
        {
            set
            {
                foreach (var behavior in value)
                {
                    if (behavior.enterTimes == -1)
                    {
                        if (normalBehavior != null)
                            Plugin.LogError("Custom Oracle","Already contains normal behavior");
                        normalBehavior = behavior;
                    }
                    else
                    {
                        if (behaviors.ContainsKey(behavior.enterTimes))
                            Plugin.LogError("Custom Oracle", $"Already contains behavior at EnterTimes:{behavior.enterTimes}");
                        else
                            behaviors.Add(behavior.enterTimes, behavior);
                    }
                }
            }
        }

        public string folderPath = string.Empty;

        public SlugcatStats.Name slugcatId;

        public Oracle.OracleID oracleId;

        public Dictionary<int, OracleBehaviorData> behaviors = new();

        [CanBeNull]
        public OracleBehaviorData normalBehavior;

        [JsonProperty("Conversations")] 
        public Dictionary<string, OracleEventData> otherConversations = new();


    }


    public class OracleBehaviorData
    {
        [JsonProperty("Events")]
        public OracleEventData[] events;

        [JsonProperty("EnterTimes")]
        public int enterTimes = -1;
    }
    public class OracleEventData
    {
        [JsonProperty("Name")]
        public string eventName;

        [JsonProperty("Loop")]
        public bool loop;

        [JsonProperty("Random")]
        public bool random;

        [JsonProperty("MinWait")]
        public float minWait;

        [JsonProperty("MaxWait")]
        public float maxWait;

        [JsonProperty("ReadPearl")]
        public bool readPearl;
    }
}
