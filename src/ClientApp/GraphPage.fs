namespace LastFmStats.Client

open LastFmStats.Client.Util
open LastFmStats.Client.ServerApi
open Fable.Core.JsInterop
open FSharp.Control
open Browser.Dom
open Fable.Core
open Browser
open ApiModels
open System

module GraphPage =

    /// Styles defined on the page matching classes.
    let pageStyleClasses = {|
        maximizeHeight = "maximizeHeight"
        maximizeSize = "maximizeSize"
        error = "error"
    |}

    let graph = document.getElementById "graph"
    let graphLoader = WebUtils.getLoader graph 

    let plotly : obj = window?Plotly

    let parseQueryParams () : JS.Promise<Result<GraphRawQueryParams, string> option> =
        promise {
            match WebUtils.getQueryParam "userName" with
            | None -> return None
            | Some userName ->
                let! userDisplayName = ServerApi.getUserDisplayName (ConsoleLogger.Default) userName
                match userDisplayName with
                | None -> return Some (Error $"User '{userName}' not found!") // user not found
                | Some userDisplayName -> 
                    // Not much validation for now...
                    return Some (Ok { userName = userDisplayName
                                      color = WebUtils.getQueryParam "color"
                                      timeZone = WebUtils.getQueryParam "timeZone"
                                      startDate = WebUtils.getQueryParam "startDate"
                                      endDate = WebUtils.getQueryParam "endDate" })
        }

    let generateGraph graph graphQueryParams =
        loadAllScrobbleData (ConsoleLogger.Default) graphQueryParams
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
                            {| title = $"{graphQueryParams.userName} - {pageData.TotalCount} scrobbles"
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

    promise {
        match! parseQueryParams() with
        | None ->
            graph.hidden <- true
        | Some (Error msg) ->
            graph.innerText <- $"Error. {msg}"
            graph.className <- pageStyleClasses.error
        | Some (Ok graphQueryParams) ->
            document.title <- $"{graphQueryParams.userName}'s graph"
            document.documentElement.className <- pageStyleClasses.maximizeHeight
            document.body.className <- pageStyleClasses.maximizeHeight
            graph.className <- pageStyleClasses.maximizeSize
            graphLoader.enable()
            do! generateGraph graph graphQueryParams
                |> Async.StartAsPromise
                |> Promise.tap (fun _ -> graphLoader.disable())
    } |> Async.AwaitPromise |> Async.StartImmediate