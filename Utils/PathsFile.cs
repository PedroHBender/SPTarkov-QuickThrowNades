using System.IO;

namespace QuickThrow.Utils
{
    public static class PathsFile
    {
        // Keep generated files inside BepInEx/plugins/QuickThrow so users can
        // delete or move the mod without hunting for config files elsewhere.
        public static readonly string LogFilePath = Path.Combine(
            BepInEx.Paths.PluginPath, "QuickThrow", "QuickThrow_log.txt");

        public static readonly string DebugPath = Path.Combine(
            BepInEx.Paths.PluginPath, "QuickThrow", "debug.cfg");
    }
}
