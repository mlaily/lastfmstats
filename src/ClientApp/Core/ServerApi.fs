namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.LastFmApi
open ApiModels
open Thoth.Json
open Fetch.Types
open Fetch
open FSharp.Control
open Fable.Core
open System

module ServerApi =

    let apiRoot = "/"

    let postScrobbles (log: ILogger) userName (scrobbles: FlatScrobble []) =
        promise {
            //let extraCoders = Extra.empty |> Extra.withInt64 |> (Extra.withCustom <|| dateTimeResolver)
            let jsonBody =
                Encode.Auto.toString (0, scrobbles, CaseStrategy.CamelCase, Extra.empty)

            log.LogDebug $"Saving {scrobbles.Length} scrobbles..."

            let! result =
                fetchParse<InsertScrobblesResponse>
                    $"{apiRoot}api/scrobbles/{userName}"
                    [ Method HttpMethod.POST
                      requestHeaders [ ContentType "application/json" ]
                      Body <| unbox (jsonBody) ]
                |> unwrapOrFail

            log.LogAlways $"Actually saved after deduplication: {JS.JSON.stringify result}"
            return result
        }

    let getResumeTimestamp (log: ILogger) userName =
        promise {
            let! result =
                fetchParse<GetResumeTimestampResponse>
                    $"{apiRoot}api/scrobbles/{userName}/resume-from"
                    [ requestHeaders [ ContentType "application/json" ] ]
                |> unwrapOrFail

            log.LogDebug $"Resuming fetching from timestamp {result.ResumeFrom} ({DateTimeOffset.FromUnixTimeSeconds(int64 result.ResumeFrom)})"

            return result.ResumeFrom
        }

    let loadAllScrobbleData (log: ILogger) userName =
        let fetchWithRetry (nextPageToken: float option) =
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

    let getColors (log: ILogger) =
        promise {
            let! result =
                fetchParse<ColorChoice[]>
                    $"{apiRoot}api/colors"
                    [ requestHeaders [ ContentType "application/json" ] ]
                |> unwrapOrFail

            return result
        }

    let getTimeZones (log: ILogger) =
        promise {
            let! result =
                fetchParse<TimeZonesResponse>
                    $"{apiRoot}api/timezones"
                    [ requestHeaders [ ContentType "application/json" ] ]
                |> unwrapOrFail

            return result
        }