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
                let traces =
                    [| {| x = pageData.time
                          y =
                              pageData.time
                              |> Array.map (fun x -> $"1970-01-01 {x.Substring(11)}")
                          text = pageData.displayValue
                          ``type`` = "scattergl"
                          mode = "markers"
                          marker =
                              {| opacity = 0.8
                                 size = 4
                                 color = pageData.color |}
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

                if pageIndex = 0L then
                    plotly?plot (graph, traces, layout, config)
                else
                    let indices = traces |> Array.mapi (fun i trace -> i)

                    let update =
                        {| x = traces |> Array.map (fun x -> x.x)
                           y = traces |> Array.map (fun x -> x.y)
                           text = traces |> Array.map (fun x -> x.text)
                           ``marker.color`` = traces |> Array.map (fun x -> x.marker.color) |}

                    plotly?extendTraces (graph, update, indices))
