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
            let! data = fetchPage page |> Async.AwaitPromise
            let totalPages = data.recenttracks.``@attr``.totalPages |> int
            let totalTracks = data.recenttracks.``@attr``.total |> int
            let tracksPerPage = data.recenttracks.``@attr``.perPage |> int
            let currentPage = data.recenttracks.``@attr``.page |> int

            let refinedData =
                data.recenttracks.track
                |> Array.where (fun x -> try x.``@attr``.nowplaying.ToUpperInvariant() = "FALSE" with | _ -> true)
                // Alternative implementation with null check:
                    ///// Null check chaining:
                    //let inline (>>?) f g = f >> Option.bind (g >> Option.ofObj)

                    //|> Array.where
                    //    (Option.ofObj
                    //    >>? (fun x -> x.``@attr``)
                    //    >>? (fun x -> x.nowplaying)
                    //    >> Option.map (fun x -> System.Convert.ToBoolean(x))
                    //    >> Option.defaultValue true)

            outputHtml.innerHTML <- $"{outputHtml.innerHTML}<br> {currentPage}/{totalPages} refined count: {refinedData.Length}" 
            yield refinedData
            if currentPage > 1 then yield! loop (page - 1) // Recurse from oldest page (totalPages) to first page (1)
        }
    promise {
        let! data = fetchPage 1 // Only used for the total pages number (used as the initial page)
        let totalPages = data.recenttracks.``@attr``.totalPages |> int
        return loop totalPages
    }

myButton.onclick <- fun _ ->
    execute()
    |> Promise.map (fun seq -> 
        seq
        |> AsyncSeq.iter ignore
        |> Async.StartAsPromise)
    