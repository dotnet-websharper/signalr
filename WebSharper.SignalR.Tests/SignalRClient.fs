namespace SignalRClient

open WebSharper.SignalR
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html

[<JavaScript>]
module Tests =
    type MyLogger () =
        interface ILogger with
            override x.Log(logLevel, msg) = "some log"

    let myLogger = MyLogger()

    let defaultClient = DefaultHttpClient(myLogger)

    let getHome = defaultClient.Get("http://localhost5000/")
    let getAbout = defaultClient.Get("http://localhost5000/about")

    let result1 =
        getHome.Then((fun resp -> "success"), (fun _ -> "rejected"))

    let result2 =
        getAbout.Then((fun resp -> "success"), (fun _ -> "rejected"))

    let tests = [
        "GET Home"
        "GET About"
    ]

    let testResults = [
        result1
        result2
    ]

    // let testView () =
    //     let zippedTests = List.zip tests testResults
    //     zippedTests
    //     |> List.map (fun i ->
    //         let r =
    //             snd i
    //             // |> Async.RunSynchronously
    //         let t = sprintf "Test: %s, result: %s" (fst i) r
    //         div [] [text t])

// [<JavaScript>]
// module Hub =

//     let connection =
//         HubConnectionBuilder()
//             .WithUrl("http://localhost/5000/signalr")
//             .ConfigureLogging(LogLevel.Information)
//             .Build()
    
//     let Start () =
//         async {
//             try
//                 do! connection.Start().AsAsync()
//             with
//             | _ -> 
//         }