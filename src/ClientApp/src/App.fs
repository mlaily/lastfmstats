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

let logToHtml msg = outputHtml.innerHTML <- $"{outputHtml.innerHTML}<br>{msg}" 

let batchSize = 100
let fetchAllTracks () =
    let fetchPage = fetchForUser batchSize
    let rec fetchAllTracks' page =
        asyncSeq {
            let! data = fetchPage page |> Async.AwaitPromise
            //let totalPages = data.recenttracks.``@attr``.totalPages |> int
            //let totalTracks = data.recenttracks.``@attr``.total |> int
            //let tracksPerPage = data.recenttracks.``@attr``.perPage |> int
            let currentPage = data.recenttracks.``@attr``.page |> int

            let refinedData =
                data.recenttracks.track
                |> Array.where (fun x -> try x.``@attr``.nowplaying.ToUpperInvariant() = "FALSE" with | _ -> true)
            
            for track in refinedData do yield track

            logToHtml $"Page {currentPage} - {refinedData.Length} tracks." 
            if currentPage > 1 then yield! fetchAllTracks' (page - 1) // Recurse from oldest page (totalPages) to first page (1)
        }
    promise {
        let! data = fetchPage 1 // Only used for the total pages number (used as the initial page)
        let totalPages = data.recenttracks.``@attr``.totalPages |> int
        logToHtml $"Enumerating pages from {totalPages} to 1..."
        return fetchAllTracks' totalPages
    }


myButton.onclick <- fun _ ->
    myButton.disabled <- true
    fetchAllTracks()
    |> Promise.map (fun asyncSeq -> 
        asyncSeq
        |> AsyncSeq.iter (fun track ->  logToHtml $"{track.name} - {track.artist.``#text``}")
        |> Async.StartAsPromise)
    