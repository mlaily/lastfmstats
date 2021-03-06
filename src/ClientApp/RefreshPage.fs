namespace LastFmStats.Client

open LastFmStats.Client.Util
open LastFmStats.Client.LastFmApi
open LastFmStats.Client.ServerApi
open Browser.Dom
open FSharp.Control
open Fable.Core
open Browser
open Browser.Types

module RefreshPage =

    let queryButton = document.querySelector ("#query-button") :?> Browser.Types.HTMLButtonElement
    let userNameInput = document.querySelector ("#userName") :?> Browser.Types.HTMLInputElement
    let outputDiv = document.querySelector ("#output") :?> Browser.Types.HTMLParagraphElement

    let logRawHtml html = outputDiv.appendChild html |> ignore
    let initializeLogNode (element: HTMLElement) style =
        element.classList.add "log"
        element.setAttribute("style", style)
        element
    let createLogNode msg style =
        let element : Browser.Types.HTMLParagraphElement = downcast document.createElement "p"
        element.innerText <- msg
        initializeLogNode element style
    let outputLogger =
        { new ILogger with
            member this.LogAlways msg = logRawHtml (createLogNode msg "color: black")
            member this.LogWarning msg = logRawHtml (createLogNode msg "color: orangered")
            member this.LogDebug msg = logRawHtml (createLogNode msg "color: gray") }

    userNameInput.focus()

    userNameInput.onkeyup <-
        fun e ->
            if e.key = "Enter" then
                queryButton.click()
                e.preventDefault()

    queryButton.onclick <-
        fun _ ->
            queryButton.disabled <- true
            let mutable userName = userNameInput.value
            outputDiv.innerHTML <- ""
            promise {
                let! from = getResumeTimestamp outputLogger userName
                let! data = fetchAllTracks outputLogger userName (int64 from)
                do!
                    data
                    |> AsyncSeq.iterAsync
                        (fun (userDisplayName, tracks) ->
                            userName <- userDisplayName // use the correct display name when we have it (with the proper casing)
                            tracks
                            |> Array.map mapTrackToScrobbleData
                            |> postScrobbles outputLogger userName
                            |> Async.AwaitPromise
                            |> Async.Ignore)
                    |> Async.StartAsPromise }
            |> Promise.catch (fun x ->
                outputLogger.LogWarning $"Aborted! - {x.Message}"
                queryButton.disabled <- false)
            |> Promise.tap (fun _ ->
                outputLogger.LogAlways "The End."
                let graphLink : Browser.Types.HTMLAnchorElement = downcast document.createElement "a"
                graphLink.text <- $"Go to {userName}'s graph page"
                graphLink.href <- $"Graph?userName={userName}"
                logRawHtml (initializeLogNode graphLink "")
                queryButton.disabled <- false)
            |> Promise.start
