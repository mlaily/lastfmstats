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
                let! from = getResumeTimestamp userName
                let! data = fetchAllTracks userName from

                do!
                    data
                    |> AsyncSeq.iterAsync
                        (fun tracks ->
                            tracks
                            |> Array.map mapScrobbleData
                            |> postScrobbles userName
                            |> Async.AwaitPromise)
                    |> Async.StartAsPromise
            }
            |> Promise.start

    graphButton.onclick <-
        fun _ ->
            graphButton.disabled <- true
            let userName = userNameInput.value
            let graph = document.getElementById "graph"
            generateGraph graph userName
            |> Async.Start
