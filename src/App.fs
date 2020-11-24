module App

open Browser.Dom
open FSharp.Control
open Fable.Core

// Get a reference to our button and cast the Element to an HTMLButtonElement
let myButton = document.querySelector(".my-button") :?> Browser.Types.HTMLButtonElement
let usernameInput = document.querySelector("#username") :?> Browser.Types.HTMLInputElement
let outputHtml = document.querySelector("#output") :?> Browser.Types.HTMLParagraphElement

/// Fetch for the user specified in usernameInput, with error handling
let fetchForUser limit page =
    fetchTracks usernameInput.value limit page
    |> Promise.map (function
        | Ok ok -> ok
        | Error error ->
            outputHtml.textContent <- $"{error.Response.StatusText} ({error.Response.Status}) - {error.Body}"
            failwith "Error while fetching tracks!")

let batchSize = 100
let execute () =
    let fetchPage = fetchForUser batchSize
    let rec loop page =
        asyncSeq {
            let! currentData = fetchPage page |> Async.AwaitPromise
            let actualLimit = currentData.recenttracks.``@attr``.perPage |> int
            let totalPages = currentData.recenttracks.``@attr``.totalPages |> int
            let currentPage = currentData.recenttracks.``@attr``.page |> int

            outputHtml.innerHTML <- $"{outputHtml.innerHTML}<br> {currentPage}/{totalPages}" 
            yield currentData
            if currentPage > 1 then yield! loop (page - 1) // Recurse from oldest page (totalPages) to first page (1)
        }
    promise {
        let! queryMetadata = fetchPage 1
        let totalPages = queryMetadata.recenttracks.``@attr``.totalPages |> int
        return loop totalPages
    }

myButton.onclick <- fun _ ->
    execute()
    |> Promise.map (fun seq -> 
        seq
        |> AsyncSeq.iter ignore
        |> Async.StartAsPromise)
    