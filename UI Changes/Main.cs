using HarmonyLib;
using System;
using System.Reflection;
using UnityModManagerNet;

namespace UIChanges
{
    internal static class Main
    {
        public static bool Enabled;
        public static UnityModManager.ModEntry.ModLogger logger;
        public static void DebugLog(string msg)
        {
            if (logger != null) logger.Log(msg);
        }
        public static void DebugError(Exception ex)
        {
            if (logger != null) logger.Log(ex.ToString() + "\n" + ex.StackTrace);
        }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                logger = modEntry.Logger;
                modEntry.OnToggle = OnToggle;
                var harmony = new Harmony(modEntry.Info.Id);

                harmony.PatchAll(Assembly.GetExecutingAssembly());
            } catch (Exception ex)
            {
                DebugLog("loading error");
                DebugError(ex);
            }
            
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }
    }
}