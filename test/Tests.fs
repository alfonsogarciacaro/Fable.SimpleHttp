module Tests

open QUnit
open Fable.SimpleHttp
open Fable.SimpleJson 

registerModule "Simple Http Tests"
 
setTimeout (10 * 1000)

testCaseAsync "Http.get returns text when status is OK" <| fun test ->
    async {
        let! (statusCode, responseText) = Http.get "/api/get-first"
        do test.areEqual responseText "first"
    }

testCaseAsync "Http.get does not throw when status code is not OK" <| fun test ->
    async {
        let! result = Async.Catch (Http.get "/api/not-existent") 
        match result with 
        | Choice1Of2 (status, responseText) -> 
            test.areEqual 404 status 
            test.areEqual "Not Found" responseText
        | Choice2Of2 error -> test.failwith "Exected no errors!"
    }

testCaseAsync "Http.get returns text when status is OK" <| fun test ->
    async {
        let! (statusCode, responseText) = Http.get "/api/get-first"
        do test.areEqual responseText "first"
    }

testCaseAsync "Http.get resolves correctly when response is 200" <| fun test ->
    async {
        let! (status, responseText) = Http.get "/api/get-first"
        test.areEqual 200 status 
        test.areEqual "first" responseText 
    }

testCaseAsync "Http.get resolves correctly when response is 404" <| fun test ->
    async {
        let! (status, responseText) = Http.get "/api/not-existent" 
        test.areEqual status 404
        test.areEqual responseText "Not Found"
    }

testCaseAsync "Http.post resolves correctly when resposne is 200" <| fun test ->
    async {
        let input = "my data"
        let! (statusCode, responseText) = Http.post "/api/post-echo" input
        test.areEqual 200 statusCode
        test.areEqual input responseText
    }

testCaseAsync "Headers can be round-tripped" <| fun test ->
    async {
        let! response = 
          Http.request "/api/echo-headers"
          |> Http.method GET
          |> Http.header (Headers.authorization "Bearer: <token>")
          |> Http.header (Headers.contentType "application/json")
          |> Http.send 

        do test.areEqual 200 response.statusCode
    }

testCaseAsync "Body content can be round-tripped" <| fun test ->
    async {
        let! response = 
          Http.request "/api/post-echo"
          |> Http.method POST
          |> Http.content (BodyContent.Text "hello")
          |> Http.send 

        test.areEqual 200 response.statusCode
        test.areEqual "hello" response.responseText
    }

testCaseAsync "Empty body content is allowed" <| fun test ->
    async {
        let! response = 
          Http.request "/api/post-echo"
          |> Http.method POST
          |> Http.content BodyContent.Empty
          |> Http.send 

        test.areEqual 200 response.statusCode
        test.areEqual "" response.responseText
    }

testCaseAsync "Form data can be round-tripped" <| fun test ->
    async {
        let formData = 
            FormData.create()
            |> FormData.append "firstName" "Zaid"
            |> FormData.append "lastName" "Ajaj"
        
        let! response = 
            Http.request "/api/echo-form"
            |> Http.method POST 
            |> Http.content (BodyContent.Form formData) 
            |> Http.send 

        test.areEqual 200 response.statusCode 

        let form = Json.parseAs<Result<string, string> list> response.responseText
        match form with 
        | [ Ok "Zaid"; Ok "Ajaj" ] -> test.pass()
        | otherwise -> test.unexpected otherwise
    }  

testCaseAsync "Binary blob data can be roundtripped" <| fun test -> 
    async {
        let blob = Blob.fromText "hello!"
        
        let! response = 
            Http.request "/api/echoBinary"
            |> Http.method POST 
            |> Http.overrideResponseType ResponseTypes.Blob
            |> Http.content (BodyContent.Binary blob)
            |> Http.send 

        test.areEqual 200 response.statusCode
        test.areEqual "" response.responseText

        match response.content with 
        | ResponseContent.Blob result -> 
            let! content = FileReader.readBlobAsText result 
            test.areEqual "hello!" content 
        | _ -> 
            test.failwith "Expected to read binary data"

        Map.tryFind "content-type" response.responseHeaders 
        |> function 
            | Some "application/octet-stream" -> test.pass()
            | _ -> test.unexpected response.responseHeaders
    }