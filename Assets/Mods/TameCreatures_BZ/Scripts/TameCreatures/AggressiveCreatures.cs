using System.Collections.Generic;
using static DaftAppleGames.TameCreatures_BZ.TameCreaturesPlugin;

namespace DaftAppleGames.TameCreatures_BZ.TameCreatures
{
    internal static class AggressiveCreatures
    {
        private static readonly Dictionary<TechType, HashSet<Creature>> CreaturesByTechType =
            new Dictionary<TechType, HashSet<Creature>>();

        /// <summary>
        /// Registers a supported aggressive creature and applies its configured friendliness
        /// </summary>
        internal static void Register(Creature creature)
        {
            TechType techType = CraftData.GetTechType(creature.gameObject);
            if (!TryGetConfiguredFriendliness(techType, out bool isFriendlyToPlayer))
            {
                return;
            }

            if (!CreaturesByTechType.TryGetValue(techType, out HashSet<Creature> creatures))
            {
                creatures = new HashSet<Creature>();
                CreaturesByTechType.Add(techType, creatures);
            }

            ModDebugLog.LogDebug($"Adding {techType} '{creature.gameObject.name}' to Aggressive Creatures list, friendliness: {isFriendlyToPlayer}.");
            SetPlayerFriendlinessOnCreature(techType, creature, isFriendlyToPlayer);
            creatures.Add(creature);
        }

        /// <summary>
        /// Unregisters a supported aggressive creature
        /// </summary>
        internal static void Unregister(Creature creature)
        {
            TechType techType = CraftData.GetTechType(creature.gameObject);
            if (!CreaturesByTechType.TryGetValue(techType, out HashSet<Creature> creatures))
            {
                return;
            }

            ModDebugLog.LogDebug($"Removing {techType} '{creature.gameObject.name}' from Aggressive Creatures list.");
            creatures.Remove(creature);

            if (creatures.Count == 0)
            {
                CreaturesByTechType.Remove(techType);
            }
        }

        /// <summary>
        /// Sets player friendliness on all registered creatures of the given type
        /// </summary>
        internal static void SetPlayerFriendlinessOnAll(TechType creatureTechType, bool isFriendlyToPlayer)
        {
            HashSet<Creature> creatures;
            if (!CreaturesByTechType.TryGetValue(creatureTechType, out creatures))
            {
                return;
            }

            foreach (Creature creature in creatures)
            {
                if (creature)
                {
                    SetPlayerFriendlinessOnCreature(creatureTechType, creature, isFriendlyToPlayer);
                }
            }
        }

        /// <summary>
        /// Gets the configured friendliness when the creature type is supported
        /// </summary>
        private static bool TryGetConfiguredFriendliness(TechType creatureTechType, out bool isFriendlyToPlayer)
        {
            switch (creatureTechType)
            {
                case TechType.PrecursorDroid:
                    isFriendlyToPlayer = ConfigFile.TameAlienRobots;
                    return true;
                case TechType.Brinewing:
                    isFriendlyToPlayer = ConfigFile.TameBrinewings;
                    return true;
                case TechType.BruteShark:
                    isFriendlyToPlayer = ConfigFile.TameBruteSharks;
                    return true;
                case TechType.Chelicerate:
                    isFriendlyToPlayer = ConfigFile.TameChelicerates;
                    return true;
                case TechType.Cryptosuchus:
                    isFriendlyToPlayer = ConfigFile.TameCryptosuchus;
                    return true;
                case TechType.LilyPaddler:
                    isFriendlyToPlayer = ConfigFile.TameLilyPaddlers;
                    return true;
                case TechType.RockPuncher:
                    isFriendlyToPlayer = ConfigFile.TameRockPunchers;
                    return true;
                case TechType.ShadowLeviathan:
                    isFriendlyToPlayer = ConfigFile.TameShadowLeviathans;
                    return true;
                case TechType.SnowStalker:
                    isFriendlyToPlayer = ConfigFile.TameSnowStalkers;
                    return true;
                case TechType.SquidShark:
                    isFriendlyToPlayer = ConfigFile.TameSquidsharks;
                    return true;
                case TechType.Symbiote:
                    isFriendlyToPlayer = ConfigFile.TameSymbiotes;
                    return true;
                default:
                    isFriendlyToPlayer = false;
                    return false;
            }
        }

        /// <summary>
        /// Sets player friendliness on one creature
        /// </summary>
        private static void SetPlayerFriendlinessOnCreature(TechType creatureTechType, Creature creature, bool isFriendlyToPlayer)
        {
            ModDebugLog.LogDebug($"Setting friendliness to '{isFriendlyToPlayer}' on {creatureTechType} {creature.gameObject.name}.");
            creature.friendlyToPlayer = isFriendlyToPlayer;
        }
    }
}
