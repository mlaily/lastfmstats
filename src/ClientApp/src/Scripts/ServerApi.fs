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

    let apiRoot = "/"

    let postScrobbles (log: ILogger) userName (scrobbles: FlatScrobble []) =
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

            log.LogDebug (result.Status.ToString())
            let! responseText = result.text ()
            log.LogDebug responseText
            ()
        }

    let getResumeTimestamp (log: ILogger) userName =
        promise {
            let! result =
                saneFetch
                    $"{apiRoot}api/scrobbles/{userName}/resume-from"
                    [ requestHeaders [ ContentType "application/json" ] ]

            log.LogDebug (result.Status.ToString())
            let! responseText = result.json ()
            let parsed : GetResumeTimestampResponse = unbox responseText
            return parsed.ResumeFrom
        }

    let loadAllScrobbleData (log: ILogger) userName =
        let fetchWithRetry (nextPageToken: int64 option) =
            let fetchPage () =
                let nextPageTokenQueryParam =
                    match nextPageToken with
                    | None -> ""
                    | Some value -> $"nextPageToken={value}"

                fetchParse<GetChartDataResponse> $"{apiRoot}api/scrobbles/%s{userName}?{nextPageTokenQueryParam}" []
                |> unwrapOrFail

            retryPromise 10 (fun ex -> log.LogAlways $"An error occured: {ex.Message}\nRetrying...") fetchPage

        let mutable currentCount = 0
        let rec loadAllScrobbleData' nextPageToken =
            asyncSeq {
                let! data = fetchWithRetry nextPageToken |> Async.AwaitPromise
                yield data
                currentCount <- currentCount + data.Timestamps.Length
                if currentCount < data.TotalCount then
                    yield! loadAllScrobbleData' (Some(data.NextPageToken))
            }

        loadAllScrobbleData' None
