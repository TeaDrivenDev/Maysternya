namespace TeaDriven.Maysternya

module FileSystemTypes =
    type DirectoryPaths =
        {
            SteamPath: string
            WorkshopContentPath: string
            Ets2ModsPath: string
            AtsModsPath: string
        }

[<RequireQualifiedAccess>]
module FileSystem =
    open System.IO

    open TeaDriven.Maysternya.Domain

    open FileSystemTypes

    let determinePaths steamPath =
        let workshopPath = Path.Combine(steamPath, Constants.Paths.WorkshopContentPath)
        let ets2Path = Path.Combine(workshopPath, Constants.SteamIds.Ets2)
        let atsPath = Path.Combine(workshopPath, Constants.SteamIds.Ats)

        {
            SteamPath = steamPath
            WorkshopContentPath = workshopPath
            Ets2ModsPath = ets2Path
            AtsModsPath = atsPath
        }
