namespace LastFmStats.Client

open LastFmStats.Client.Util
open LastFmStats.Client.LastFmApi
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

            log.LogDebug $"Resuming fetch from timestamp {result.ResumeFrom} ({DateTimeOffset.FromUnixTimeSeconds(int64 result.ResumeFrom)})"

            return result.ResumeFrom
        }

    let getUserDisplayName (log: ILogger) userName =
        promise {
            log.LogDebug "Querying user name..."
            let! result =
                fetchParse<GetUserNameResponse>
                    $"{apiRoot}api/scrobbles/{userName}/display-name"
                    [ requestHeaders [ ContentType "application/json" ] ]
                |> Promise.map
                    (function
                    | Ok ok -> Some ok.DisplayName
                    | Error error ->
                        match error.Response.Status with
                        | 404 ->
                            log.LogDebug $"User {userName} not found ({JS.JSON.stringify error.Body})"
                            None
                        | _ -> failwith $"Error while querying user name: {error.Response.StatusText} ({error.Response.Status}) - {JS.JSON.stringify error.Body}")
            return result
        }

    type GraphRawQueryParams =
        { userName : string
          color : string option
          timeZone : string option
          startDate : string option
          endDate : string option }

    let loadAllScrobbleData (log: ILogger) graphQueryParams =
        let fetchWithRetry (nextPageToken: float option) =
            let fetchPage () =
                let nextPageTokenQueryParam =
                    match nextPageToken with
                    | None -> ""
                    | Some value -> $"nextPageToken={value}"

                let queryParams =
                    [ "nextPageToken", (nextPageToken |> Option.map string)
                      "userName", (Some graphQueryParams.userName)
                      "color", (graphQueryParams.color)
                      "timeZone", (graphQueryParams.timeZone)
                      "startDate", (graphQueryParams.startDate)
                      "endDate", (graphQueryParams.endDate) ]
                    |> List.choose (fun (k, v) -> v |> Option.map (fun v -> $"{k}={v}"))
                    |> (String.join "&")

                fetchParse<GetChartDataResponse> $"{apiRoot}api/scrobbles/%s{graphQueryParams.userName}?{queryParams}" []
                |> unwrapOrFail

            retryPromise 10 (fun err -> log.LogAlways $"An error occured: {err.Exception.Message}\nRetrying... ({err.NextTryNumber}/{err.MaxRetries})") fetchPage

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
