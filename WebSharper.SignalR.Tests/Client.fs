namespace WebSharper.SignalR.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.SignalR

[<JavaScript>]
module Client =
    type IndexTemplate = Template<"index.html", ClientLoad.FromDocument>

    type MyLogger (l: LogLevel, msg: string) =
        interface ILogger with
            member x.Log(l, msg) =
                "asd"

    let myLogger = MyLogger(LogLevel.Debug, "debug")

    let client = DefaultHttpClient(myLogger)

    client.Delete("") |> ignore

    [<SPAEntryPoint>]
    let Main () =

        IndexTemplate.Main()
            .Doc()
        |> Doc.RunById "main"
