namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.LastFmApi
open LastFMStats.Client.ServerApi
open Browser.Dom
open FSharp.Control
open Fable.Core
open Browser

module RefreshPage =

    let queryButton = document.querySelector ("#query-button") :?> Browser.Types.HTMLButtonElement
    let userNameInput = document.querySelector ("#userName") :?> Browser.Types.HTMLInputElement
    let outputDiv = document.querySelector ("#output") :?> Browser.Types.HTMLParagraphElement

    let outputLogger =
        let createElement msg style =
            let element : Browser.Types.HTMLParagraphElement = downcast document.createElement "p"
            element.innerText <- msg
            element.className <- "log"
            element.setAttribute("style", style)
            element
        { new ILogger with
            member this.LogAlways msg = outputDiv.appendChild (createElement msg "color: black") |> ignore
            member this.LogWarning msg = outputDiv.appendChild (createElement msg "color: orangered") |> ignore
            member this.LogDebug msg = outputDiv.appendChild (createElement msg "color: gray") |> ignore }

    userNameInput.onkeyup <-
        fun e ->
            if e.key = "Enter" then
                queryButton.click()
                e.preventDefault()

    queryButton.onclick <-
        fun _ ->
            queryButton.disabled <- true
            let userName = userNameInput.value

            promise {
                let! from = getResumeTimestamp outputLogger userName
                let! data = fetchAllTracks outputLogger userName (int64 from)

                do!
                    data
                    |> AsyncSeq.iterAsync
                        (fun tracks ->
                            tracks
                            |> Array.map mapTrackToScrobbleData
                            |> postScrobbles outputLogger userName
                            |> Async.AwaitPromise
                            |> Async.Ignore)
                    |> Async.StartAsPromise
            }
            |> Promise.catch (fun x ->
                outputLogger.LogWarning $"Aborted! - {x.Message}"
                queryButton.disabled <- false)
            |> Promise.tap (fun _ ->
                outputLogger.LogAlways "The End."
                queryButton.disabled <- false)
            |> Promise.start
