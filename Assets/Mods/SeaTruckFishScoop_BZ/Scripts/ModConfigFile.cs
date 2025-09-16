using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Sea Truck Fish Scoop")]
    internal class ModConfigFile : ConfigFile
    {
        [Toggle("Scoop While Static")]
        public bool ScoopWhileStatic = false;

        [Toggle("Scoop While Piloting")]
        public bool ScoopWhilePiloting = true;
    }
}