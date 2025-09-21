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
        [Toggle("Scoop While Static", Tooltip="If checked, the scoop will only work while the Sea Truck is moving.")]
        public bool ScoopWhileStatic = false;

        [Toggle("Only Scoop While Piloting", Tooltip="If checked, scooping will only take place when piloting the Sea Truck. If unchecked, the scoop will continue to work even if you exist the vehicle.")]
        public bool OnlyScoopWhilePiloting = true;
        
        [Toggle("Release Failed Scoop Fish", Tooltip="If checked, fish that can't be scooped (for example, if aquariums are full) will be released rather than hit.")]
        public bool ReleaseFailedScoopFish = true;

        [Slider("Bioreactor Range", Tooltip="Purging while within this distance to one or more base bioreactors will automatically stock them.", Step = 0.5f, Format = "{0:F2}", Min = 1.0f, Max = 100.0f, DefaultValue = 20f)]
        public float BioreactorRange = 20.0f;
        
        [Toggle("Show Alerts", Tooltip="If checked, messages will be displayed in the top left of the screen whenever a scoop related action takes place. Uncheck this to suppress those.")]
        public bool ShowScoopAlerts = true;

    }
}