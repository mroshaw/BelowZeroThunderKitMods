using DaftAppleGames.TameCreatures_BZ.TameCreatures;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using static DaftAppleGames.TameCreatures_BZ.TameCreaturesPlugin;

namespace DaftAppleGames.TameCreatures_BZ
{
    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Tame Creatures")]
    public class ModConfigFile : ConfigFile
    {
        [Toggle("Tame Alien Robots", Tooltip = "Toggle this ON to tame Alien Robots."), OnChange(nameof(OnTameAlienRobotsChanged))]
        public bool TameAlienRobots = true;

        [Toggle("Tame Brinewings", Tooltip = "Toggle this ON to tame Brinewings."), OnChange(nameof(OnTameBrinewingsChanged))]
        public bool TameBrinewings = true;

        [Toggle("Tame Brute Sharks", Tooltip = "Toggle this ON to tame Brute Sharks."), OnChange(nameof(OnTameBruteSharksChanged))]
        public bool TameBruteSharks = true;

        [Toggle("Tame Chelicerates", Tooltip = "Toggle this ON to tame Chelicerates."), OnChange(nameof(OnTameCheliceratesChanged))]
        public bool TameChelicerates = true;

        [Toggle("Tame Cryptosuchus", Tooltip = "Toggle this ON to tame Cryptosuchus."), OnChange(nameof(OnTameCryptosuchusChanged))]
        public bool TameCryptosuchus = true;

        [Toggle("Tame Ice Worms", Tooltip = "Prevents noise-based detection of the player and vehicles. Scripted encounters still occur.")]
        public bool TameIceWorms = true;

        [Toggle("Tame Lily Paddlers", Tooltip = "Toggle this ON to tame Lily Paddlers."), OnChange(nameof(OnTameLilyPaddlersChanged))]
        public bool TameLilyPaddlers = true;

        [Toggle("Tame Rock Punchers", Tooltip = "Toggle this ON to tame Rock Punchers."), OnChange(nameof(OnTameRockPunchersChanged))]
        public bool TameRockPunchers = true;

        [Toggle("Tame Shadow Leviathans", Tooltip = "Toggle this ON to tame Shadow Leviathans."), OnChange(nameof(OnTameShadowLeviathansChanged))]
        public bool TameShadowLeviathans = true;

        [Toggle("Tame Snow Stalkers", Tooltip = "Toggle this ON to tame Snow Stalkers."), OnChange(nameof(OnTameSnowStalkersChanged))]
        public bool TameSnowStalkers = true;

        [Toggle("Tame Squidsharks", Tooltip = "Toggle this ON to tame Squidsharks."), OnChange(nameof(OnTameSquidsharksChanged))]
        public bool TameSquidsharks = true;

        [Toggle("Tame Symbiotes", Tooltip = "Toggle this ON to tame Symbiotes."), OnChange(nameof(OnTameSymbiotesChanged))]
        public bool TameSymbiotes = true;

        /// <summary>
        /// Enable detailed logging
        /// </summary>
        [Toggle("Detailed logging", Tooltip="Use this to produce a detailed log when reporting bugs. Logs are written to %LOCALAPPDATA%low\\Unknown Worlds\\Subnautica Below Zero\\Player.log"), OnChange(nameof(OnLoggingChanged))]
        public bool DetailedLogging = false;
        
        /// <summary>
        /// Handle toggling of detailed logging
        /// </summary>
        private void OnLoggingChanged(ToggleChangedEventArgs eventArgs)
        {
            ModDebugLog.SetDetailedLoggingState(eventArgs.Value);
        }

        private void OnTameAlienRobotsChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.PrecursorDroid, eventArgs.Value);
        }

        private void OnTameBrinewingsChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.Brinewing, eventArgs.Value);
        }

        private void OnTameBruteSharksChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.BruteShark, eventArgs.Value);
        }

        private void OnTameCheliceratesChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.Chelicerate, eventArgs.Value);
        }

        private void OnTameCryptosuchusChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.Cryptosuchus, eventArgs.Value);
        }

        private void OnTameLilyPaddlersChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.LilyPaddler, eventArgs.Value);
        }

        private void OnTameRockPunchersChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.RockPuncher, eventArgs.Value);
        }

        private void OnTameShadowLeviathansChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.ShadowLeviathan, eventArgs.Value);
        }

        private void OnTameSnowStalkersChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.SnowStalker, eventArgs.Value);
        }

        private void OnTameSquidsharksChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.SquidShark, eventArgs.Value);
        }

        private void OnTameSymbiotesChanged(ToggleChangedEventArgs eventArgs)
        {
            AggressiveCreatures.SetPlayerFriendlinessOnAll(TechType.Symbiote, eventArgs.Value);
        }
    }
}
