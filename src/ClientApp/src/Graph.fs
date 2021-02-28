namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.ServerApi
open Feliz.Plotly
open Fable.Core.JsInterop
open FSharp.Control

module Graph =

    let plotly: obj = importAll "plotly.js/dist/plotly"

    let getUserChart userName (data: GetUserScrobblesJson) =
        {| traces =
               [ traces.scattergl [ scattergl.x data.time
                                    scattergl.y (
                                        data.time
                                        |> Seq.map (fun x -> $"1970-01-01 {x.Substring(11)}")
                                    )
                                    scattergl.text data.displayValue
                                    scattergl.hoverinfo [ scattergl.hoverinfo.x
                                                          scattergl.hoverinfo.text ]
                                    scattergl.mode.markers
                                    scattergl.marker [ marker.opacity 0.8
                                                       marker.size 4
                                                       marker.color data.color ] ] ]
           layout =
               [ layout.title $"{userName} - {data.totalCount} scrobbles"
                 layout.hovermode.closest
                 layout.xaxis [ xaxis.showgrid false
                                xaxis.zeroline true
                                xaxis.type'.date
                                xaxis.autorange.true' ]
                 layout.yaxis [ yaxis.autorange.reversed
                                yaxis.showgrid true
                                yaxis.type'.date
                                yaxis.tickformat "%H:%M"
                                yaxis.nticks 24
                                yaxis.range [ "1970-01-01 00:00:00"
                                              "1970-01-02 00:00:00" ] ] ]
           config =
               [ config.responsive true
                 config.autosizable true
                 config.fillFrame true ] |}

    let generateGraph graph userName =
        loadAllScrobbleData userName
        |> AsyncSeq.indexed
        |> AsyncSeq.iter
            (fun (i, data) ->
                let chart = getUserChart userName data

                let jsTraces =
                    (chart.traces
                     |> plot.traces
                     |> Bindings.getKV
                     |> snd)

                let jsLayout =
                    (chart.layout
                     |> plot.layout
                     |> Bindings.getKV
                     |> snd)

                let jsConfig =
                    (chart.config
                     |> plot.config
                     |> Bindings.getKV
                     |> snd)

                if i = 0L then
                    plotly?plot (graph, jsTraces, jsLayout, jsConfig)
                else
                    let cast =
                        jsTraces
                        |> unbox<{| x: string array
                                    y: string array
                                    text: string array
                                    marker: {| color: string array |} |} array>

                    let indices = cast |> Array.mapi (fun i trace -> i)

                    let update =
                        {| x = cast |> Array.map (fun trace -> trace.x)
                           y = cast |> Array.map (fun trace -> trace.y)
                           text = cast |> Array.map (fun trace -> trace.text)
                           ``marker.color`` =
                               cast
                               |> Array.map (fun trace -> trace.marker.color) |}

                    plotly?extendTraces (graph, update, indices))
