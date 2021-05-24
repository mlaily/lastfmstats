namespace LastFMStats.Client

open Browser
open Fable.Core

module WebUtils =

    let getLoader (target: Browser.Types.HTMLElement) =
        let loader : Browser.Types.HTMLDivElement = downcast document.createElement "div"
        loader.setAttribute ("style", "position: absolute; margin: auto; top: 0; right: 0; bottom: 0; left: 0; width: 100px; height: 30px;") |> ignore
        let steps = [| "Loading..."; "Loading"; "Loading."; "Loading.." |]
        let mutable currentStep = 0
        let mutable timerToken = 0
        {| enable = fun () ->
            currentStep <- 0
            JS.clearInterval(timerToken)
            timerToken <-
                JS.setInterval (fun() ->
                    loader.innerText <- steps.[currentStep]
                    currentStep <- (currentStep + 1) % steps.Length)
                    250 // ms
            target.appendChild loader |> ignore
           disable = fun () ->
            JS.clearInterval(timerToken)
            target.removeChild loader |> ignore |}

    let getQueryParam paramName = Url.URLSearchParams.Create(window.location.search).get(paramName)
