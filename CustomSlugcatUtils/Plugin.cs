﻿using BepInEx;
using System;
using CustomSlugcatUtils.Hooks;
using UnityEngine;

using System.Security.Permissions;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace CustomSlugcatUtils
{
    [BepInPlugin(ModId, "Custom Slugcat Utils", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModId = "CustomSlugcatUtils";

        public void OnEnable()
        {
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                if (!isLoaded)
                {
                    SessionHooks.OnModsInit();
                    DevToolsHooks.OnModsInit();
                    CraftHooks.OnModsInit();
                    CustomEdibleHooks.OnModInit();
                    OracleHooks.OnModsInit();
                    isLoaded = true;

                }

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {

            try
            {
                if (!isPostLoaded)
                {
                    CycleLimitHooks.OnModInit();
                    ChatLogHooks.OnModsInit();
                    PlayerGraphicsHooks.OnModsInit();
                    CoopHooks.OnModsInit();

                    isPostLoaded = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }



        private bool isPostLoaded = false;
        private bool isLoaded = false;
        public static void Log(object m)
        {
            Debug.Log($"[Custom Slugcat Utils] {m}");
        }


        public static void LogError(object m)
        {
            Debug.LogError($"[Custom Slugcat Utils] {m}");
        }

        public static void Log(object header, object m)
        {
            Debug.Log($"[Custom Slugcat Utils - {header}] {m}");
        }


        public static void LogError(object header, object m)
        {
            Debug.LogError($"[Custom Slugcat Utils - {header}] {m}");
        }
    }
}