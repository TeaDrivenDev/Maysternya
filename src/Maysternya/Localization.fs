namespace TeaDriven.Maysternya.Localization

module Loc =
    /// Maysternya
    let WindowTitle = "Loc.WindowTitle"

    /// Steam Directory
    let SteamDirectory = "Loc.SteamDirectory"

    /// This is the directory of the Steam library your SCS truck simulators are installed in. By default this is 'C:\Program Files (x86)\Steam', but if your games are installed in another location, this must be the directory that contains 'steamapps'.
    let SteamDirectoryInfoMessage = "Loc.SteamDirectoryInfoMessage"

    /// HashFS Extractor
    let HashFsExtractor = "Loc.HashFsExtractor"

    /// Download
    let Download = "Loc.Download"

    /// The HashFS extractor is used to get metadata (name, author, description, version) for mods packaged as HashFS, as well as the installed game versions. Using it is optional, but recommended; only the display of some mods and the game versions will be affected if it is missing.
    /// 
    /// If using the extractor, version 2026-02-15 or newer is required; using an older version will cause the application to not work at all.
    let ExtractorInfoMessage = "Loc.ExtractorInfoMessage"

    /// Select Extractor executable
    let SelectExtractorExecutable = "Loc.SelectExtractorExecutable"

    /// Extractor
    let Extractor = "Loc.Extractor"

    /// Workshop directory found, but no ETS2 or ATS mod directories
    let WorkshopButNoModDirectories = "Loc.WorkshopButNoModDirectories"

    /// Workshop directory not found
    let WorkshopDirectoryNotFound = "Loc.WorkshopDirectoryNotFound"

    /// ETS2
    let Ets2 = "Loc.Ets2"

    /// ETS2 (version {0})
    let Ets2Version_Format = "Loc.Ets2Version.Format"

    /// ETS2 mod directory not found
    let Ets2ModsDirectoryNotFound = "Loc.Ets2ModsDirectoryNotFound"

    /// ATS
    let Ats = "Loc.Ats"

    /// ATS (version {0})
    let AtsVersion_Format = "Loc.AtsVersion.Format"

    /// ATS mod directory not found
    let AtsModsDirectoryNotFound = "Loc.AtsModsDirectoryNotFound"

    /// Refresh list
    let RefreshList = "Loc.RefreshList"

    /// ID
    let Id = "Loc.Id"

    /// Name
    let Name = "Loc.Name"

    /// Author
    let Author = "Loc.Author"

    /// Version
    let Version = "Loc.Version"

    /// Usable with
    let UsableWith = "Loc.UsableWith"

    /// Temporarily remove version restriction
    let RemoveRestrictionButtonToolTip = "Loc.RemoveRestrictionButtonToolTip"

    /// [No display name]
    let NoDisplayName = "Loc.NoDisplayName"

    /// [Metadata in {0}]
    let MetadataIn_Format = "Loc.MetadataIn.Format"

    /// [Package {0} not found]
    let PackageNotFound_Format = "Loc.PackageNotFound.Format"

    /// Show Log
    let ShowLog = "Loc.ShowLog"

    /// Hide Log
    let HideLog = "Loc.HideLog"

    /// Time
    let Time = "Loc.Time"

    /// Log Level
    let LogLevel = "Loc.LogLevel"

    /// Activity
    let Activity = "Loc.Activity"

    /// Message
    let Message = "Loc.Message"


module Log =
    /// Steam directory "{0}" not found
    let SteamDirectoryNotFound_Format = "Log.SteamDirectoryNotFound.Format"

    /// Workshop directory not found in Steam directory "{0}"
    let WorkshopDirectoryNotFound_Format = "Log.WorkshopDirectoryNotFound.Format"

    /// No ETS2 or ATS mod directories found in Workshop directory "{0}"
    let ModDirectoriesNotFound_Format = "Log.ModDirectoriesNotFound.Format"

    /// Steam path is "{0}"
    let SteamPathIs_Format = "Log.SteamPathIs.Format"

    /// Extractor path is "{0}"
    let ExtractorPathIs_Format = "Log.ExtractorPathIs.Format"

    /// ETS2 version {0}
    let Ets2Version_Format = "Log.Ets2Version.Format"

    /// No ETS2 version
    let NoEts2Version = "Log.NoEts2Version"

    /// ATS version {0}
    let AtsVersion_Format = "Log.AtsVersion.Format"

    /// No ATS version
    let NoAtsVersion = "Log.NoAtsVersion"

    /// {0}: {1} mods read
    let ModsRead_Format = "Log.ModsRead.Format"

    /// Removed version restriction from {0}
    let RemovedVersionRestriction_Format = "Log.RemovedVersionRestriction.Format"

    /// Error removing version restriction from {0}: {1}
    let ErrorRemovingVersionRestriction_Format = "Log.ErrorRemovingVersionRestriction.Format"

    /// Error reading versions from {0}: {1}
    let ErrorReadingVersionsFrom_Format = "Log.ErrorReadingVersionsFrom.Format"

    /// Error reading manifest from {0}: {1}
    let ErrorReadingManifestFrom_Format = "Log.ErrorReadingManifestFrom.Format"

    /// Reading mod {0}
    let ReadingMod_Format = "Log.ReadingMod.Format"

    /// Error reading mod {0}: {1}
    let ErrorReadingMod_Format = "Log.ErrorReadingMod.Format"

