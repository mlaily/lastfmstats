namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.ServerApi
open Fable.Core.JsInterop
open FSharp.Control
open Browser.Dom

module Graph =

    let plotly: obj = window?Plotly //importAll "plotly.js/dist/plotly"

    // [<Emit("new Date($0).toISOString()")>]
    // let convertTimestampToISOString timestamp : string = jsNative

    let generateGraph graph userName =
        loadAllScrobbleData userName
        |> AsyncSeq.indexed
        |> AsyncSeq.iter
            (fun (pageIndex, pageData) ->
            let traces = 
                [|
                    {|
                        x = pageData.time
                        y = pageData.time |> Array.map (fun x -> $"1970-01-01 {x.Substring(11)}")
                            // t = unix time
                            // second = t MOD 60  
                            // minute = INT(t / 60) MOD 60  
                            // hour = INT(t / 60 / 60) MOD 24  
                            // days = INT(t / 60 / 60 / 24)  
                            // years = INT(days / 365.25)  
                            // year = 1970 + years + 1
                            // FIXME: it still does not work because plotly messes with the timezone TT
                            // let v = (x - Math.Floor(x / 1000.0 / 60.0 / 60.0 / 24.0) * 86400000.0)
                            // plotly fails to properly parse timestamps, so we have to manually convert them to a js Date
                            // convertTimestampToISOString v)
                        text = pageData.displayValue
                        ``type`` = "scattergl"
                        mode = "markers"
                        marker =
                            {|
                                opacity = 0.8
                                size = 4
                                color = pageData.color
                            |}
                        hovertemplate = "%{x|%a %Y-%m-%d %H:%M:%S}<br>%{text}<extra></extra>"
                    |}
                |]
            let layout =
                {|
                    title = $"{userName} - {pageData.totalCount} scrobbles"
                    hovermode = "closest"
                    xaxis =
                        {|
                            showgrid = false
                            zeroline = true
                            ``type`` = "date"
                            autorange = true
                        |}
                    yaxis =
                        {|
                            autorange = "reversed"
                            showgrid = true
                            ``type`` = "date"
                            tickformat = "%H:%M"
                            ntick = 24
                            range = [| "1970-01-01 00:00:00"; "1970-01-02 00:00:00" |]
                        |}
                |}
            let config =
                {|
                    responsive = true
                    autosizeable = true
                    fillFrame = true
                |}
            if pageIndex = 0L then
                plotly?plot (graph, traces, layout, config)
            else
                let indices = traces |> Array.mapi (fun i trace -> i)
                let update =
                    {|
                        x = traces |> Array.map (fun x -> x.x)
                        y = traces |> Array.map (fun x -> x.y)
                        text = traces |> Array.map (fun x -> x.text)
                        ``marker.color`` = traces |> Array.map (fun x -> x.marker.color)
                    |}

                plotly?extendTraces(graph, update, indices))
