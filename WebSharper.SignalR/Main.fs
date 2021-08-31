namespace WebSharper.SignalR.Extension

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =

    let ErrorCtor = !? T<string> * !? T<obj> * !? T<string> * !? T<int> ^-> T<Error>

    let EventSourceCtor = T<string> * !? T<EventSourceOptions> ^-> T<EventSource>

    let WebSocketCtor = T<string> * !? (T<string> + !| T<string>) ^-> T<WebSocket>

    let AbortSignal =
        Interface "AbortSignal"
        |+> [
            "aborted" =? T<bool>
            |> WithComment "Indicates if the request has been aborted."
            "onabort" =@ T<unit> ^-> T<unit>
            |> WithComment "Set this to a handler that will be invoked when the request is aborted."
        ]

    let HttpRequest =
        Interface "HttpRequest"
        |+> [
            "abortSignal" =? !? AbortSignal
            |> WithComment "An AbortSignal that can be monitored for cancellation."
            "content" =? !? T<string> + T<ArrayBuffer>
            |> WithComment "The body content for the request. May be a string or an ArrayBuffer (for binary data)."
            "headers" =? !? T<Object<string>>
            |> WithComment "An object describing headers to apply to the request."
            "method" =? !? T<string>
            |> WithComment "The HTTP method to use for the request."
            "responseType" =? !? T<XMLHttpRequestResponseType>
            |> WithComment "The XMLHttpRequestResponseType to apply to the request."
            "timeout" =? !? T<int>
            |> WithComment "The time to wait for the request to complete before throwing a TimeoutError. Measured in milliseconds."
            "url" =? !? T<string>
            |> WithComment "The URL for the request."
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
        |+> [
            "log" => LogLevel?logLevel * T<string>?message ^-> T<string>
            |> WithComment "Called by the framework to emit a diagnostic message."
        ]

    let HttpResponse =
        Class "signalR.HttpResponse"
        |+> Static [
            Constructor T<int>?statusCode
            |> WithComment "Constructs a new instance of HttpResponse with the specified status code."
            Constructor (T<int>?statusCode * T<string>?statusText)
            |> WithComment "Constructs a new instance of HttpResponse with the specified status code and message."
            Constructor (T<int>?statusCode * T<string>?statusText * T<ArrayBuffer>?content)
            |> WithComment "Constructs a new instance of HttpResponse with the specified status code, message and binary content."
            Constructor (T<int>?statusCode * T<string>?statusText * T<string>?content)
            |> WithComment "Constructs a new instance of HttpResponse with the specified status code, message and string content."
        ]
        |+> Instance [
            "content" =? !? (T<ArrayBuffer> + T<string>)
            |> WithComment "The content of the response"
            "statusCode" =? T<int>
            |> WithComment "The status code of the response."
            "statusText" =? !? T<string>
            |> WithComment "The status message of the response."
        ]

    let HttpClient =
        Class "signalR.HttpClient"
        |+> Static [
            Constructor T<unit>
        ]
        |+> Instance [
            "delete" => T<string>?url * !? HttpRequest?options ^-> T<Promise<_>>.[HttpResponse]
            |> WithComment "Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "get" => T<string>?url * !? HttpRequest?options ^-> T<Promise<_>>.[HttpResponse]
            |> WithComment "Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "getCookieString" => T<string> ^-> T<string>
            |> WithComment "Gets the cookiestring"
            "post" => T<string>?url * !? HttpRequest?options ^-> T<Promise<_>>.[HttpResponse]
            |> WithComment "Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "send" => HttpRequest ^-> !? T<Promise<_>>.[HttpResponse]
            |>WithComment "Send a request, returning a Promise that resolves with an HttpResponse representing the result."
        ]

    let DefaultHttpClient =
        Class "signalR.DefaultHttpClient"
        |=> Inherits HttpClient
        |+> Static [
            Constructor ILogger?logger
            |> WithComment "Creates a new instance of the DefaultHttpClient, using the provided ILogger to log messages."
        ]

    let AbortError =
        Class "signalR.AbortError"
        |=> Inherits T<Error>
        |+> Static [
            Constructor T<string>?errorMessage
            |> WithComment "Constructs a new instance of AbortError."

            "ErrorConstructor" =? ErrorCtor
        ]
        |+> Instance [
            "stack" =? !? T<string>
        ]

    let HttpError =
        Class "signalR.HttpError"
        |=> Inherits T<Error>
        |+> Static [
            Constructor (T<string>?errorMessage * T<int>?statusCode)
            |> WithComment "Constructs a new instance of HttpError."

            "ErrorConstructor" =? ErrorCtor
        ]
        |+> Instance [
            "stack" =? !? T<string>
            "statusCode" =? T<int>
        ]

    let TimeoutError =
        Class "signalR.TimeoutError"
        |=> Inherits T<Error>
        |+> Static [
            Constructor T<string>?errorMessage
            |> WithComment "Constructs a new instance of TimeoutError."

            "ErrorConstructor" =? ErrorCtor
        ]
        |+> Instance [
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

    let RetryContext =
        Interface "RetryContext"
        |+> [
            "elapsedMilliseconds" =? T<int>
            |> WithComment "The amount of time in milliseconds spent retrying so far."
            "previousRetryCount" =? T<int>
            |> WithComment "The number of consecutive failed tries so far."
            "retryReason" =? T<Error>
            |> WithComment "The error that forced the upcoming retry."
        ]

    let IRetryPolicy =
        Interface "IRetryPolicy"
        |+> [
            "nextRetryDelayInMilliseconds" => RetryContext?retryContext ^-> T<int>
            |> WithComment "Called after the transport loses the connection."
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
        |+> [
            "type" =? MessageType
            |> WithComment "A MessageType value indicating the type of this message."
        ]

    let HubInvocationMessage =
        Interface "HubInvocationMessage"
        |=> Extends [HubMessageBase]
        |+> [
            "headers" =? !? MessageHeaders
            |> WithComment "A MessageHeaders dictionary containing headers attached to the message."
            "invocationId" =? !? T<string>
            |> WithComment "The ID of the invocation relating to this message. This is expected to be present for StreamInvocationMessage and CompletionMessage. It may be 'undefined' for an InvocationMessage if the sender does not expect a response."
        ]

    let CancelInvocationMessage =
        Interface "CancelInvocationMessage"
        |=> Extends [HubInvocationMessage]

    let CloseMessage =
        Interface "CloseMessage"
        |=> Extends [HubMessageBase]
        |+> [
            "allowReconnect" =? !? T<bool>
            |> WithComment "If true, clients with automatic reconnects enabled should attempt to reconnect after receiving the CloseMessage. Otherwise, they should not."
            "error" =? !? T<string>
            |> WithComment "The error that triggered the close, if any. If this property is undefined, the connection was closed normally and without error."
        ]

    let CompletionMessage =
        Interface "CompletionMessage"
        |=> Extends [HubInvocationMessage]
        |+> [
            "error" =? !? T<string>
            |> WithComment "The error produced by the invocation, if any. Either error or result must be defined, but not both."
            "result" =? !? T<obj>
            |> WithComment "The result produced by the invocation, if any. Either error or result must be defined, but not both."
        ]

    let InvocationMessage =
        Interface "InvocationMessage"
        |=> Extends [HubInvocationMessage]
        |+> [
            "arguments" =? !| T<obj>
            |> WithComment "The target method arguments."
            "streamIds" =? !| T<string>
            |> WithComment "The target methods stream IDs."
            "target" =? T<string>
            |> WithComment "The target method name."
        ]

    let PingMessage =
        Interface "PingMessage"
        |=> Extends [HubMessageBase]

    let StreamInvocationMessage =
        Interface "StreamInvocationMessage"
        |=> Extends [HubInvocationMessage]
        |+> [
            "arguments" =? !| T<obj>
            |> WithComment "The target method arguments."
            "streamIds" =? !| T<string>
            |> WithComment "The target methods stream IDs."
            "target" =? T<string>
            |> WithComment "The target method name."
        ]

    let StreamItemMessage =
        Interface "StreamItemMessage"
        |=> Extends [HubInvocationMessage]
        |+> [
            "item" =? !? T<obj>
            |> WithComment "The item produced by the server."
        ]

    let HubMessage =
        CancelInvocationMessage +
        CloseMessage +
        CompletionMessage +
        InvocationMessage +
        PingMessage +
        StreamInvocationMessage +
        StreamItemMessage

    let TransferFormat =
        Pattern.EnumInlines "TransferFormat" [
            "Text", "1"
            "Binary", "2"
        ]

    let IHubProtocol =
        Interface "IHubProtocol"
        |+> [
            "name" =? T<string>
            |> WithComment "The name of the protocol. This is used by SignalR to resolve the protocol between the client and server."
            "transferFormat" =? TransferFormat
            |> WithComment "The TransferFormat of the protocol."
            "version" =? T<int>
            |> WithComment "The version of the protocol."
            "parseMessages" => T<int>?input * ILogger?logger ^-> !| HubMessage
            |> WithComment "Creates an array of HubMessage objects from the specified serialized representation. If transferFormat is 'Text', the input parameter must be a string, otherwise it must be an ArrayBuffer."
            "parseMessages" => T<ArrayBuffer>?input * ILogger?logger ^-> !| HubMessage
            |> WithComment "Creates an array of HubMessage objects from the specified serialized representation. If transferFormat is 'Text', the input parameter must be a string, otherwise it must be an ArrayBuffer."
            "writeMessage" => HubMessage?message ^-> (T<string> + T<ArrayBuffer>)
            |> WithComment "Writes the specified HubMessage to a string or ArrayBuffer and returns it. If transferFormat is 'Text', the result of this method will be a string, otherwise it will be an ArrayBuffer."
        ]

    let IStreamSubscriber =
        Generic - fun t ->
            Interface "IStreamSubscriber"
            |+> [
                "closed" =? !? T<bool>
                |> WithComment "A boolean that will be set by the IStreamResult when the stream is closed."
                "complete" => T<unit> ^-> T<unit>
                |> WithComment "Called by the framework when the end of the stream is reached. After this method is called, no additional methods on the IStreamSubscriber will be called."
                "error" => T<obj>?err ^-> T<unit>
                |> WithComment "Called by the framework when an error has occurred. After this method is called, no additional methods on the IStreamSubscriber will be called."
                "next" => t ^-> TSelf.[t]
                |> WithComment "Called by the framework when a new item is available."
            ]

    let ISubscription =
        Interface "ISubscription"
        |+> [
            "dispose" => T<unit> ^-> T<unit>
            |> WithComment "Disconnects the IStreamSubscriber associated with this subscription from the stream."
        ]

    let IStreamResult =
        Generic - fun t ->
            Interface "IStreamResult"
            |+> [
                "subscribe" => IStreamSubscriber.[t]?subscriber ^-> ISubscription
                |> WithComment "Attaches a IStreamSubscriber, which will be invoked when new items are available from the stream."
            ]

    let HubConnection =
        Generic - fun t ->
            Class "signalR.HubConnection"
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
                "invoke" => T<string>?methodName * (!| T<obj>)?args ^-> T<Promise<_>>.[t]
                |> WithComment "Invokes a hub method on the server using the specified name and arguments. The Promise returned by this method resolves when the server indicates it has finished invoking the method. When the promise resolves, the server has finished invoking the method. If the server method returns a result, it is produced as the result of resolving the Promise."
                "off" => T<string>?methodName ^-> T<unit>
                |> WithComment "Removes all handlers for the specified hub method."
                "off" => T<string>?methodName * (!| T<obj> ^-> T<unit>)?method ^-> T<unit>
                |> WithComment "Removes the specified handler for the specified hub method. You must pass the exact same Function instance as was previously passed to on(string, (args: any[]) => void). Passing a different instance (even if the function body is the same) will not remove the handler."
                "on" => T<string>?methodName * (!| T<obj> ^-> T<unit>)?newMethod ^-> T<unit>
                |> WithComment "Registers a handler that will be invoked when the hub method with the specified method name is invoked."
                "onclose" => (!? T<Error> ^-> T<unit>)?callback ^-> T<unit>
                |> WithComment "Registers a handler that will be invoked when the connection is closed."
                "onreconnected" => (!? T<string> ^-> T<unit>)?callback ^-> T<unit>
                "onreconnecting" => (!? T<Error> ^-> T<unit>)?callback ^-> T<unit>
                "send" => T<string>?methodName * (!| T<obj>)?args ^-> T<Promise<unit>>
                "start" => T<unit> ^-> T<Promise<unit>>
                "stop" => T<unit> ^-> T<Promise<unit>>
                "stream" => t * t * T<string>?methodName * (!| T<obj>)?args ^-> IStreamResult.[t]
            ]

    let HttpTransportType =
        Pattern.EnumInlines "HttpTransportType" [
            "None", "0"
            "WebSockets", "1"
            "ServerSentEvents", "2"
            "LongPolling", "4"
        ]

    let ITransport =
        Interface "ITransport"
        |+> [
            "onclose" =? T<Error>?error ^-> T<unit>
            "onreceive" =? (T<string> + T<ArrayBuffer>)?data ^-> T<unit>
            "connect" => T<string>?url * TransferFormat?transferFormat ^-> T<Promise<unit>> 
            "send" => T<obj>?data ^-> T<Promise<unit>>
            "stop" => T<unit> ^-> T<Promise<unit>>
        ]

    let IHttpConnectionOptions =
        Pattern.Config "IHttpConnectionOptions" {
            Required = []
            Optional = [
                "EventSource", EventSourceCtor
                "httpClient", HttpClient.Type
                "logger", (ILogger + LogLevel)
                "logMessageContent", T<bool>
                "skipNegotiation", T<bool>
                "transport", (HttpTransportType + ITransport)
                "WebSocket", WebSocketCtor
                "accessTokenFactory", T<unit> ^-> (T<string> + T<Promise<string>>)
            ]
        }

    // let IHttpConnectionOptions =
    //     Interface "IHttpConnectionOptions"
    //     |+> [
    //         "EventSource" =? !? EventSourceCtor
    //         |> WithComment "A constructor that can be used to create an EventSource."
    //         "httpClient" =? !? HttpClient
    //         |> WithComment "An HttpClient that will be used to make HTTP requests."
    //         "logger" =? !? (ILogger + LogLevel)
    //         |> WithComment "Configures the logger used for logging. Provide an ILogger instance, and log messages will be logged via that instance. Alternatively, provide a value from the LogLevel enumeration and a default logger which logs to the Console will be configured to log messages of the specified level (or higher)."
    //         "logMessageContent" =? !? T<bool>
    //         |> WithComment "A boolean indicating if message content should be logged. Message content can contain sensitive user data, so this is disabled by default."
    //         "skipNegotiation" =? !? T<bool>
    //         |> WithComment "A boolean indicating if negotiation should be skipped. Negotiation can only be skipped when the transport property is set to 'HttpTransportType.WebSockets'."
    //         "transport" =? !? (HttpTransportType + ITransport)
    //         |> WithComment "An HttpTransportType value specifying the transport to use for the connection."
    //         "WebSocket" =? !? WebSocketCtor
    //         |> WithComment "A constructor that can be used to create a WebSocket."
    //         "accessTokenFactory" => T<unit> ^-> (T<string> + T<Promise<string>>)
    //         |> WithComment "A function that provides an access token required for HTTP Bearer authentication."
    //     ]

    let HubConnectionBuilder =
        Generic - fun t -> 
            Class "signalR.HubConnectionBuilder"
            |+> Static [
                Constructor T<unit>
            ]
            |+> Instance [
                "httpConnectionOptions" =? !? IHttpConnectionOptions
                "logger" =? !? ILogger
                "protocol" =? !? IHubProtocol
                "reconnectPolicy" =? !? IRetryPolicy
                "url" =? !? T<string>
                "build" => T<unit> ^-> HubConnection.[t]
                |> WithComment "Creates a HubConnection from the configuration options specified in this builder."
                "configureLogging" => ILogger?logger ^-> TSelf.[t]
                |> WithComment "Configures custom logging for the HubConnection."
                "configureLogging" => T<string>?logLevel ^-> TSelf.[t]
                |> WithComment "Configures custom logging for the HubConnection."
                "configureLogging" => LogLevel?logLevel ^-> TSelf.[t]
                |> WithComment "Configures custom logging for the HubConnection."
                "withAutomaticReconnect" => T<unit> ^-> TSelf.[t]
                |> WithComment "Configures the HubConnection to automatically attempt to reconnect if the connection is lost. By default, the client will wait 0, 2, 10 and 30 seconds respectively before trying up to 4 reconnect attempts."
                "withAutomaticReconnect" => IRetryPolicy?reconnectPolicy ^-> TSelf.[t]
                |> WithComment "Configures the HubConnection to automatically attempt to reconnect if the connection is lost."
                "withAutomaticReconnect" => (!| T<int>)?retryDelays ^-> TSelf.[t]
                |> WithComment "Configures the HubConnection to automatically attempt to reconnect if the connection is lost."
                "withHubProtocol" => IHubProtocol?protocol ^-> TSelf.[t]
                |> WithComment "Configures the HubConnection to use the specified Hub Protocol."
                "withUrl" => T<string>?url ^-> TSelf.[t]
                |> WithComment "Configures the HubConnection to use HTTP-based transports to connect to the specified URL. The transport will be selected automatically based on what the server and client support."
                "withUrl" => T<string>?url * HttpTransportType?transportType ^-> TSelf.[t]
                |> WithComment "Configures the HubConnection to use the specified HTTP-based transport to connect to the specified URL."
                "withUrl" => T<string>?url * IHttpConnectionOptions?options ^-> TSelf.[t]
                |> WithComment "Configures the HubConnection to use HTTP-based transports to connect to the specified URL."
            ]

    let JsonHubProtocol =
        Class "signalR.JsonHubProtocol"
        |+> Static [
            Constructor T<unit>
        ]
        |+> Instance [
            "name" =? T<string>
            |> WithComment "The name of the protocol. This is used by SignalR to resolve the protocol between the client and server."
            "tranferFormat" =? TransferFormat
            |> WithComment "The TransferFormat of the protocol."
            "version" =? T<int>
            |> WithComment "The version of the protocol."
            "parseMessages" => T<string>?input * ILogger?logger ^-> !| HubMessage
            |> WithComment "Creates an array of HubMessage objects from the specified serialized representation."
            "writeMessage" => HubMessage?message ^-> T<string>
            |> WithComment "Writes the specified HubMessage to a string and returns it."
        ]

    let NullLogger =
        Class "signalR.NullLogger"
        |+> Static [

            "instance" =? ILogger
            |> WithComment "The singleton instance of the NullLogger."
        ]
        |+> Instance [
            "log" => LogLevel?_logLevel * T<string>?_message ^-> T<unit>
            |> WithComment "Called by the framework to emit a diagnostic message."
        ]

    let Subject =
        Generic - fun t ->
            Class "signalR.Subject"
            |=> Implements [IStreamSubscriber.[t]]
            |+> Static [
                Constructor T<unit>
            ]
            |+> Instance [
                "cancelCallback" =? !? T<unit -> Promise<unit>>
                "observers" =? !| IStreamSubscriber.[t]
                "subscribe" => IStreamSubscriber.[t]?observer ^-> ISubscription
            ]

    let XhrHttpClient =
        Class "signalR.XhrHttpClient"
        |=> Inherits HttpClient
        |+> Static [
            Constructor ILogger?logger
            |> WithComment "Creates a new instance of the XhrHttpClient, using the provided ILogger to log messages."
        ]

    let SignalR =
        Class "signalR"
        |=> Nested [
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

    let Assembly =
        Assembly [
            Namespace "WebSharper.SignalR.Resources" [
                Resource "SignalRCDN" "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/5.0.9/signalr.min.js"
                |> AssemblyWide
            ]
            Namespace "WebSharper.SignalR" [
                SignalR
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
