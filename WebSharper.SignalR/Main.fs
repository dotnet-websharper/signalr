namespace WebSharper.SignalR

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =

    let AbortSignal =
        Interface "AbortSignal"
        |++> [
            "abortSignal" =? T<bool>
            "onabort" => T<unit> ^-> T<unit>
        ]

    let HttpRequest =
        Interface "HttpRequest"
        |++> [
            "abortSignal" =? !? AbortSignal
            "content" =? !? T<string> + T<ArrayBuffer>
            "headers" =? !? T<Object<string>>
            "method" =? !? T<string>
            "responseType" =? !? T<XMLHttpRequestResponseType>
            "timeout" =? !? T<int> // num
            "url" =? !? T<string>
        ]

    let LogLevel =
        Pattern.EnumInlines "LogLevel" [
            "Trace", "0"
            "Debug", "1"
            "Information", "2"
            "Warning", "3"
            "Error", "4"
            "Critical", "5"
            "None", "6"
        ]

    let ILogger =
        Interface "ILogger"
        |++> [
            "log" => LogLevel?logLevel * T<string>?message ^-> T<string>
        ]

    let HttpResponse =
        Class "HttpResponse"
        |+> Static [
            Constructor T<int>?statusCode
            Constructor (T<int>?statusCode * T<string>?statusText)
            Constructor (T<int>?statusCode * T<string>?statusText * T<ArrayBuffer>?content)
            Constructor (T<int>?statusCode * T<string>?statusText * T<string>?content)
        ]
        |+> Instance [
            "content" =? !? (T<ArrayBuffer> + T<string>)
            "statusCode" =? T<int>
            "statusText" =? !? T<string>
        ]

    let HttpClient =
        Class "HttpClient"
        |+> Instance [
            "delete" => T<string>?url * !? HttpRequest?options ^-> HttpResponse // T<Promise<HttpResponse>>
            |> WithComment "Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "get" => T<string>?url * !? HttpRequest?options ^-> HttpResponse // T<Promise<HttpResponse>>
            |> WithComment "Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "getCookieString" => T<string> ^-> T<string>
            |> WithComment "Gets the cookiestring"
            "post" => T<string>?url * !? HttpRequest?options ^-> HttpResponse // T<Promise<HttpResponse>>
            |> WithComment "Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "send" => HttpRequest ^-> !? HttpResponse // T<Promise<HttpResponse>>
            |>WithComment "Send a request, returning a Promise that resolves with an HttpResponse representing the result."
        ]

    let DefaultHttpClient =
        Class "DefaultHttpClient"
        |=> Inherits HttpClient
        |+> Static [
            Constructor ILogger?logger // ILogger
        ]

    let AbortError =
        Class "AbortError"
        |+> Static [
            Constructor T<string>?errorMessage

            "ErrorConstructor" =? T<Error> // ErrorConstructor
        ]
        |+> Instance [
            "message" =? T<string>
            "name" =? T<string>
            "stack" =? !? T<string>
        ]

    let HttpError =
        Class "HttpError"
        |+> Static [
            Constructor (T<string>?errorMessage * T<int>?statusCode)

            "ErrorConstructor" =? T<Error> // ErrorConstructor
        ]
        |+> Instance [
            "message" =? T<string>
            "name" =? T<string>
            "stack" =? !? T<string>
            "statusCode" =? T<int>
        ]

    let TimeoutError =
        Class "TimeoutError"
        |+> Static [
            Constructor T<string>?errorMessage

            "ErrorConstructor" =? T<Error> // ErrorConstructor
        ]
        |+> Instance [
            "message" =? T<string>
            "name" =? T<string>
            "stack" =? !? T<string>
        ]

    let HubConnectionState =
        Pattern.EnumStrings "HubConnectionState" [
            "Connected"
            "Connecting"
            "Disconnected"
            "Disconnecting"
            "Reconnecting"
        ]

    let HubConnection =
        Class "HubConnection"
        |+> Instance [
            "baseUrl" =? T<string>
            |> WithComment "Indicates the url of the <xref:HubConnection> to the server. Sets a new url for the HubConnection. Note that the url can only be changed when the connection is in either the Disconnected or Reconnecting states."
            "connectionId" =? T<string>
            |> WithComment "Represents the connection id of the <xref:HubConnection> on the server. The connection id will be null when the connection is either in the disconnected state or if the negotiation step was skipped."
            "keepAliveIntervalInMilliseconds" =? T<int>
            |> WithComment "Default interval at which to ping the server. The default value is 15,000 milliseconds (15 seconds). Allows the server to detect hard disconnects (like when a client unplugs their computer)."
            "serverTimeoutInMilliseconds" =? T<int>
            |> WithComment "The server timeout in milliseconds. If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error. The default timeout value is 30,000 milliseconds (30 seconds)."
            "state" =? HubConnectionState
            |> WithComment "Indicates the state of the <xref:HubConnection> to the server."
        ]

    let Assembly =
        Assembly [
            Namespace "WebSharper.SignalR.Resources" [
                Resource "SignalRCDN" "https://cdn.jsdelivr.net/npm/signalr@2.4.2/jquery.signalR.min.js"
            ]
            Namespace "WebSharper.SignalR" [
                AbortSignal
                HttpRequest
                LogLevel
                ILogger
                HttpResponse
                HttpClient
                DefaultHttpClient
                AbortError
                HttpError
                TimeoutError
                HubConnectionState
                HubConnection
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
