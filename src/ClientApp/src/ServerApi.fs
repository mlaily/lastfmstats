namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.LastFmApi
open ApiModels
open Thoth.Json
open Fetch.Types
open Fetch
open FSharp.Control
open Fable.Core

module ServerApi =

    type ResumeFromInfoJson = Fable.JsonProvider.Generator<"""{ "from": 1420206818 }""">

    type GetUserScrobblesJson =
        Fable.JsonProvider.Generator<"""{"time": ["2021-02-24 22:09:15"], "color": ["#e31a1c"], "displayValue": ["bla"], "totalCount": 123, "nextPageToken": 1614200955}""">

    let apiRoot = "http://localhost:5000/"

    let postScrobbles userName (scrobbles: ScrobbleData []) =
        promise {
            //let extraCoders = Extra.empty |> (Extra.withCustom <|| dateTimeResolver)
            let jsonBody =
                Encode.Auto.toString (0, scrobbles, CaseStrategy.CamelCase, Extra.empty |> Extra.withInt64)

            let! result =
                saneFetch
                    $"{apiRoot}api/scrobbles/{userName}"
                    [ Method HttpMethod.POST
                      requestHeaders [ ContentType "application/json" ]
                      Body <| unbox (jsonBody) ]

            log result.Status
            let! responseText = result.text ()
            log responseText
            ()
        }

    let getResumeTimestamp userName =
        promise {
            let! result =
                saneFetch
                    $"{apiRoot}api/scrobbles/{userName}/resume-from"
                    [ requestHeaders [ ContentType "application/json" ] ]

            log result.Status
            let! responseText = result.text ()
            log responseText
            let parsed = ResumeFromInfoJson(responseText)
            return parsed.from |> int64
        }

    let loadAllScrobbleData userName =
        let fetchWithRetry (nextPageToken: int64 option) =
            let fetchPage () =
                let nextPageTokenQueryParam =
                    match nextPageToken with
                    | None -> ""
                    | Some value -> $"nextPageToken={value}"

                fetchParse $"{apiRoot}api/scrobbles/%s{userName}?{nextPageTokenQueryParam}" [] GetUserScrobblesJson
                |> unwrapOrFail

            retryPromise 10 (fun ex -> log $"An error occured: {ex.Message}\nRetrying...") fetchPage

        let rec loadAllScrobbleData' nextPageToken =
            asyncSeq {
                let! data = fetchWithRetry nextPageToken |> Async.AwaitPromise

                yield data

                if data.time.Length > 0 && data.nextPageToken > 0. then
                    yield! loadAllScrobbleData' (Some(data.nextPageToken |> int64))
            }

        loadAllScrobbleData' None
