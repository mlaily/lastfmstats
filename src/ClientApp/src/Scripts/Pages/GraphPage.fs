namespace LastFMStats.Client.Pages

open LastFMStats.Client.Util
open LastFMStats.Client.ServerApi
open Fable.Core.JsInterop
open FSharp.Control
open Browser.Dom
open Fable.Core
open Browser
open LastFMStats.Client

module GraphPage =

    /// Styles defined on the page matching classes.
    let pageStyleClasses = {|
        maximizeHeight = "maximizeHeight"
        maximizeSize = "maximizeSize"
    |}
    let queryForm = document.getElementById "queryForm"
    let userNameInput = document.querySelector ("#userName") :?> Browser.Types.HTMLInputElement

    let graph = document.getElementById "graph"
    let graphLoader = WebUtils.getLoader graph

    let colorSelect = document.getElementById "color" :?> Browser.Types.HTMLSelectElement
    let timeZoneSelect = document.getElementById "timeZone" :?> Browser.Types.HTMLSelectElement

    let defaultUserName = WebUtils.getUserNameFromQueryParams()

    let initializeColors () =
        promise {
            let! allColors = getColors (ConsoleLogger.Default)
            for color in allColors do
                let selectOption : Browser.Types.HTMLOptionElement = downcast document.createElement "option"
                selectOption.id <- string color
                selectOption.text <- string color
                colorSelect.add selectOption
        }

    let initializeTimeZones () =
        promise {
            let! tzResponse = getTimeZones (ConsoleLogger.Default)
            for tz in tzResponse.TimeZones do
                let selectOption : Browser.Types.HTMLOptionElement = downcast document.createElement "option"
                selectOption.id <- tz.Id
                selectOption.text <- string tz.DisplayName
                timeZoneSelect.add selectOption
                if tz = tzResponse.Default
                then selectOption.selected <- true
        }

    initializeColors () |> Promise.start
    initializeTimeZones () |> Promise.start

    let plotly : obj = window?Plotly

    let generateGraph graph userName =
        loadAllScrobbleData (ConsoleLogger.Default) userName
        |> AsyncSeq.indexed
        |> AsyncSeq.iterAsync
            (fun (pageIndex, pageData) ->
                async {
                    let x = pageData.Timestamps
                    let y = pageData.Timestamps |> Array.map (fun x -> "1970-01-01 " + x.Substring(11))
                    let text = pageData.Texts
                    let color = pageData.Colors

                    if pageIndex = 0L then
                        let traces =
                            [| {| x = x
                                  y = y
                                  text = text
                                  ``type`` = "scattergl"
                                  mode = "markers"
                                  marker =
                                      {| opacity = 0.8
                                         size = 4
                                         color = color |}
                                  hovertemplate = "%{x|%a %Y-%m-%d %H:%M:%S}<br>%{text}<extra></extra>" |} |]

                        let layout =
                            {| title = $"{userName} - {pageData.TotalCount} scrobbles"
                               hovermode = "closest"
                               xaxis =
                                   {| showgrid = false
                                      zeroline = true
                                      ``type`` = "date"
                                      autorange = true |}
                               yaxis =
                                   {| autorange = "reversed"
                                      showgrid = true
                                      ``type`` = "date"
                                      tickformat = "%H:%M"
                                      ntick = 24
                                      range =
                                          [| "1970-01-01 00:00:00"
                                             "1970-01-02 00:00:00" |] |} |}

                        let config =
                            {| responsive = true
                               autosizeable = true |}

                        let p : JS.Promise<obj> = plotly?newPlot (graph, traces, layout, config)
                        do! p |> Async.AwaitPromise |> Async.Ignore
                    else // page index > 0, meaning we need to add the new data to an existing chart
                        let update =
                            {| x = [| x |]
                               y = [| y |]
                               text = [| text |]
                               ``marker.color`` = [| color |] |}
                        let traceIndices = [| 0 |]
                        let p : JS.Promise<obj> = plotly?extendTraces (graph, update, traceIndices)
                        do! p |> Async.AwaitPromise |> Async.Ignore
                })

    match defaultUserName with
    | None -> graph.hidden <- true
    | Some userName ->
        userNameInput.value <- userName
        document.documentElement.className <- pageStyleClasses.maximizeHeight
        document.body.className <- pageStyleClasses.maximizeHeight
        graph.className <- pageStyleClasses.maximizeSize
        graphLoader.enable()
        generateGraph graph userName
        |> Async.tap (fun _ -> graphLoader.disable())
        |> Async.StartImmediate