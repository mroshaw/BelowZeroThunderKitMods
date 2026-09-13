# Aqua Eclipse Now BZ

This mod adds a new console command to accelerate time to the next solar eclipse event!

## User Guide

Open the command prompt (usually via the tilda ~ keyboard key) and enter one of these commands:

- *eclipse now* - skips time to the next eclipse event in game time.
- *eclipse next* - shows you when the next eclipse event is due to occur in game time.

## Options

You can set a couple of options via the Options > Mods menu:

- **Time Before Eclipse** - the eclipse now command will forward game time to this many seconds before the next eclipse.
- **Time Skip Duration** - the time taken to smoothly transition from the current time to the time of the next eclipse.

The mod works by running a simulation of the game time and sky managers to find a point where the code determines an eclipse is occurring. The results are dependent on the accuracy of the simulation, which can be tinkered with using these mod options:

- **Simulation Iterations** - The higher the number, the longer the mod will look for an eclipse.
- Simulation Time Step - The lower the number, the more checks the mod will do at shorter time intervals, to find a more accurate time of an eclipse.
- **Simulation Threshold** - an eclipse occurs when the planet obscures the sun. The degree to which the planet obscures the sun is calculated in each iteration as a value between 0 and 1. The simulation ends when this value reaches the threshold set. The closer the value is to 1, the more likely you'll get a "total eclipse" rather than a partial one.

> ﻿**WARNING**! Use of the command skips actual game time. In doing so, you may miss or trigger in-game events that impact your playthrough!

## Installation and Dependencies

> ***IMPORTANT!** This mod uses **BepInEx** ﻿﻿and **Nautilus**﻿﻿. You **must** install the latest versions of these to use this mod. As the 2025 patch broke a lot of mods, you must use **Nautilus version 1.0.0-pre.50** or later.*

## Source Code

All of my mods are open source, and you can find the full source code in my [Below Zero Mods GitHub Repository](https://github.com/mroshaw/BelowZeroThunderKitMods).
