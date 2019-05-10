namespace Fable.SimpleHttp

open Browser
open Browser.Types
open Fable.Core

type HttpMethod =
    | GET
    | POST
    | PUT
    | PATCH
    | DELELE
    | HEAD
    | OPTIONS

type Header = Header of string * string

[<RequireQualifiedAccess>]
type BodyContent =
    | Empty
    | Text of string
    | Binary of Browser.Types.Blob
    | Form of Browser.Types.FormData

[<RequireQualifiedAccess>]
type ResponseTypes =
    | Text
    | Blob
    | ArrayBuffer

type HttpRequest = {
    url: string
    method: HttpMethod
    headers: Header list
    overridenMimeType: Option<string>
    overridenResponseType: Option<ResponseTypes>
    content: BodyContent
}

[<RequireQualifiedAccess>]
type ResponseContent =
    | Text of string
    | Blob of Browser.Types.Blob
    | ArrayBuffer of JS.ArrayBuffer
    | Unknown of obj

type HttpResponse = {
    statusCode: int
    responseText: string
    responseType: string
    responseHeaders: Map<string, string>
    content : ResponseContent
}

module Blob =
    /// Creates a Blob from the given input string
    [<Emit("new Blob([$0], { 'mimeType':'text/plain' })")>]
    let fromText (value: string) : Blob = jsNative


/// Utility functions to work with blob and file APIs.
module FileReader =
    /// Asynchronously reads the blob data content as string
    let readBlobAsText (blob: Blob) : Async<string> =
        Async.FromContinuations <| fun (resolve, _, _) ->
            let reader = FileReader.Create()
            reader.onload <- fun _ ->
                if reader.readyState = FileReaderState.DONE
                then resolve (unbox reader.result)

            reader.readAsText(blob)

    /// Asynchronously reads the blob data content as string
    let readFileAsText (file: File) : Async<string> =
        Async.FromContinuations <| fun (resolve, _, _) ->
            let reader = FileReader.Create()
            reader.onload <- fun _ ->
                if reader.readyState = FileReaderState.DONE
                then resolve (unbox reader.result)

            reader.readAsText(file)

module FormData =

    [<Emit("new FormData()")>]
    /// Creates a new FormData object
    let create() : FormData = jsNative

    /// Appends a key-value pair to the form data
    let append (key:string) (value:string) (form : FormData) : FormData =
        form.append(key, value)
        form

    /// Appends a file to the form data
    let appendFile (key: string) (file: File) (form: FormData) : FormData =
        form.append (key, file)
        form

    /// Appends a named file to the form data
    let appendNamedFile (key: string) (fileName: string) (file: File) (form: FormData) : FormData =
        form.append (key, file, fileName)
        form

    /// Appends a blog to the form data
    let appendBlob (key: string) (blob: Blob) (form: FormData) : FormData =
        form.append (key, blob)
        form

    /// Appends a blog to the form data
    let appendNamedBlob (key: string) (fileName: string) (blob: Blob) (form: FormData) : FormData =
        form.append (key, blob, fileName)
        form

module Headers =
    let contentType value = Header("Content-Type", value)
    let accept value = Header("Accept", value)
    let acceptCharset value = Header("Accept-Charset", value)
    let acceptEncoding value = Header("Accept-Encoding", value)
    let acceptLanguage value = Header("Accept-Language", value)
    let acceptDateTime value = Header("Accept-Datetime", value)
    let authorization value = Header("Authorization", value)
    let cacheControl value = Header("Cache-Control", value)
    let connection value = Header("Connection", value)
    let cookie value = Header("Cookie", value)
    let contentMD5 value = Header("Content-MD5", value)
    let date value = Header("Date", value)
    let expect value = Header("Expect", value)
    let ifMatch value = Header("If-Match", value)
    let ifModifiedSince value = Header("If-Modified-Since", value)
    let ifNoneMatch value = Header("If-None-Match", value)
    let ifRange value = Header("If-Range", value)
    let IfUnmodifiedSince value = Header("If-Unmodified-Since", value)
    let maxForwards value = Header("Max-Forwards", value)
    let origin value = Header ("Origin", value)
    let pragma value = Header("Pragma", value)
    let proxyAuthorization value = Header("Proxy-Authorization", value)
    let range value = Header("Range", value)
    let referer value = Header("Referer", value)
    let userAgent value = Header("User-Agent", value)
    let create key value = Header(key, value)

