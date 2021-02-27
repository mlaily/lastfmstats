module App

open Browser.Dom
open FSharp.Control
open Fable.Core
open Fetch.Types
open Fetch
open ApiModels
open System
open Thoth.Json
open Feliz.Plotly
open Fable.Core.JsInterop


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

type GetUserScrobblesJson = Fable.JsonProvider.Generator< """{"time": [1234567890], "color": ["#e31a1c"], "displayValue": ["bla"]}""" >

let getScrobbleData userName fromTimestamp =
    fetchJson $"{apiRoot}api/scrobbles/{userName}?fromTimestamp=%d{fromTimestamp}" [] GetUserScrobblesJson
    // |> Promise.map (function
    //            | Ok ok -> ok
    //            | Error error -> failwith $"Error while fetching scrobble data for {userName}: {error.Response.StatusText} ({error.Response.Status}) - {error.Body}")

let loadAllScrobbleData userName =
    let fetchPage fromTimestamp =
        let fetchOne () =
            getScrobbleData userName fromTimestamp
            |> Promise.map (function
                | Ok ok -> ok
                | Error error -> failwith $"Error while fetching scrobble data for {userName}: {error.Response.StatusText} ({error.Response.Status}) - {error.Body}")
        retryPromise 10 (fun ex -> logToHtml $"An error occured: {ex.Message}\nRetrying...") fetchOne
    let rec loadAllScrobbleData' fromTimestamp =
        asyncSeq {
            let! data = fetchPage fromTimestamp |> Async.AwaitPromise
            let oldestTimestamp = data.time |> Seq.min |> int64

            yield data

            if data.time.Length > 0 && oldestTimestamp > 0L
            then yield! loadAllScrobbleData' oldestTimestamp
            else logToHtml "end"
        }
    loadAllScrobbleData' Int64.MaxValue

let chartScrobbleData userName (data: GetUserScrobblesJson) =
    {|
        traces =
            [
                traces.scattergl [
                    scattergl.x (data.time |> Seq.map (fun x -> $"""{DateTimeOffset.FromUnixTimeSeconds(x |> int64).ToString("yyyy-MM-dd HH:mm:ss")}"""))
                    scattergl.y (data.time |> Seq.map (fun x -> $"""1970-01-01 {DateTimeOffset.FromUnixTimeSeconds(x |> int64).ToString("HH:mm:ss")}"""))
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
        layout =
            [
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
        config =
            [
                config.responsive true
                config.autosizable true
                config.fillFrame true
            ]
    |}

// Plotly.extendTraces(graph, {x: [["2021-03-20 10:10:10"]], y: [["1970-01-01 10:10:10"]]}, [0])
//Feliz.Plotly.Bindings.plotly.

let plotly: obj = importAll "plotly.js-dist"
window?plotly <- plotly

myButton2.onclick <- fun _ ->
    myButton2.disabled <- true
    let userName = userNameInput.value
    let graph = document.getElementById "graph"
    loadAllScrobbleData userName
    |> AsyncSeq.indexed
    |> AsyncSeq.iter (fun (i, data) ->
        let chart = chartScrobbleData userName data
        let jsTraces = (chart.traces |> plot.traces |> Bindings.getKV |> snd)
        let jsLayout = (chart.layout |> plot.layout |> Bindings.getKV |> snd)
        let jsConfig = (chart.config |> plot.config |> Bindings.getKV |> snd) 
        if i = 0L
        then
            plotly?plot(graph, jsTraces, jsLayout, jsConfig)
        else
            let cast = jsTraces |> unbox<{| x: string array; y: string array; text: string array; marker: {| color: string array |} |} array>
            let indices = cast |> Array.mapi (fun i trace -> i)
            let update =
                {|
                    x = cast |> Array.map (fun trace -> trace.x)
                    y = cast |> Array.map (fun trace -> trace.y)
                    text = cast |> Array.map (fun trace -> trace.text)
                    ``marker.color`` = cast |> Array.map (fun trace -> trace.marker.color)
                |}
            plotly?extendTraces(graph, update, indices))
    |> Async.Start
