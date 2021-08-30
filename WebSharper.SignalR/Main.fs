namespace WebSharper.SignalR

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =

    let ErrorCtor = !? T<string> * !? T<obj> * !? T<string> * !? T<int> ^-> T<Error>

    // let EventSourceCtor = T<string> * T<obj> ^-> T<EventSource>

    let WebSocketCtor = T<string> * !? (T<string> + !| T<string>) ^-> T<WebSocket>

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
        |+> Static [
            Constructor T<unit>
        ]
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
            Constructor ILogger?logger
        ]

    let AbortError =
        Class "AbortError"
        |+> Static [
            Constructor T<string>?errorMessage

            "ErrorConstructor" =? ErrorCtor
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

            "ErrorConstructor" =? ErrorCtor
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

            "ErrorConstructor" =? ErrorCtor
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

    let HttpTransportType =
        Pattern.EnumInlines "HttpTransportType" [
            "None", "0"
            "WebSockets", "1"
            "ServerSentEvents", "2"
            "LongPolling", "4"
        ]

    let TransferFormat =
        Pattern.EnumInlines "TransferFormat" [
            "Text", "1"
            "Binary", "2"
        ]

    let ITransport =
        Interface "ITransport"
        |++> [
            "onclose" =? T<Error>?error ^-> T<unit>
            "onreceive" =? (T<string> + T<ArrayBuffer>)?data ^-> T<unit>
            "connect" => T<string>?url * TransferFormat?transferFormat ^-> T<Promise<unit>> 
            "send" => T<obj>?data ^-> T<Promise<unit>>
            "stop" => T<unit> ^-> T<Promise<unit>>
        ]

    let IHttpConnectionOptions =
        Interface "IHttpConnectionOptions"
        |++> [
            // "EventSource" =? !? EventSourceCtor
            "httpClient" =? !? HttpClient
            "logger" =? !? ILogger + LogLevel
            "logMessageContent" =? !? T<bool>
            "skipNegotiation" =? !? T<bool>
            "transport" =? !? HttpTransportType + ITransport
            "WebSocket" =? !? WebSocketCtor
            "accessTokenFactory" => T<unit> ^-> (T<string> + T<Promise<string>>)
        ]

    let MessageHeaders =
        Interface "MessageHeaders"

    let MessageType =
        Pattern.EnumInlines "MessageType" [
            "Invocation", "1"
            "StreamItem", "2"
            "Completion", "3"
            "StreamInvocation", "4"
            "CancelInvocation", "5"
            "Ping", "6"
            "Close", "7"
        ]

    let HubMessageBase =
        Interface "HubMessageBase"
        |++> [
            "type" =? MessageType
        ]

    let HubInvocationMessage =
        Interface "HubInvocationMessage"
        |=> Extends [HubMessageBase]
        |++> [
            "headers" =? !? MessageHeaders
            "invocationId" =? !? T<string>
        ]

    let CancelInvocationMessage =
        Interface "CancelInvocationMessage"
        |=> Extends [HubInvocationMessage]

    let CloseMessage =
        Interface "CloseMessage"
        |=> Extends [HubMessageBase]
        |++> [
            "allowReconnect" =? !? T<bool>
            "error" =? !? T<string>
        ]

    let CompletionMessage =
        Interface "CompletionMessage"
        |=> Extends [HubInvocationMessage]
        |++> [
            "error" =? !? T<string>
            "result" =? !? T<obj>
        ]

    let InvocationMessage =
        Interface "InvocationMessage"
        |=> Extends [HubInvocationMessage]
        |++> [
            "arguments" =? !| T<obj>
            "streamIds" =? !| T<string>
            "target" =? T<string>
        ]

    let PingMessage =
        Interface "PingMessage"
        |=> Extends [HubMessageBase]

    let StreamInvocationMessage =
        Interface "StreamInvocationMessage"
        |=> Extends [HubInvocationMessage]
        |++> [
            "arguments" =? !| T<obj>
            "streamIds" =? !| T<string>
            "target" =? T<string>
        ]

    let StreamItemMessage =
        Interface "StreamItemMessage"
        |=> Extends [HubInvocationMessage]
        |++> [
            "item" =? !? T<obj>
        ]

    let HubMessage =
        CancelInvocationMessage +
        CloseMessage +
        CompletionMessage +
        InvocationMessage +
        PingMessage +
        StreamInvocationMessage +
        StreamItemMessage

    let IHubProtocol =
        Interface "IHubProtocol"
        |++> [
            "name" =? T<string>
            "transferFormat" =? TransferFormat
            "version" =? T<int>
            "parseMessages" => T<int>?input * ILogger?logger ^-> !| HubMessage
            "parseMessages" => T<ArrayBuffer>?input * ILogger?logger ^-> !| HubMessage
            // "parseMessages" => T<Buffer>?input ^-> !| HubMessage
            "writeMessage" => HubMessage?message ^-> (T<string> + T<ArrayBuffer>)
        ]

    let RetryContext =
        Interface "RetryContext"
        |++> [
            "elapsedMilliseconds" =? T<int>
            "previousRetryCount" =? T<int>
            "retryReason" =? T<Error>
        ]

    let IRetryPolicy =
        Interface "IRetryPolicy"
        |++> [
            "nextRetryDelayInMilliseconds" => RetryContext?retryContext ^-> T<int>
        ]

    let HubConnectionBuilder =
        Class "HubConnectionBuilder"
        |+> Static [
            Constructor T<unit>
        ]
        |+> Instance [
            "httpConnectionOptions" =? !? IHttpConnectionOptions
            "logger" =? !? ILogger
            "protocol" =? !? IHubProtocol
            "reconnectPolicy" =? !? IRetryPolicy
            "url" =? !? T<string>
            "build" => T<unit> ^-> HubConnection
            "configureLogging" => ILogger?logger ^-> TSelf
            "configureLogging" => T<string>?logLevel ^-> TSelf
            "configureLogging" => LogLevel?logLevel ^-> TSelf
            "withAutomaticReconnect" => T<unit> ^-> TSelf
            "withAutomaticReconnect" => IRetryPolicy?reconnectPolicy ^-> TSelf
            "withAutomaticReconnect" => (!| T<int>)?retryDelays ^-> TSelf
            "withHubProtocol" => IHubProtocol?protocol ^-> TSelf
            "withUrl" => T<string>?url ^-> TSelf
            "withUrl" => T<string>?url * HttpTransportType?transportType ^-> TSelf
            "withUrl" => T<string>?url * IHttpConnectionOptions?options ^-> TSelf
        ]

    let JsonHubProtocol =
        Class "JsonHubProtocol"
        |+> Static [
            Constructor T<unit>
        ]
        |+> Instance [
            "name" =? T<string>
            "tranferFormat" =? TransferFormat
            "version" =? T<int>
            "parseMessages" => T<string>?input * ILogger?logger ^-> !| HubMessage
            "writeMessage" => HubMessage?message ^-> T<string>
        ]

    let NullLogger =
        Class "NullLogger"
        |+> Static [
            Constructor T<unit>

            "instance" =? TSelf
        ]
        |+> Instance [
            "log" => LogLevel?_logLevel * T<string>?_message ^-> T<unit>
        ]

    let IStreamSubscriber =
        //Generic - fun t ->
        Interface "IStreamSubscriber"
        |++> [
            "closed" =? !? T<bool>
            "complete" => T<unit> ^-> T<unit>
            "error" => T<obj>?err ^-> T<unit>
            // "next" => T<'T> ^-> TSelf
            "next" => T<obj> ^-> TSelf
        ]

    let ISubscription =
        Interface "ISubscription"
        |++> [
            "dispose" => T<unit> ^-> T<unit>
        ]

    let Subject =
        Class "Subject"
        |=> Implements [IStreamSubscriber]
        |+> Static [
            Constructor T<unit>
        ]
        |+> Instance [
            "cancelCallback" =? !? T<unit -> Promise<unit>>
            "observers" =? !| IStreamSubscriber
            "subscribe" => IStreamSubscriber?observer ^-> ISubscription
        ]

    let XhrHttpClient =
        Class "XhrHttpClient"
        |=> Inherits HttpClient
        |+> Static [
            Constructor ILogger?logger
        ]

    let IStreamResult =
        Interface "IStreamResult"
        |++> [
            "subscribe" => IStreamSubscriber?subscriber ^-> ISubscription
        ]

    let Assembly =
        Assembly [
            Namespace "WebSharper.SignalR.Resources" [
                Resource "SignalRCDN" "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/5.0.9/signalr.min.js"
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
                HttpTransportType
                TransferFormat
                ITransport
                IHttpConnectionOptions
                MessageHeaders
                MessageType
                HubMessageBase
                HubInvocationMessage
                CancelInvocationMessage
                CloseMessage
                CompletionMessage
                InvocationMessage
                PingMessage
                StreamInvocationMessage
                StreamItemMessage
                IHubProtocol
                RetryContext
                IRetryPolicy
                HubConnectionBuilder
                JsonHubProtocol
                NullLogger
                IStreamSubscriber
                ISubscription
                Subject
                XhrHttpClient
                IStreamResult
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
