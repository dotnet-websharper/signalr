namespace SignalRClient

open WebSharper.SignalR
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html

[<JavaScript>]
module MyHub =

    // type MyOptions () =
    //     interface SignalR.IHttpConnectionOptions with
    //         member x.EventSource = Optional.Undefined
    //         member x.HttpClient = Optional.Undefined
    //         member x.Logger = Optional.Undefined
    //         member x.LogMessageContent = Optional.Undefined
    //         member x.SkipNegotiation = true |> Optional.Defined
    //         member x.Transport = SignalR.HttpTransportType.WebSockets :> obj |> Optional.Defined
    //         member x.WebSocket = Optional.Undefined
    //         member x.AccessTokenFactory () = Union2Of2("token")

    let options = SignalR.IHttpConnectionOptions()

    let connection : SignalR.HubConnection<string> =
        SignalR.HubConnectionBuilder()
            .WithUrl("http://localhost:5000/chathub", options)
            .ConfigureLogging(SignalR.LogLevel.Trace)
            .Build()
    
    let conn = 
        do 
            let btn = JS.Document.GetElementById("btnSendMessage")
            btn.SetAttribute("disabled", "")

            let createLi user (msg:string) =
                let message = msg.Replace("/&/g", "&amp;").Replace("/</g", "&lt;").Replace("/>/g", "&gt;")
                let encodedMsg = user + " : " + message
                let li = JS.Document.CreateElement("li")
                li.TextContent <- encodedMsg
                li

            connection.On(
                "ReceiveMessage",
                fun arr ->
                    let user = arr.[0] :?> string
                    let msg = arr.[1] :?> string
                    let newLi = createLi user msg
                    JS.Document.GetElementById("messagesList").AppendChild(newLi) |> ignore
            )

            connection.Start().Then(
                fun () ->
                    let btn = JS.Document.GetElementById("btnSendMessage")
                    btn.SetAttribute("disabled", "not")
            ).Catch(
                fun _ -> ()
            )
            |> ignore

            JS.Document.GetElementById("btnSendMessage").AddEventListener(
                "click",
                fun (ev: Dom.Event) ->
                    let user = JS.Document.GetElementById("userName").NodeValue
                    let message = JS.Document.GetElementById("userName").NodeValue
                    connection.Invoke("SendMessage", [|user :> obj; message :> obj|])
                        .Catch(fun _ -> ())
                    |> ignore
                    ev.PreventDefault()
            )