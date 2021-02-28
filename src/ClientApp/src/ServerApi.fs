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

    let apiRoot = "http://localhost:5000/"

    let postScrobbles userName (scrobbles: ScrobbleData []) =
        promise {
            //let extraCoders = Extra.empty |> (Extra.withCustom <|| dateTimeResolver)
            let jsonBody =
                Encode.Auto.toString (0, scrobbles, CaseStrategy.CamelCase, Extra.empty |> Extra.withInt64)

            let! result =
                saneFetch
                    $"{apiRoot}api/scrobbles/{userName}"
                    [ RequestProperties.Method HttpMethod.POST
                      requestHeaders [ HttpRequestHeaders.ContentType "application/json" ]
                      RequestProperties.Body <| unbox (jsonBody) ]

            log result.Status
            let! responseText = result.text ()
            log responseText
            ()
        }

    type ResumeFromInfoJson = Fable.JsonProvider.Generator<"""{ "from": 1420206818 }""">

    let getResumeTimestamp userName =
        promise {
            let! result =
                saneFetch
                    $"{apiRoot}api/scrobbles/{userName}/resume-from"
                    [ requestHeaders [ HttpRequestHeaders.ContentType "application/json" ] ]

            log result.Status
            let! responseText = result.text ()
            log responseText
            let parsed = ResumeFromInfoJson(responseText)
            return parsed.from |> int64
        }


    type GetUserScrobblesJson =
        Fable.JsonProvider.Generator<"""{"time": ["2021-02-24 22:09:15"], "color": ["#e31a1c"], "displayValue": ["bla"], "totalCount": 123, "nextPageToken": 1614200955}""">

    let loadAllScrobbleData userName =
        let fetchPage (nextPageToken: int64 option) =
            let fetchOne () =
                let nextPageTokenQueryParam = match nextPageToken with | None -> "" | Some value -> $"nextPageToken={value}"
                fetchJson $"{apiRoot}api/scrobbles/%s{userName}?{nextPageTokenQueryParam}" [] GetUserScrobblesJson
                |> Promise.map
                    (function
                    | Ok ok -> ok
                    | Error error ->
                        failwith
                            $"Error while fetching scrobble data for {userName}: {error.Response.StatusText} ({error.Response.Status}) - {error.Body}")
            retryPromise 10 (fun ex -> log $"An error occured: {ex.Message}\nRetrying...") fetchOne

        let rec loadAllScrobbleData' nextPageToken =
            asyncSeq {
                let! data = fetchPage nextPageToken |> Async.AwaitPromise

                yield data

                if data.time.Length > 0 && data.nextPageToken > 0. then
                    yield! loadAllScrobbleData' (Some(data.nextPageToken |> int64))
            }

        loadAllScrobbleData' None