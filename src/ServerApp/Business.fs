module Business

open LastFmStatsServer
open Utils
open System
open ApiModels
open Microsoft.AspNetCore.Http
open Giraffe
open Microsoft.AspNetCore.Http
open LastFmStatsServer
open Microsoft.AspNetCore.Http

/// Business error object. Should not be used for unexpected errors (exceptions should flow)
type ErrorInfo<'Detail> =
    {
        Message: string
        Detail: 'Detail option
    }

let getResumeTimestamp userName (ctx: HttpContext) =
    let mainContext = ctx.GetService<MainContext>()
    let lastKnownTimestamp =
        query {
            for scrobble in mainContext.Scrobbles do
                where (scrobble.User.Name = normalizeUserName userName)
                sortBy scrobble.Timestamp
                select scrobble.Timestamp
                lastOrDefault
        }
    // Last.fm allows uploading scrobbles up to two weeks back in time, I think,
    // so one month back should be more than enough to be safe...
    let backInTimeSeconds = TimeSpan.FromDays(30.).TotalSeconds |> int64
    let fromValue = lastKnownTimestamp - backInTimeSeconds
    //Error { Message = "error"; Detail = None}
    if fromValue < 0L
    then Ok {| From = 0L |}
    else Ok {| From = fromValue |}

let insertScrobbleData userName (data: ScrobbleData[]) (ctx: HttpContext) =
    {| SavedCount = 0 |}


let mainApi (context: HttpContext) : IMainApi = {
    getResumeTimestamp = fun userName ->
        async {
            let mainContext = context.GetService<MainContext>()
            let lastKnownTimestamp =
                query {
                    for scrobble in mainContext.Scrobbles do
                        where (scrobble.User.Name = normalizeUserName userName)
                        sortBy scrobble.Timestamp
                        select scrobble.Timestamp
                        lastOrDefault
                }
            // Last.fm allows uploading scrobbles up to two weeks back in time, I think,
            // so one month back should be more than enough to be safe...
            let backInTimeSeconds = TimeSpan.FromDays(30.).TotalSeconds |> int64
            let fromValue = lastKnownTimestamp - backInTimeSeconds
            //Error { Message = "error"; Detail = None}
            if fromValue < 0L
            then return {| From = 0L |}
            else return {| From = fromValue |}
        }
}