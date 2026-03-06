# Maysternya

## What

Temporarily remove the version restriction from Steam Workshop mods for the games _Euro Truck Simulator 2_ and _American Truck Simulator_ by [SCS Software](https://www.scssoft.com)

## Why 

Mod authors may take a few days or weeks to update their mods after game updates, or might only do so after the full release of an update and not for an Open Beta. However, most mods usually continue to work fine with new game versions and might only be blocked from use by a version restriction. (It should be noted that few Workshop mods have this restriction in the first place, but if you happen to rely on one that does, that can be annoying.)

## How

The application shows a list of your Steam Workshop mods for each game with the respective version restrictions, if any, and lets you temporarily remove the restriction for mods that cannot be activated with the current version. This will stay until each mod is updated, after which the old version restriction should be removed anyway.<br />
You may have to restart your game, or at the very least reopen the mod manager, after unblocking mods.

You can also open each mod's directory and Steam Workshop page from here.

![Screenshot](./docs/screenshot.png "Screenshot")

## Important

- **This does not change the actual mods in any way.** Mods that are incompatible on a technical level won't work properly and may crash the game or break your save game, just like any outdated mod that has no version restriction in the first place. Many mods aren't often broken by game updates, but there is never a guarantee that they will continue to work.
- This only works for mods installed through the Steam Workshop. It knows nothing about the manually installed mods in your game data folder.
- This does nothing you couldn't do manually; it just saves you the hassle of finding the mod directories in the forest of numbers, and is a bit faster if you need to unblock multiple mods.
- The intended purpose for this is bridging the few weeks - at most - until a mod author updates their mod after a game update. The older the mods are that you reenable, the higher the risk for instabilities - again, as with any older mods. 

Note:
- There are some older mods with a differently formatted versions file that the parser currently cannot read. These will show errors in the log and not appear in the mod list, but they shouldn't have version restrictions anyway.

## Miscellaneous

The [HashFS extractor](https://github.com/sk-zk/Extractor) is not required for mod unblocking to work, but is needed to read detail information from some mods and find the installed game versions. Once downloaded and selected, you won't notice it working.

Light and dark theme are supported, but can currently not be selected manually; instead, the theme follows the system theme.

This is currently Windows-only; Linux support should be technically possible, but at least requires additional research. 

---

This includes the code of the now-abandoned [SII unit parser](https://github.com/SirTony/sii-unit-parser) by Tony King, with some minor enhancements and fixes.
