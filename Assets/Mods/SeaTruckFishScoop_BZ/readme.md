# Sea Truck Fish Scoop BZ

Do you regularly find yourself on a deadly rampage, mowing down innocent hoop fish in your Seatruck? Feel bad? Well, you bloody well should do! Or maybe you just really need some fuel for that shiny new bioreactor?

This mod adds a "Seatruck Fish Scoop" upgrade module that, when enabled, acts like an underwater vacuum, slurping up innocent fish and depositing them into any attached aquarium Seatruck aquariums. You can then collect fish from the aquariums as normal, purge the content into open water, or even automatically refill base bioreactors! The module can also be configured to automatically eject scooped fish if attached aquariums are full.

## Features

- Adds a new Sea Truck upgrade to the Upgrade Workbench: the "SeaTruck Fish Scoop".
- Can be tweaked to scoop even when you’re not piloting the Seatruck, configured via the Options > Mods menu.
- Can be used to top up nearby bio-reactors, or just release caught fish back into the wild.

## User Guide

Using the mod is easy, and follows the same process as existing Seatruck upgrades:

- Craft the Seatruck Fish Scoop upgrade in the Upgrade Workbench.
- Install it in a Seatruck as you would any other upgrade.
- Select it using the 1-4 key for the slot it's installed in.
- Press the configured primary action once to toggle the scoop, or hold it to purge all aquariums.
- If you purge aquariums while close to base bioreactors, aquarium stock will be automatically transferred until they are at capacity.

## Options

Go to Options > Mod Options, and you can tweak some features of the mod:

- **Scoop while static** - if enabled, fish bumping into the Seatruck while it's static will be scooped. If false, the Seatruck must be moving to scoop.
- **Only scoop while piloting** - if enabled, you must be inside and piloting the Seatruck for fish to be scooped. If false, and "Scoop while static" is true, fish can be scooped while you are away from the Seatruck.
- **Release failed fish scoop** - if enabled, fish will be safely ejected if there is no room in attached aquariums. If false, fish will be damaged just as they are if you hit them without the scoop.
- **Bioreactor Range** - bioreactors within this range, at the point the scoop is "purged", will receive purged fish up until they are at capacity. Fish will be distributed across multiple bioreactors, if more than one if within range.
- **Show Alerts** - show or hide the "Scoop enabled/disabled" alerts.
- **Detailed Logging** - only enable this if you have an issue and want to provide useful logging when reporting a bug.

## Installation and Dependencies

> ***IMPORTANT!** This mod uses **BepInEx** ﻿﻿and **Nautilus**﻿﻿. You **must** install the latest versions of these to use this mod. As the 2025 patch broke a lot of mods, you must use **Nautilus version 1.0.0-pre.50** or later.*

## Source Code

All of my mods are open source, and you can find the full source code in my [Below Zero Mods GitHub Repository](https://github.com/mroshaw/BelowZeroThunderKitMods).
