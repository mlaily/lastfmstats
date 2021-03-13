namespace ServerApp.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open ServerApp
open LastFmStatsServer
open ApiModels

type PostScrobbleOk =
    { SavedCount: int }
type PostScrobbleResult =
    | Ok of PostScrobbleOk
    | Error of string

[<ApiController>]
[<Route("api/scrobbles")>]
type ScrobbleController (mainContext: MainContext, logger : ILogger<ScrobbleController>) =
    inherit ControllerBase()

    let normalizeEmpty value =
        if String.IsNullOrWhiteSpace(value) then ""
        else value.Trim()

    let normalizeUserName userName =
        let safeValue = normalizeEmpty userName
        safeValue.ToLowerInvariant()

    [<HttpGet("{userName}/resume-from")>]
    member this.GetResumeTimestamp(userName: string) =
        let lastKnownTimestamp =
            query {
                for scrobble in mainContext.Scrobbles do
                    where (scrobble.User.Name = normalizeUserName userName)
                    sortBy scrobble.Timestamp
                    select scrobble.Timestamp
                    lastOrDefault
            }

        // Last.fm allows uploading scrobbles up to two weeks back in time, I think...
        let backInTimeSeconds = TimeSpan.FromDays(30.).TotalSeconds |> int64
        let fromValue = lastKnownTimestamp - backInTimeSeconds
        if fromValue < 0L
        then {| from = 0L |}
        else {| from = fromValue |}

    [<HttpPost("{userName}")>]
    member this.PostScrobbles(userName: string, data: ScrobbleData[]) =
        if isNull data then Ok { SavedCount = 0 }
        else if data.Length > 1000 then Error "Too much data. Stay at or below 1000"
        else Ok { SavedCount = 42 }
        