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

    let DefaultHttpClient =
        Class "DefaultHttpClient"
        |+> Static [
            Constructor ILogger?logger // ILogger
        ]
        |+> Instance [
            "delete" => T<string>?url * HttpRequest?options ^-> HttpRequest // T<Promise<HttpRequest>>
            |> WithComment "Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "get" => T<string>?url * HttpRequest?options ^-> HttpRequest // T<Promise<HttpRequest>>
            |> WithComment "Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "getCookieString" => T<string> ^-> T<string>
            |> WithComment "Gets the cookiestring"
            "post" => T<string>?url * HttpRequest?options ^-> HttpRequest // T<Promise<HttpRequest>>
            |> WithComment "Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result."
            "send" => HttpRequest ^-> HttpRequest // T<Promise<HttpRequest>>
            |>WithComment "Send a request, returning a Promise that resolves with an HttpResponse representing the result."
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
                DefaultHttpClient
                AbortError
                HttpError
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
