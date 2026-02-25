namespace TeaDriven.Maysternya

type LogLevel =
    Diagnostic | Informational | Warning | Error
    with
        member this.Priority =
            match this with
            | Diagnostic -> 1
            | Informational -> 2
            | Warning -> 3
            | Error -> 4

module LoggingTypes =
    open System
    open DynamicData
    open ReactiveElmish

    type LogEntry<'Activity> =
        {
            Timestamp: DateTimeOffset
            LogLevel: LogLevel
            Activity: 'Activity
            Message: string
        } with
            static member create logLevel activity message =
                {
                    Timestamp = DateTimeOffset.Now
                    LogLevel = logLevel
                    Activity = activity
                    Message = message
                }

            member this.addTo (entries: SourceCache<_, _>) =
                entries |> SourceCache.addOrUpdate this

    type ILogTarget<'Activity, 'Key, 'Model> =
        abstract member LogEntries: SourceCache<LogEntry<'Activity>, 'Key> with get

        abstract member UpdateLogEntries<'Activity, 'Key, 'Model>: SourceCache<LogEntry<'Activity>, 'Key> -> 'Model

    type LogEntryViewModel<'Activity>(logEntry: LogEntry<'Activity>) =
        inherit ReactiveElmishViewModel()

        member _.Timestamp = logEntry.Timestamp
        member _.LogLevel = logEntry.LogLevel
        member _.Activity = logEntry.Activity
        member _.Message = logEntry.Message

[<RequireQualifiedAccess>]
module Logging =
    open ReactiveElmish

    open LoggingTypes

    let withLog logLevel activity message (logTarget: ILogTarget<_, _, _>) =
        let logEntry = LogEntry<_>.create logLevel activity message

        logTarget.LogEntries
        |> SourceCache.addOrUpdate logEntry
        |> logTarget.UpdateLogEntries
