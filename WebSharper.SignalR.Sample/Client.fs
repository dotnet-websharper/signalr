namespace WebSharper.SignalR.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open WebSharper.UI.Notation
open WebSharper.UI.Templating
open WebSharper.SignalR

[<JavaScript>]
module Client =

    // The template is the whole document, loaded from the DOM, so editing
    // index.html and refreshing the browser is enough.
    type IndexTemplate = Template<"index.html", ClientLoad.FromDocument>

    /// Promise rejections carry a plain `obj`; pull the message out of it.
    [<Inline "$0 && ($0.message || ('' + $0))">]
    let private errorText (e: obj) : string = X<string>

    // ---------------------------------------------------------------
    // View state
    // ---------------------------------------------------------------

    let notice = Var.Create ""
    let state = Var.Create "Disconnected"
    let connectionId = Var.Create "-"
    let baseUrl = Var.Create "-"
    let serverTimeout = Var.Create "-"
    let keepAlive = Var.Create "-"
    let streamStatus = Var.Create "No stream running."
    let messages = Var.Create ([] : string list)
    let events = Var.Create ([] : (string * string) list)
    let streamItems = Var.Create ([] : string list)

    let hubUrl = Var.Create "/chathub"
    let transport = Var.Create "None"
    let logLevel = Var.Create "Information"
    let userName = Var.Create "WebSharper"
    let messageText = Var.Create "Hello from WebSharper.SignalR!"

    // Theme: light is the default; the choice is persisted in localStorage.
    // The storage key is shared with the bootstrap script in index.html.
    let theme = Var.Create "light"
    let themeLabel = Var.Create "🌙 Dark mode"
    let themeStorageKey = "websharper-signalr-theme"

    let mutable conn : SignalR.HubConnection<string> = null
    let mutable subscription : SignalR.ISubscription = null
    let mutable subject : SignalR.Subject<string> = null

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    let applyTheme () =
        JS.Document.DocumentElement.SetAttribute("data-theme", theme.Value)
        themeLabel.Value <-
            if theme.Value = "dark" then "☀️ Light mode" else "🌙 Dark mode"

    let toggleTheme () =
        theme.Value <- (if theme.Value = "dark" then "light" else "dark")
        try JS.Window.LocalStorage.SetItem(themeStorageKey, theme.Value) with _ -> ()
        applyTheme ()

    let stamp () = System.DateTime.Now.ToLongTimeString()

    /// Appends a line to the connection-events log (capped at 100 lines).
    let addEvent (cls: string) (msg: string) =
        let line = sprintf "%s  %s" (stamp ()) msg
        let next = events.Value @ List.singleton (cls, line)
        events := if next.Length > 100 then List.skip (next.Length - 100) next else next

    let refresh () =
        if isNull conn then
            state := "Disconnected"
            connectionId := "-"
            baseUrl := "-"
            serverTimeout := "-"
            keepAlive := "-"
        else
            state := As<string> conn.State
            let cid = conn.ConnectionId
            connectionId := (if isNull cid then "-" else cid)
            baseUrl := conn.BaseUrl
            serverTimeout := string conn.ServerTimeoutInMilliseconds
            keepAlive := string conn.KeepAliveIntervalInMilliseconds

    let chosenLogLevel =
        match logLevel.Value with
        | "Trace" -> SignalR.LogLevel.Trace
        | "Debug" -> SignalR.LogLevel.Debug
        | "Information" -> SignalR.LogLevel.Information
        | "Warning" -> SignalR.LogLevel.Warning
        | "Error" -> SignalR.LogLevel.Error
        | "Critical" -> SignalR.LogLevel.Critical
        | _ -> SignalR.LogLevel.None

    /// The handler is a module-level value so `off` can remove the exact same
    /// function instance that `on` registered.
    let receiveMessage (args: obj[]) =
        let user = args.[0] :?> string
        let msg = args.[1] :?> string
        messages := messages.Value @ [ sprintf "%s: %s" user msg ]

    let receiveUpload (item: string) =
        streamItems := streamItems.Value @ [ sprintf "(upload) %s" item ]

    // ---------------------------------------------------------------
    // Connection
    // ---------------------------------------------------------------

    let disposeConnection () =
        if not (isNull subscription) then
            subscription.Dispose()
            subscription <- null
        if not (isNull subject) then
            subject.Complete()
            subject <- null
        if not (isNull conn) then
            conn <- null
        refresh ()

    let connect () =
        disposeConnection ()
        let url = hubUrl.Value
        let builder : SignalR.HubConnectionBuilder<string> = SignalR.HubConnectionBuilder()

        // IHttpConnectionOptions carries the request-level settings; the
        // remaining options go through the dedicated builder methods.
        let options = SignalR.IHttpConnectionOptions()
        options.LogMessageContent <- true
        options.Timeout <- 100000
        options.WithCredentials <- true

        let configured =
            match transport.Value with
            | "WebSockets" ->
                builder.WithUrl(url, SignalR.HttpTransportType.WebSockets)
            | "ServerSentEvents" ->
                builder.WithUrl(url, SignalR.HttpTransportType.ServerSentEvents)
            | "LongPolling" ->
                builder.WithUrl(url, SignalR.HttpTransportType.LongPolling)
            | _ ->
                builder.WithUrl(url, options)

        let connection =
            configured
                .ConfigureLogging(chosenLogLevel)
                .WithHubProtocol(SignalR.JsonHubProtocol())
                .WithServerTimeout(30000)
                .WithKeepAliveInterval(15000)
                .WithAutomaticReconnect()
                .Build()

        connection.On("ReceiveMessage", receiveMessage)
        connection.On("ReceiveUpload", fun args -> receiveUpload (args.[0] :?> string))
        connection.Onclose(fun (error: Error) ->
            addEvent "ev-error" (if isNull error then "onclose" else "onclose: " + error.Message)
            refresh ())
        connection.Onreconnecting(fun (error: Error) ->
            addEvent "ev-error" (if isNull error then "onreconnecting" else "onreconnecting: " + error.Message)
            refresh ())
        connection.Onreconnected(fun (id: string) ->
            addEvent "ev-ok" "onreconnected"
            refresh ())

        conn <- connection
        notice := ""
        addEvent "ev-ok" (sprintf "connecting to %s (%s)" url transport.Value)

        connection.Start()
            .Then(fun () ->
                addEvent "ev-ok" "connected"
                notice := ""
                refresh ())
            .Catch(fun error ->
                let text = errorText error
                addEvent "ev-error" ("start failed: " + text)
                notice :=
                    sprintf "Could not connect to %s (%s). Start WebSharper.SignalR.Server to chat." url text
                refresh ())
        |> ignore

    let disconnect () =
        addEvent "ev-ok" "stopping"
        conn.Stop()
            .Then(fun () ->
                addEvent "ev-ok" "disconnected"
                disposeConnection ())
            .Catch(fun error ->
                addEvent "ev-error" ("stop failed: " + errorText error)
                disposeConnection ())
        |> ignore

    // ---------------------------------------------------------------
    // Chat
    // ---------------------------------------------------------------

    let requireConnection () =
        if isNull conn then
            notice := "Connect first."
            false
        else
            true

    let sendViaInvoke () =
        if requireConnection () then
            conn.Invoke("SendMessage", [| userName.Value :> obj; messageText.Value :> obj |])
                .Then(fun result ->
                    if not (isNull result) && result <> "" then
                        addEvent "ev-ok" ("invoke resolved: " + result)
                    refresh ())
                .Catch(fun error ->
                    addEvent "ev-error" ("invoke failed: " + errorText error)
                    refresh ())
            |> ignore

    let sendViaSend () =
        if requireConnection () then
            conn.Send("SendMessage", [| userName.Value :> obj; messageText.Value :> obj |])
                .Catch(fun error ->
                    addEvent "ev-error" ("send failed: " + errorText error))
            |> ignore

    let offHandler () =
        if requireConnection () then
            conn.Off("ReceiveMessage", receiveMessage)
            addEvent "ev-ok" "off(\"ReceiveMessage\")"

    // ---------------------------------------------------------------
    // Streaming
    // ---------------------------------------------------------------

    let startStream () =
        if requireConnection () then
            if not (isNull subscription) then
                subscription.Dispose()
                subscription <- null
            let count = 5
            let results = conn.Stream("Counter", [| count :> obj |])
            streamItems := []
            streamStatus := sprintf "Subscribed to Counter(%d)." count
            let subscriber =
                { new SignalR.IStreamSubscriber<string> with
                    member _.Closed = Optional.Undefined
                    member _.Next(value) = streamItems := streamItems.Value @ [ value ]
                    member _.Error(err) = streamStatus := "stream error: " + errorText err
                    member _.Complete() = streamStatus := "stream completed."
                }
            subscription <- results.Subscribe(subscriber)
            addEvent "ev-ok" "stream(\"Counter\") subscribed"

    let stopStream () =
        if not (isNull subscription) then
            subscription.Dispose()
            subscription <- null
            streamStatus := "Subscription disposed."
            addEvent "ev-ok" "stream subscription disposed"

    let uploadSubject () =
        if requireConnection () then
            let s = SignalR.Subject<string>()
            subject <- s
            addEvent "ev-ok" "send(\"Upload\", subject)"
            conn.Send("Upload", [| s :> obj |])
                .Then(fun () ->
                    s.Next("alpha")
                    s.Next("beta")
                    s.Next("gamma")
                    s.Complete()
                    addEvent "ev-ok" "subject completed")
                .Catch(fun error ->
                    addEvent "ev-error" ("upload failed: " + errorText error))
            |> ignore

    // ---------------------------------------------------------------
    // Entry point
    // ---------------------------------------------------------------

    [<SPAEntryPoint>]
    let Main () =
        hubUrl.Value <- JS.Window.Location.Origin + "/chathub"
        try
            let stored = JS.Window.LocalStorage.GetItem(themeStorageKey)
            if stored ===. "dark" || stored ===. "light" then theme.Value <- stored
        with _ -> ()
        applyTheme ()

        IndexTemplate()
            .notice(notice.V)
            .themeLabel(themeLabel.V)
            .state(state.V)
            .connectionId(connectionId.V)
            .baseUrl(baseUrl.V)
            .serverTimeout(serverTimeout.V)
            .keepAlive(keepAlive.V)
            .streamStatus(streamStatus.V)
            .HubUrl(hubUrl)
            .Transport(transport)
            .LogLevel(logLevel)
            .UserName(userName)
            .MessageText(messageText)
            .Messages(Doc.BindSeqCached (fun (s: string) -> Doc.Element "li" [] [ Doc.TextNode s ]) messages.View)
            .StreamItems(Doc.BindSeqCached (fun (s: string) -> Doc.Element "li" [] [ Doc.TextNode s ]) streamItems.View)
            .Events(Doc.BindSeqCached (fun (cls: string, text: string) ->
                Doc.Element "li" [attr.``class`` cls] [ Doc.TextNode text ]) events.View)
            .ToggleTheme(fun _ -> toggleTheme ())
            .Connect(fun _ -> connect ())
            .Disconnect(fun _ -> disconnect ())
            .Refresh(fun _ -> refresh ())
            .SendInvoke(fun _ -> sendViaInvoke ())
            .SendRaw(fun _ -> sendViaSend ())
            .OffHandler(fun _ -> offHandler ())
            .StartStream(fun _ -> startStream ())
            .StopStream(fun _ -> stopStream ())
            .UploadSubject(fun _ -> uploadSubject ())
            .ClearEvents(fun _ -> events := [])
            .Bind()
