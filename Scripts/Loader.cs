using BepInEx;

namespace MR223_plugin
{
    [BepInDependency("pl.szikaka.receiver_2_modding_kit")]
    [BepInPlugin("Ciarencew.MR223", PluginInfo.PLUGIN_NAME, "0.1.0")]
    public class Loader : BaseUnityPlugin
    {
        public static readonly string folder_name = "MR223_Files";
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin Ciarencew.MR223 is loaded!");
        }
    }
}
