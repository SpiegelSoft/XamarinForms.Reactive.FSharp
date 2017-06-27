namespace XamarinForms.Reactive.Sample.Mars.Common

open System

type IAppendLog =
    abstract member Debug : tag:string -> message:string -> ex:Exception option -> unit
    abstract member Information : tag:string -> message:string -> ex:Exception option -> unit
    abstract member Warning : tag:string -> message:string -> ex:Exception option -> unit
    abstract member Error : tag:string -> message:string -> ex:Exception option -> unit
    abstract member Critical : tag:string -> message:string -> ex:Exception option -> unit

type ILog =
    abstract member Debug : tag:string -> message:string -> ex:Exception option -> unit
    abstract member Information : tag:string -> message:string -> ex:Exception option -> unit
    abstract member Warning : tag:string -> message:string -> ex:Exception option -> unit
    abstract member Error : tag:string -> message:string -> ex:Exception option -> unit
    abstract member Critical : tag:string -> message:string -> ex:Exception option -> unit
    abstract member AddAppender: IAppendLog -> unit

type Logger(logAppenders: IAppendLog seq) =
    let mutable appenders = logAppenders |> List.ofSeq
    interface ILog with
        member __.AddAppender appender = appenders <- appender :: appenders
        member __.Debug tag message ex = for appender in appenders do appender.Debug tag message ex
        member __.Information tag message ex = for appender in appenders do appender.Information tag message ex
        member __.Warning tag message ex = for appender in appenders do appender.Warning tag message ex
        member __.Error tag message ex = for appender in appenders do appender.Error tag message ex
        member __.Critical tag message ex = for appender in appenders do appender.Critical tag message ex

module SafeReactiveCommands =
    open System.Threading.Tasks
    open System.Reactive.Linq
    open ReactiveUI
    open Splat

    let [<Literal>] private Tag = "SafeReactiveCommands"
    let create (factory:IObservable<bool> -> ReactiveCommand<'src, 'dest>) (canExecute) =
        let logger = Locator.Current.GetService<ILog>()
        let ce = match canExecute with | Some c -> c | None -> Observable.Return<bool>(true)
        let command = factory(ce)
        let logException ex =  Some ex |> logger.Critical Tag "Command Error" 
        command.ThrownExceptions.Subscribe logException |> ignore
        command
    let createFromTask(task: 'src -> Task<'dest>, canExecute) = 
        let factory ce = ReactiveCommand.CreateFromTask<'src, 'dest>(task, ce)
        create factory canExecute
    let createFromObservable(observable: 'src -> IObservable<'dest>, canExecute: IObservable<bool> option) =
        let factory ce = ReactiveCommand.CreateFromObservable<'src, 'dest>((fun s -> observable s), ce)
        create factory canExecute


