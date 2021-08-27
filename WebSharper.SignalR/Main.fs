namespace WebSharper.SignalR

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =

    let Assembly =
        Assembly [
            Namespace "WebSharper.SignalR.Resources" [
                Resource "SignalRCDN" "https://cdn.jsdelivr.net/npm/signalr@2.4.2/jquery.signalR.min.js"
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
