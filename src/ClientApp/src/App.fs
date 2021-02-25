module App

open Browser.Dom
open FSharp.Control
open Fable.Core
open Fetch.Types
open Fetch
open ApiModels
open System
open Thoth.Json

open Feliz
open Feliz.Plotly
open Feliz.prop
open Feliz.Plotly
open Feliz.Plotly

let myButton = document.querySelector(".my-button") :?> Browser.Types.HTMLButtonElement
let myButton2 = document.querySelector(".my-button2") :?> Browser.Types.HTMLButtonElement
let userNameInput = document.querySelector("#username") :?> Browser.Types.HTMLInputElement
let outputHtml = document.querySelector("#output") :?> Browser.Types.HTMLParagraphElement

let apiRoot = "http://localhost:5000/"

let logToHtml msg = outputHtml.innerHTML <- $"{outputHtml.innerHTML}<br>{msg}"

let retryPromise maxRetries beforeRetry f =
    let rec loop retriesRemaining =
        promise {
            try
                return! f()
            with ex ->
                if retriesRemaining > 0 then
                    beforeRetry ex
                    return! loop (retriesRemaining - 1)
                else return raise (Exception($"Still failing after {maxRetries} retries.", ex))
        }
    loop maxRetries

let fetchAllTracks userName from =
    let batchSize = 1000
    let fetchPage page =
        let fetchOne () =
            fetchTracks userName batchSize page from
            |> Promise.map (function
                | Ok ok -> ok
                | Error error -> failwith $"Error while fetching tracks: {error.Response.StatusText} ({error.Response.Status}) - {error.Body}")
        retryPromise 10 (fun ex -> logToHtml $"An error occured: {ex.Message}\nRetrying...") fetchOne
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
    Timestamp = int64 track.date.uts
    Track = track.name
    }

let postScrobbles userName (scrobbles: ScrobbleData[]) =
    promise {
        //let extraCoders = Extra.empty |> (Extra.withCustom <|| dateTimeResolver)
        let jsonBody = Encode.Auto.toString(0, scrobbles, CaseStrategy.CamelCase, Extra.empty |> Extra.withInt64)
        let! result =
            saneFetch
                $"{apiRoot}api/scrobbles/{userName}" [
                    RequestProperties.Method HttpMethod.POST
                    requestHeaders [ HttpRequestHeaders.ContentType "application/json" ]
                    RequestProperties.Body <| unbox(jsonBody)
                    ]
        logToHtml result.Status
        let! responseText = result.text()
        logToHtml responseText
        ()
    }

type ResumeFromInfoJson = Fable.JsonProvider.Generator<"""{ "from": 1420206818 }""">

let getResumeTimestamp userName =
    promise {
        let! result =
            saneFetch
                $"{apiRoot}api/scrobbles/{userName}/resume-from" [
                    requestHeaders [ HttpRequestHeaders.ContentType "application/json" ]
                    ]
        logToHtml result.Status
        let! responseText = result.text()
        logToHtml responseText
        let parsed = ResumeFromInfoJson(responseText)
        return parsed.from |> int64
    }

myButton.onclick <- fun _ ->
    myButton.disabled <- true
    let userName = userNameInput.value
    promise {
        let! from = getResumeTimestamp userName
        let! data = fetchAllTracks userName from
        do!
            data
            |> AsyncSeq.iterAsync (fun tracks ->
                tracks
                |> Array.map (fun track -> mapScrobbleData track)
                |> postScrobbles userName
                |> Async.AwaitPromise)
            |> Async.StartAsPromise
    } |> Promise.start

type GetUserScrobblesJson = Fable.JsonProvider.Generator< """{"time": ["2014-01-19 21:12:14"], "color": ["#e31a1c"], "displayValue": ["bla"]}""" >

let getScrobbleData userName =
    fetchJson $"{apiRoot}api/scrobbles/{userName}" [] GetUserScrobblesJson
    |> Promise.map (function
               | Ok ok -> ok
               | Error error -> failwith $"Error while fetching scrobble data for {userName}: {error.Response.StatusText} ({error.Response.Status}) - {error.Body}")

let chartScrobbleData userName (data: GetUserScrobblesJson) =
    Plotly.plot [
        plot.traces [
            traces.scattergl [
                scattergl.x data.time
                scattergl.y (data.time |> Seq.map (fun x -> $"1970-01-01 {x.Substring(11)}"))
                scattergl.text data.displayValue
                scattergl.hoverinfo [
                    scattergl.hoverinfo.x
                    scattergl.hoverinfo.text
                ]
                scattergl.mode.markers
                scattergl.marker [
                    marker.opacity 0.8
                    marker.size 4
                    marker.color data.color
                ]
            ]
        ]
        plot.layout [
            layout.title $"{userName} - {data.time.Length} scrobbles"
            layout.hovermode.closest
            layout.xaxis [
                xaxis.showgrid false
                xaxis.zeroline true
                xaxis.type'.date
                xaxis.autorange.true'
            ]
            layout.yaxis [
                yaxis.autorange.reversed
                yaxis.showgrid true
                yaxis.type'.date
                yaxis.tickformat "%H:%M"
                yaxis.nticks 24
                yaxis.range [
                    "1970-01-01 00:00:00"
                    "1970-01-02 00:00:00"
                ]
            ]
        ]
        plot.config [
            config.responsive true
            config.autosizable true
            config.fillFrame true
        ]
        plot.style [
        ]
    ]

myButton2.onclick <- fun _ ->
    myButton2.disabled <- true
    let userName = userNameInput.value
    let root = document.getElementById "graph"
    promise {
        let! data = getScrobbleData userName
        let chart = chartScrobbleData userName data
        ReactDOM.render(chart, root)
    } |> Promise.start
