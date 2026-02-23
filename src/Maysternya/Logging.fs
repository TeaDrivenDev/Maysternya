namespace TeaDriven.Maysternya.Logging

open System
open DynamicData
open ReactiveElmish

type LogLevel = Diagnostic | Informational | Warning | Error

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

type LogEntryViewModel<'Activity>(logEntry: LogEntry<'Activity>) =
    inherit ReactiveElmishViewModel()

    member _.Timestamp = logEntry.Timestamp
    member _.LogLevel = logEntry.LogLevel
    member _.Activity = logEntry.Activity
    member _.Message = logEntry.Message
