module WebSharper.SignalR.Server.Program

open System.Collections.Generic
open System.IO
open System.Threading.Channels
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders

/// A small chat hub that the WebSharper.SignalR.Sample SPA connects to.
type ChatHub() =
    inherit Hub()

    /// Broadcasts a message to every connected client.
    member this.SendMessage(user: string, message: string) : Task =
        this.Clients.All.SendAsync("ReceiveMessage", user, message)

    /// Server-to-client stream: the client subscribes with `stream("Counter", n)`.
    member this.Counter(count: int) : IAsyncEnumerable<string> =
        let channel = Channel.CreateUnbounded<string>()
        task {
            for i in 1 .. count do
                do! Task.Delay 400
                do! channel.Writer.WriteAsync(sprintf "tick %d / %d" i count)
            channel.Writer.Complete()
        }
        |> ignore
        channel.Reader.ReadAllAsync()

    /// Client-to-server stream: the client sends a `Subject` and pushes items.
    member this.Upload(stream: IAsyncEnumerable<string>) : Task =
        task {
            let e = stream.GetAsyncEnumerator()
            let mutable go = true
            while go do
                let! moved = e.MoveNextAsync().AsTask()
                if moved then
                    do! this.Clients.Caller.SendAsync("ReceiveUpload", e.Current)
                else
                    go <- false
        }

[<EntryPoint>]
let main _ =
    let builder = WebApplication.CreateBuilder()
    builder.Services.AddSignalR() |> ignore

    let app = builder.Build()

    // Serve the SPA produced by WebSharper.SignalR.Sample from the same origin,
    // so the hub and the page share http://localhost:5149. The sample bundles
    // into the repository-root `dist/` folder (see esbuild.config.mjs).
    let dist =
        Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "dist"))

    if Directory.Exists dist then
        let files = new PhysicalFileProvider(dist)
        app.UseDefaultFiles(DefaultFilesOptions(FileProvider = files)) |> ignore
        app.UseStaticFiles(StaticFileOptions(FileProvider = files)) |> ignore
    else
        eprintfn "Sample output not found at %s — build WebSharper.SignalR.Sample first." dist

    app.MapHub<ChatHub>("/chathub") |> ignore

    // Deterministic, non-5000 port (see the workspace conventions).
    app.Run("http://localhost:5149")
    0
