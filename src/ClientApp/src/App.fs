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
// open Fable.Core.DynamicExtensions

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
                            |> Array.map mapTrackToScrobbleData
                            |> postScrobbles userName
                            |> Async.AwaitPromise)
                    |> Async.StartAsPromise
            }
            |> Promise.tap (fun _ -> scrapeButton.disabled <- false)
            |> Promise.start

    graphButton.onclick <-
        fun _ ->
            graphButton.disabled <- true
            let userName = userNameInput.value
            let graph = document.getElementById "graph"

            generateGraph graph userName
            |> Async.tap (fun _ -> graphButton.disabled <- false)
            |> Async.StartImmediate

    // [<Emit("""{"list": [ "a", "b", "c"], "time": [123456, 7891011120], "testObj": {"a": 42, "b": 43}}""")>]
    // let rawJson : obj = jsNative

    // type MyType = {
    //     list: string[]
    //     time: int64 []
    //     testObj: Hash<int>
    // }

    // let testStr = Fable.Core.JS.JSON.stringify rawJson
    // console.log testStr

    // let testUnbox : MyType = unbox rawJson

    // console.log (testUnbox.testObj.Keys() |> Array.map (fun x -> x + "cool!"))
    // console.log (testUnbox.testObj.Values()|> Array.map (fun x -> x*10))
    // console.log (testUnbox.testObj.Entries()|> Array.map (fun (k,v) -> $"{k}={v}"))
    // console.log (testUnbox.testObj.HasKey "a")
    // console.log (testUnbox.testObj.HasKey "ab")
    // console.log (testUnbox.testObj.HasKey "toString")

    // testUnbox.testObj.Clear()

    // console.log (testUnbox.testObj.Keys() |> Array.map (fun x -> x + "cool!"))
    // console.log (testUnbox.testObj.Values()|> Array.map (fun x -> x*10))
    // console.log (testUnbox.testObj.Entries()|> Array.map (fun (k,v) -> $"{k}={v}"))
    // console.log (testUnbox.testObj.HasKey "a")
    // console.log (testUnbox.testObj.HasKey "ab")
    // console.log (testUnbox.testObj.HasKey "toString")

    // let access = testUnbox.list.Length