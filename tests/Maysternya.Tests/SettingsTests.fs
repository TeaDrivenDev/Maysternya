namespace TeaDriven.Maysternya.Tests

module SettingsTests =
    open Xunit
    
    open TeaDriven.Maysternya
    open TeaDriven.Maysternya.Domain
    
    [<Fact>]
    let ``Settings can be serialized and deserialized``() =
        // Arrange
        let settings =
            {
                SteamPath = "xSteamx"
                DefaultGame = NoGame 
            }
        
        // Act
        let serialized = Settings.serializeSettings settings
        let deserialized = Settings.deserializeSettings<Settings> serialized
        
        // Assert
        Assert.Equal(deserialized, settings)
