using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using QuickThrow.Utils;
using UnityEngine;

namespace QuickThrow
{
    [BepInPlugin("com.spt.QuickThrow", "QuickThrow", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> DisableFastGrenade = null!;
        public static ConfigEntry<KeyboardShortcut> KeyboardBindingOrigine = null!;
        public static ConfigEntry<KeyboardShortcut> KeyboardBindingShort = null!;

        public static ManualLogSource? LogSource;

        private static Harmony? HarmonyInstance;

        private void Awake()
        {
            // Keep BepInEx's logger available to every patch class. This is useful
            // for troubleshooting Harmony patches without enabling the custom file log.
            LogSource = Logger;

            // Holding this key bypasses the mod and lets Tarkov equip the grenade
            // normally. That preserves the vanilla "hold grenade before throwing" flow.
            KeyboardBindingOrigine = Config.Bind(
                "Fast grenade",
                "Cancel fast grenade",
                new KeyboardShortcut(KeyCode.LeftShift),
                "Keyboard to cancel fast grenade"
            );

            // Holding this key tells the quick grenade controller to use the low
            // throw path, similar to vanilla right-click/alternate grenade throw.
            KeyboardBindingShort = Config.Bind(
                "Fast grenade",
                "Short grenade",
                new KeyboardShortcut(KeyCode.LeftControl),
                "Forces low-throw behavior for quick grenade throws when using the fast grenade feature"
            );

            // Master toggle. When enabled, all patches fall back to vanilla behavior.
            DisableFastGrenade = Config.Bind(
                "Fast grenade",
                "disabled fast grenade",
                false,
                "Enables or disables fast grenade"
            );

            InitializeFiles();
            InitializeActionLogger();
            SetupHarmonyPatches();

            LogSource?.LogInfo("QuickThrow initialized");
        }

        private static void InitializeFiles()
        {
            // The custom logger writes beside the plugin DLL so the mod can be
            // debugged even when the full BepInEx log is noisy.
            var logDirectory = Path.GetDirectoryName(PathsFile.LogFilePath);

            if (!string.IsNullOrEmpty(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            if (!File.Exists(PathsFile.DebugPath))
            {
                File.WriteAllText(PathsFile.DebugPath, "false");
            }

            if (!File.Exists(PathsFile.LogFilePath))
            {
                File.WriteAllText(PathsFile.LogFilePath, "");
            }

            LogSource?.LogInfo($"Log file: {PathsFile.LogFilePath}");
        }

        private static void InitializeActionLogger()
        {
            // File logging is disabled by default through debug.cfg. Set that file
            // to "true" when you want the extra per-action debug lines.
            QuickThrowLogger.Init(EnumLoggerMode.DirectWrite);
            Application.quitting += QuickThrowLogger.OnApplicationQuit;
        }

        private static void SetupHarmonyPatches()
        {
            // PatchAll scans this assembly for every [HarmonyPatch] class, including
            // the Player methods and the SPT 4.x input translator patch.
            HarmonyInstance = new Harmony("com.spt.quickthrow");
            HarmonyInstance.PatchAll();

            LogSource?.LogInfo("Harmony patches applied");
        }
    }
}