module Http =
    let private defaultRequest =
        { url = "";
          method = HttpMethod.GET
          headers = []
          overridenMimeType = None
          overridenResponseType = None
          content = BodyContent.Empty }

    [<Emit("$1.split($0)")>]
    let private splitAt (delimeter: string) (input: string) : string [] = jsNative

    let private serializeMethod = function
        | HttpMethod.GET -> "GET"
        | HttpMethod.POST -> "POST"
        | HttpMethod.PATCH -> "PATCH"
        | HttpMethod.PUT -> "PUT"
        | HttpMethod.DELELE -> "DELETE"
        | HttpMethod.OPTIONS -> "OPTIONS"
        | HttpMethod.HEAD -> "HEAD"

    /// Starts the configuration of the request with the specified url
    let request (url: string) : HttpRequest =
        { defaultRequest with url = url }

    /// Sets the Http method of the request
    let method httpVerb (req: HttpRequest) =
        { req with method = httpVerb }

    /// Appends a header to the request configuration
    let header (singleHeader: Header) (req: HttpRequest) =
        { req with headers = List.append req.headers [singleHeader] }

    /// Appends a list of headers to the request configuration
    let headers (values: Header list) (req: HttpRequest)  =
        { req with headers = List.append req.headers values }

    /// Specifies a MIME type other than the one provided by the server to be used instead when interpreting the data being transferred in a request. This may be used, for example, to force a stream to be treated and parsed as "text/xml", even if the server does not report it as such.
    let overrideMimeType (value: string) (req: HttpRequest) =
        { req with overridenMimeType = Some value }

    /// Change the expected response type from the server
    let overrideResponseType (value: ResponseTypes) (req: HttpRequest) =
        { req with overridenResponseType = Some value }

    /// Sends the request to the server, this function does not throw
    let send (req: HttpRequest) : Async<HttpResponse> =
        Async.FromContinuations <| fun (resolve, reject, _) ->
            let xhr = XMLHttpRequest.Create()
            xhr.``open``(serializeMethod req.method, req.url)
            xhr.onreadystatechange <- fun _ ->
                if xhr.readyState = ReadyState.Done
                then resolve {
                    responseText =
                        match xhr.responseType with
                        | "" -> xhr.responseText
                        | "text" -> xhr.responseText
                        | _ -> ""

                    statusCode = int xhr.status
                    responseType = xhr.responseType
                    content =
                        match xhr.responseType with
                        | ("" | "text") -> ResponseContent.Text xhr.responseText
                        | "arraybuffer" -> ResponseContent.ArrayBuffer (unbox xhr.response)
                        | "blob" -> ResponseContent.Blob (unbox xhr.response)
                        | _ -> ResponseContent.Unknown xhr.response

                    responseHeaders =
                        xhr.getAllResponseHeaders()
                        |> splitAt "\r\n"
                        |> Array.choose (fun headerLine ->
                            let parts = splitAt ":" headerLine
                            match List.ofArray parts with
                            | key :: rest ->  Some (key.ToLower(), (String.concat ":" rest).Trim())
                            | otherwise -> None)
                        |> Map.ofArray
                }

            for (Header(key, value)) in req.headers do
                xhr.setRequestHeader(key, value)

            match req.overridenMimeType with
            | Some mimeType -> xhr.overrideMimeType(mimeType)
            | None -> ()

            match req.overridenResponseType with
            | Some ResponseTypes.Text -> xhr.responseType <- "text"
            | Some ResponseTypes.Blob -> xhr.responseType <- "blob"
            | Some ResponseTypes.ArrayBuffer -> xhr.responseType <- "arraybuffer"
            | None -> ()

            match req.method, req.content with
            | GET, _ -> xhr.send(None)
            | _, BodyContent.Empty -> xhr.send(None)
            | _, BodyContent.Text value -> xhr.send(value)
            | _, BodyContent.Form formData -> xhr.send(formData)
            | _, BodyContent.Binary blob -> xhr.send(blob)

    /// Sets the body content of the request
    let content (bodyContent: BodyContent) (req: HttpRequest) : HttpRequest =
        { req with content = bodyContent }

    /// Safely sends a GET request and returns a tuple(status code * response text). This function does not throw.
    let get url : Async<int * string> =
        Async.FromContinuations <| fun (resolve, reject, _) ->
            let xhr = XMLHttpRequest.Create()
            xhr.``open``("GET", url)
            xhr.onreadystatechange <- fun _ ->
                if xhr.readyState = ReadyState.Done
                then resolve (int xhr.status, xhr.responseText)
            xhr.send(None)

    /// Safely sends a PUT request and returns a tuple(status code * response text). This function does not throw.
    let put url (date: string): Async<int * string> =
        Async.FromContinuations <| fun (resolve, reject, _) ->
            let xhr = XMLHttpRequest.Create()
            xhr.``open``("PUT", url)
            xhr.onreadystatechange <- fun _ ->
                if xhr.readyState = ReadyState.Done
                then resolve (int xhr.status, xhr.responseText)
            xhr.send(None)

    /// Safely sends a DELETE request and returns a tuple(status code * response text). This function does not throw.
    let delete url (date: string): Async<int * string> =
        Async.FromContinuations <| fun (resolve, reject, _) ->
            let xhr = XMLHttpRequest.Create()
            xhr.``open``("DELETE", url)
            xhr.onreadystatechange <- fun _ ->
                if xhr.readyState = ReadyState.Done
                then resolve (int xhr.status, xhr.responseText)
            xhr.send(None)

    /// Safely sends a PUT request and returns a tuple(status code * response text). This function does not throw.
    let patch url (data: string) : Async<int * string> =
        Async.FromContinuations <| fun (resolve, reject, _) ->
            let xhr = XMLHttpRequest.Create()
            xhr.``open``("PATCH", url)
            xhr.onreadystatechange <- fun _ ->
                if xhr.readyState = ReadyState.Done
                then resolve (int xhr.status, xhr.responseText)
            xhr.send(data)

    /// Safely sends a POST request and returns a tuple(status code * response text). This function does not throw.
    let post url (data: string) : Async<int * string> =
        Async.FromContinuations <| fun (resolve, reject, _) ->
            let xhr = XMLHttpRequest.Create()
            xhr.``open``("POST", url)
            xhr.onreadystatechange <- fun _ ->
                if xhr.readyState = ReadyState.Done
                then resolve (int xhr.status, xhr.responseText)
            xhr.send(data)
