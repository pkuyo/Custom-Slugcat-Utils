using BepInEx;
using System;
using System.Linq;
using CustomSlugcatUtils.Hooks;
using UnityEngine;

using System.Security.Permissions;
using CustomSlugcatUtils.Tools;
using Random = UnityEngine.Random;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace CustomSlugcatUtils
{
    [BepInPlugin(ModId, "Custom Slugcat Utils", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModId = "CustomSlugcatUtils";

        public const string Version = "1.0.0";
        public void OnEnable()
        {
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            //On.RWCustom.Custom.Log += (_, values) => Debug.Log(string.Join(" ", values));
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            Plugin.Log($"Mod Version:{Version}");
     
            orig(self);
         

            try
            {
                if (!isLoaded)
                {
                    isLoaded = true;

                    StartCoroutine(ErrorTracker.LateCreateExceptionTracker());
                    SessionHooks.OnModsInit();
                    DevToolsHooks.OnModsInit();
                    CraftHooks.OnModsInit();
                    CustomGrababilityHooks.OnModsInit();
                    CustomEdibleHooks.OnModInit();
                    OracleHooks.OnModsInit();   
                }

            }
            catch (Exception e)
            {
                ErrorTracker.TrackError(e, "Custom Slugcat Utils OnModsInit Failed!");
                Debug.LogException(e);
            }
        }


    
        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {

            try
            {
                if (!isPostLoaded)
                {
                    isPostLoaded = true;

                    CycleLimitHooks.OnModInit();
                    ChatLogHooks.OnModsInit();
                    PlayerGraphicsHooks.OnModsInit();
                    CoopHooks.OnModsInit();
                    
                }
            }
            catch (Exception e)
            {
                ErrorTracker.TrackError(e, "Custom Slugcat Utils PostModsInit Failed!");
                Debug.LogException(e);
            }
            
            orig(self);
        }



        private bool isPostLoaded = false;
        private bool isLoaded = false;
        public static void Log(object m)
        {
            Debug.Log($"[Custom Slugcat Utils] {m}");
        }

        public static void LogDebug(object m)
        {
            Debug.Log($"[Custom Slugcat Utils] {m}");
        }
        public static void LogWarning(object m)
        {
            Debug.LogWarning($"[Custom Slugcat Utils] {m}");
        }
        public static void Log(object header, object m)
        {
            Debug.Log($"[Custom Slugcat Utils - {header}] {m}");
        }
        
        public static void LogWarning(object header, object m)
        {
            Debug.LogWarning($"[Custom Slugcat Utils - {header}] {m}");
        }

        public static void LogError(object header, object m)
        {
            Debug.LogError($"[Custom Slugcat Utils - {header}] {m}");
            ErrorTracker.TrackError(header.ToString(),m.ToString());
        }

        public void Update()
        {
            ErrorTracker.Instance?.Update();
        }
    }
}