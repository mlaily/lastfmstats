module App

open Browser.Dom
open FSharp.Control
open Fable.Core
open Fetch.Types
open Fetch
open ApiModels
open System
open Thoth.Json
open System.Globalization

// Get a reference to our button and cast the Element to an HTMLButtonElement
let myButton = document.querySelector(".my-button") :?> Browser.Types.HTMLButtonElement
let userNameInput = document.querySelector("#username") :?> Browser.Types.HTMLInputElement
let outputHtml = document.querySelector("#output") :?> Browser.Types.HTMLParagraphElement


let logToHtml msg = outputHtml.innerHTML <- $"{outputHtml.innerHTML}<br>{msg}" 

let fetchAllTracks userName =
    let batchSize = 100
    let fetchPage  page =
        fetchTracks userName batchSize page
        |> Promise.map (function
            | Ok ok -> ok
            | Error error -> failwith $"Error while fetching tracks: {error.Response.StatusText} ({error.Response.Status}) - {error.Body}")
    let rec fetchAllTracks' page =
        asyncSeq {
            let! data = fetchPage page |> Async.AwaitPromise
            let currentPage = data.recenttracks.``@attr``.page |> int

            let refinedData =
                data.recenttracks.track
                |> Array.where (fun x -> try x.``@attr``.nowplaying.ToUpperInvariant() = "FALSE" with | _ -> true)
            
            yield refinedData

            logToHtml $"Page {currentPage} - {refinedData.Length} tracks." 
            if currentPage > 1 then yield! fetchAllTracks' (page - 1) // Recurse from oldest page (totalPages) to first page (1)
        }
    promise {
        let! data = fetchPage 1 // Only used for the total pages number (used as the initial page)
        let totalPages = data.recenttracks.``@attr``.totalPages |> int
        logToHtml $"Enumerating pages from {totalPages} to 1..."
        return fetchAllTracks' totalPages
    }

let mapScrobbleData (track: GetRecentTracksJson.Recenttracks.Track) = {
    Artist = track.artist.``#text``
    Album = track.album.``#text``
    TimePlayedTimeStamp = track.date.uts
    }

let truncate precision (dateTime: DateTimeOffset) =
    if precision = TimeSpan.Zero then dateTime
    else dateTime.AddTicks(-(dateTime.Ticks % precision.Ticks))

let truncateToSecond = truncate (TimeSpan.FromSeconds(1.0))

let dateTimeResolver =
    let encoder = truncateToSecond >> (fun x -> x.ToString("u", CultureInfo.InvariantCulture) |> box<JsonValue>)
    let decoder = Decode.datetimeOffset
    (encoder, decoder)

let postScrobbles userName (scrobbles: ScrobbleData[]) =
    promise {
        let jsonBody = Encode.Auto.toString(0, scrobbles, CaseStrategy.CamelCase, Extra.empty |> (Extra.withCustom <|| dateTimeResolver))
        let! result =
            saneFetch
                $"http://localhost:5000/api/scrobbles/{userName}" [
                    RequestProperties.Method HttpMethod.POST
                    requestHeaders [ HttpRequestHeaders.ContentType "application/json" ]
                    RequestProperties.Body <| unbox(jsonBody)
                    ]
        logToHtml result.Status
        ()
    }

//myButton.onclick <- fun _ ->
//    promise {
//        let scrobbleData1 = { User = "prout"; Album = ""; Artist = ""; TimePlayed = DateTimeOffset.UtcNow }
//        let body = [ scrobbleData1 ]
//        let jsonBody = Encode.Auto.toString(4, body, CaseStrategy.CamelCase, Extra.empty |> (Extra.withCustom <|| dateTimeResolver))
//        let! result =
//            saneFetch
//                "http://localhost:5000/api/scrobble" [
//                    RequestProperties.Method HttpMethod.POST
//                    requestHeaders [ HttpRequestHeaders.ContentType "application/json" ]
//                    RequestProperties.Body <| unbox(jsonBody)
//                    ]
//        myButton.textContent <- "sent"
//    }

myButton.onclick <- fun _ ->
    myButton.disabled <- true
    let userName = userNameInput.value
    fetchAllTracks userName
    |> Promise.map (fun tracksAsyncSeq ->
        tracksAsyncSeq
        // |> AsyncSeq.iter fun x-> logToHtml $"{track.name} - {track.artist.``#text``}")
        |> AsyncSeq.iterAsync (fun tracks ->
            tracks
            |> Array.map (fun track -> mapScrobbleData track)
            |> postScrobbles userName
            |> Async.AwaitPromise)
        |> Async.StartAsPromise)
