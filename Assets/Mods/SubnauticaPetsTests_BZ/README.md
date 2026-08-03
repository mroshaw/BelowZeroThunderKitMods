# Subnautica Pets in-game tests

This is a development-only BepInEx plugin. Deploy its `SubnauticaPetsTests_BZ` ThunderKit manifest alongside the
normal `SubnauticaPets_BZ` manifest when testing. Do not add the test assembly to the release mod manifest.

Run these commands from the Below Zero developer console:

- `pettest run fragments`
- `pettest run dna`
- `pettest run biome`
- `pettest run all`
- `pettest status`
- `pettest cancel`

The runner teleports the player through each fixed spawn location, waits for the surrounding streaming range,
then polls for entity-cell deserialization before validating the expected prefab and components. DNA tests allow
for samples falling vertically from their configured spawn point onto terrain. Results are written as `[PetTests]`
entries in `Player.log`. The runner restores the player's original position when the run completes or is cancelled.

Run fixed-spawn tests in a new, unmodified save. A test correctly fails if its expected object was previously
collected, destroyed, or scanned away.

The `biome` suite does not teleport the player. It audits the live, Nautilus-patched loot distribution in both
lookup directions, including each DNA prefab's expected number of unique biome entries, spawn count, and positive
probability. It also reports how many matching instances are currently loaded. A valid registration with no loaded
instance is a warning rather than a failure because biome spawning is probabilistic and streaming-dependent.
The `all` suite runs this audit before starting the fixed fragment and DNA location tests.
