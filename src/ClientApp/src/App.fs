namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.ServerApi
open LastFMStats.Client.LastFmApi
open LastFMStats.Client.Graph
open Browser.Dom
open FSharp.Control
open Fable.Core
open Fetch.Types
open Fetch
open Fable.Core.JsInterop
open ApiModels

module App =

    let scrapeButton =
        document.querySelector ("#scrape-button") :?> Browser.Types.HTMLButtonElement

    let graphButton =
        document.querySelector ("#graph-button") :?> Browser.Types.HTMLButtonElement

    let userNameInput =
        document.querySelector ("#username") :?> Browser.Types.HTMLInputElement

    scrapeButton.onclick <-
        fun _ ->
            scrapeButton.disabled <- true
            let userName = userNameInput.value

            promise {
                let! resumeTimestampResponse = mainApi.getResumeTimestamp (UserName userName) |> Async.StartAsPromise
                let! data = fetchAllTracks userName (resumeTimestampResponse.ResumeFrom)

                do!
                    data
                    |> AsyncSeq.iterAsync
                        (fun tracks ->
                            tracks
                            |> Array.map mapTrackToScrobbleData
                            |> mainApi.insertScrobbles (UserName userName)
                            |> Async.Ignore)
                    |> Async.StartAsPromise
            }
            |> Promise.tap (fun _ -> scrapeButton.disabled <- false)
            |> Promise.start

    graphButton.onclick <-
        fun _ ->
            graphButton.disabled <- true
            let userName = userNameInput.value
            let graph = document.getElementById "graph"

            generateGraph graph userName
            |> Async.tap (fun _ -> graphButton.disabled <- false)
            |> Async.StartImmediate

    async {
        let! result = ServerApi.mainApi.getResumeTimestamp (UserName "yaurthek")
        console.log result
    }
    |> Async.StartImmediate