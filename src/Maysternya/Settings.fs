namespace TeaDriven.Maysternya

[<RequireQualifiedAccess>]
module Settings =
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Serialization
    
    open TeaDriven.Maysternya.Domain
    
    let private serializationOptions =
        let fsharpOptions =
            JsonFSharpOptions.Default()
            
        let jsonOptions = fsharpOptions.ToJsonSerializerOptions()

        jsonOptions

    let settingsPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Constants.Application.Vendor,
            Constants.Application.Application)

    let settingsFileName = "settings.json"

    let serializeSettings settings =
        JsonSerializer.Serialize(settings, serializationOptions)

    let deserializeSettings<'Settings> (json: string) =
        JsonSerializer.Deserialize<'Settings>(json, serializationOptions)

    let saveSettings settings =
        let serializedSettings = serializeSettings settings

        Directory.CreateDirectory(settingsPath) |> ignore

        File.WriteAllText(Path.Combine(settingsPath, settingsFileName), serializedSettings)

    let loadSettings () =
        let settingsFilePath = Path.Combine(settingsPath, settingsFileName)

        if File.Exists settingsFilePath
        then
            try
                let serializedSettings = File.ReadAllText(settingsFilePath)
                deserializeSettings serializedSettings |> Some
            with _ -> None
        else None
