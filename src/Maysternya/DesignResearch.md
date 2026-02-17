# Design and Research

## Mod Structure

### Packages

Each Workshop mod is stored in its own directory under `workshop\content\{gameId}\{modId}`. This usually contains a number of files and subdirectories or archives, but apparently can also just be a single archive instead. `gameId` and `modId` are numerical strings defined by Steam and contain no useful information on their own.

A mod may contain multiple "packages", not all of which are relevant at the same time. Different packages can apply to different game versions, and packages can also be marked as "informational", meaning they don't contain game content at all and are only relevant for display in the game's mod manager.

A package can be present in the mod as
- a subdirectory
- a zip archive
- a HashFS archive

The contents of packages in subdirectories and zip archives can be accessed via the file system and any standard zip functionality respectively. `.scs` files can be either simply renamed zip archives HashFS archives, in which case they will require an extraction tool like the one [provided by SCS](https://modding.scssoft.com/wiki/Documentation/Tools/Game_Archive_Extractor) or [this open source implementation](https://github.com/sk-zk/Extractor) to unpack.

The information relevant to this application is contained in plain text data files with the extension `.sii` that use a syntax vaguely similar to JSON.

### Metadata

Metadata about each package is stored in a `manifest.sii` file within the respective package. This information mostly affects how the game interacts with the mod on an administrative level, and how it is displayed in the mod manager. It is irrelevant to how the actual game engine interacts with the mod.

Notably for this application, the manifest apparently does not necessarily need to contain a name for the mod. Presumably in that case the game uses the name provided by the Steam Workshop registration.

Example:

```json lines
SiiNunit
{
mod_package : .package_name {

    # Package version can be any string with any length.
    package_version: "2.0.10"

    # Author can be any string with any length.
    author: "Harven & more"

    # Categories is an array of strings.
    category[]: "truck"
    
    # Icon is a path relative to mod root directory.
    icon: "freight_flb.jpg"

    # Description file is a path relative to mod root directory.
    description_file: "mod_description.txt"
    
    mp_mod_optional: true
}
}
```

### Compatible Versions

A mod's packages are tied together by the `versions.sii` file in the mod's root directory. This file declares which packages are present within the mod, which game versions they are valid for, and which packages are informational only.

Example:

```json lines
SiiNunit
{
package_version_info : .info.compatible.versions
{
package_name: "compatibility_info"
informational: true
}

package_version_info : .154
{
package_name: "154"
compatible_versions[]: "1.54.*"
compatible_versions[]: "1.55.*"
compatible_versions[]: "1.56.*"
}

package_version_info : .157
{
package_name: "157"
compatible_versions[]: "1.57.*"
}
}
```

This mod contains 3 packages:
- Package `151` compatible with game versions 1.51 through 1.56
- Package `157` only compatible with game version 1.57
- Package `compatibility_info` that is only informational and has no compatibility information. The data in this informational package will be used when displaying the mod in a game version other than the ones listed for any of the other two packages.

For a mod to be not version locked at all, it needs to contain a package that is neither informational nor has any compatible versions listed.

## Game Version

The currently installed game version can be found in the respective game's root directory, in a package named `version.scs` (to be extracted using the SCS extractor), which in turn consists solely of a `version.sii` file. This file contains various pieces of identifying information, including the exact current version. Most of the entries are `pack_identifiers` lines presumably containing hashes for the various pieces of content to validate the integrity of the installation.

```json lines
SiiNunit
{
fs_pack_set : _nameless.14b.fa70 {
 application: ats
 version: "1.57.2.3"
 platforms: 1
 platforms[0]: pc
 creation_timestamp: 1765464560
 pack_verifiers: 200
 pack_verifiers[0]: 010246AB47C9DEBD10D74BAB91CFAAFDF1BEE26168E4A67F6C487DC6A208747A
 pack_verifiers[1]: 0155EB5AF038A739E7DB4EC822798FC1D95BFA349C6F8ED09E6E02D701A8D57B
 ...
}

}

```

## Design

The game IDs are fixed values and defined as constants in the application. The mod IDs are just whatever directories happen to be present for the respective game and have no further meaning on their own. In particular, they are not useful for identifying the respective mod in a human-readable way, as they are just arbitrary numerical strings.

`.sii` files are parsed using code adapted from https://github.com/SirTony/sii-unit-parser.

Determining the highest compatible version is easy: Parse `versions.sii` and find the non-informational package with the highest compatible version, or no compatible versions at all.

Obtaining the metadata is fundamentally not difficult either: Read the `manifest.sii` file in the package determined above; it is assumed that every package contains a manifest file. However, many mods don't include a display name in the manifest, apparently relying on the information shown in the game coming from Steam. As the mod IDs are just long numbers and cannot be used to tell the user what the individual mods are, as a workaround the first line of the description file is shown as the name instead. The entire description file is shown in the tooltip of an info icon next to the name. 

If a relevant package is present as an archive, access it via .NET's built-in zip functionality to read the manifest file. If the archive cannot be read using the zip functionality, that means it is a HashFS archive; extract it to a temporary location using one the open source HashFS extractor, and read the manifest file from the file system. The open source extractor is preferable to the SCS one, as it allows extracting individual files from the archive, avoiding the extraction of entire, possibly large packages just to access the small manifest and description files.  
