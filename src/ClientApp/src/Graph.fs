namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.ServerApi
open Fable.Core.JsInterop
open FSharp.Control
open Browser.Dom

module Graph =

    let plotly : obj = window?Plotly

    let generateGraph graph userName =
        loadAllScrobbleData userName
        |> AsyncSeq.indexed
        |> AsyncSeq.iter
            (fun (pageIndex, pageData) ->
                let x = pageData.time
                let y = pageData.time |> Array.map (fun x -> $"1970-01-01 {x.Substring(11)}")
                let text = pageData.displayValue
                let color = pageData.color

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
                        {| title = $"{userName} - {pageData.totalCount} scrobbles"
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
                           autosizeable = true
                           fillFrame = true |}

                    plotly?plot (graph, traces, layout, config)
                else // page index > 0
                    let update =
                        {| x = [| x |]
                           y = [| y |]
                           text = [| text |]
                           ``marker.color`` = [| color |] |}
                    let traceIndices = [| 0 |]
                    plotly?extendTraces (graph, update, traceIndices))
