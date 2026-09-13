using static DaftAppleGames.SeaTruckSpeedMod_BZ.SeaTruckSpeedPluginBz;

namespace DaftAppleGames.SeaTruckSpeedMod_BZ
{
    /// <summary>
    /// This class allows us to keep track of SeaTrucks that we've modded so we can dynamically
    /// change the modifier in real time.
    /// </summary>
    /// 
    internal class SeaTruckHistoryItem
    {
        private readonly SeaTruckMotor _SeaTruckInstance;
        private readonly float _originalSeaTruckDrag;

        public SeaTruckMotor SeaTruckInstance => _SeaTruckInstance;

        public SeaTruckHistoryItem(SeaTruckMotor truckInstance)
        {
            _SeaTruckInstance = truckInstance;
            _originalSeaTruckDrag = truckInstance.pilotingDrag;

            ApplyDragdModifier(ConfigFile.DragModifier);
        }

        /// <summary>
        /// Apply a multiplier to the SeaTruck speed
        /// </summary>
        internal void ApplyDragdModifier(float modifier)
        {
            _SeaTruckInstance.pilotingDrag = _originalSeaTruckDrag / modifier;
            ModDebugLog.LogDebug($"Updated SeaTruck. Drag modifier: {modifier}, from: {_originalSeaTruckDrag} to: {_originalSeaTruckDrag / modifier}");
        }
    }
}
