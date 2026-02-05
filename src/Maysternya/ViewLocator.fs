namespace TeaDriven.Maysternya

open System

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Templates

open ReactiveElmish
open ReactiveElmish.Avalonia

// From ReactiveElmish Avalonia example; no functional changes
type ViewLocator() =
    let notFoundTextBlock name = TextBlock(Text = $"Not Found: %s{name}")

    interface IDataTemplate with
        member this.Build(data) =
            match data with
            | :? ReactiveElmishViewModel as reactiveViewModel ->
                AppCompositionRoot.Instance.GetViewFor(reactiveViewModel)
            | _ ->
                let t = data.GetType()
                let viewName = t.FullName.Replace("ViewModels", "Views").Replace("ViewModel", "View")
                let parts = viewName.Split([|'['; '+'|], StringSplitOptions.RemoveEmptyEntries)
                let name =
                    if parts.Length > 2
                    then parts[1]
                    else parts[0]
                let viewType = Type.GetType(name)

                if isNull viewType
                then
                    notFoundTextBlock name
                else
                    let view = downcast Activator.CreateInstance(viewType)
                    match data with
                    | :? ReactiveUI.ReactiveObject as vm ->
                        ViewBinder.bindWithDisposeOnViewUnload (vm, view) |> snd
                    | _ ->
                        notFoundTextBlock name

        member this.Match(data) =
            match data with
            | :? ReactiveElmishViewModel as vm -> AppCompositionRoot.Instance.HasViewFor(vm)
            | :? ReactiveUI.ReactiveObject -> true
            | _ -> false
