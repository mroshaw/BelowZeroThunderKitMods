using System.IO;
using System.Reflection;
using Nautilus.Json;

namespace DaftAppleGames.StartupCommand
{
    internal class ScriptConfigFile: JsonFile
    {
        public override string JsonFilePath => Path.Combine(Path.Combine(BepInEx.Paths.ConfigPath, Assembly.GetExecutingAssembly().GetName().Name),
            "command_config.json");
        
        // Json properties to store
        public string StartupScript = "";
    }
}