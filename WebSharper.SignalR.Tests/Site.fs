namespace WebSharper.SignalR.Tests

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Templating
open WebSharper.UI.Server
open Microsoft.AspNetCore.SignalR
open System.Threading.Tasks
open SignalRClient

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/about">] About
    | [<EndPoint "/chathub">] Chat
    | [<EndPoint "/chathub/negotiate"; Query "negotiateVersion">] Negotiate of negotiateVersion: int

module Templating =
    open WebSharper.UI.Html

    type MainTemplate = Templating.Template<"Main.html">

    // Compute a menubar where the menu item for the given endpoint is active
    let MenuBar (ctx: Context<EndPoint>) endpoint : Doc list =
        let ( => ) txt act =
             li [if endpoint = act then yield attr.``class`` "active"] [
                a [attr.href (ctx.Link act)] [text txt]
             ]
        [
            "Home" => EndPoint.Home
            "About" => EndPoint.About
            "SignalR Tests" => EndPoint.Chat
        ]

    let Main ctx action (title: string) (body: Doc list) =
        Content.Page(
            MainTemplate()
                .Title(title)
                .MenuBar(MenuBar ctx action)
                .Body(body)
                .Doc()
        )

    type TestTemplate = Templating.Template<"testing.html">

    let Testing ctx action (title: string) =
        Content.Page(
            TestTemplate()
                .Title(title)
                .Elt()
                .OnAfterRender(fun _ -> MyHub.conn)
        )

type LetsChat () =
    inherit Hub ()

    member x.Send(user: string) (msg: string) : Task =
        x.Clients.All.SendAsync("ReceiveMessage", user, msg)

module Site =
    open WebSharper.UI.Html

    let HomePage ctx =
        Templating.Main ctx EndPoint.Home "Home" [
            h1 [] [text "Say Hi to the server!"]
            div [] [client <@ Client.Main() @>]
        ]

    let AboutPage ctx =
        Templating.Main ctx EndPoint.About "About" [
            h1 [] [text "About"]
            p [] [text "This is a template WebSharper client-server application."]
        ]

    let TestingPage ctx =
        Templating.Testing ctx EndPoint.Chat "Testing"

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.About -> AboutPage ctx
            | EndPoint.Chat -> TestingPage ctx
            | EndPoint.Negotiate n -> Content.Ok
        )
