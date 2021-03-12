namespace LastFMStats.Client

open System
open Browser.Dom
open Fetch

module Util =

    type FetchResponse = { Response: Response; Body: string }

    let log msg = console.log msg

    let retryPromise maxRetries beforeRetry f =
        let rec loop retriesRemaining =
            promise {
                try
                    return! f ()
                with ex ->
                    if retriesRemaining > 0 then
                        beforeRetry ex
                        return! loop (retriesRemaining - 1)
                    else
                        return raise (Exception($"Still failing after {maxRetries} retries.", ex))
            }

        loop maxRetries

    /// Fetch.fetch throws on non 200 status codes
    let saneFetch url props =
        GlobalFetch.fetch (RequestInfo.Url url, requestProps props)

    let fetchParse url props parser =
        promise {
            try
                let! response = saneFetch url props
                let! body = response.text ()

                if response.Ok then
                    let parsed = parser body // Ignore exceptions here for now
                    return Ok parsed
                else
                    return Error { Response = response; Body = body }
            with ex -> return raise ex
        }

    let unwrapOrFail fetchPromise =
        fetchPromise
        |> Promise.map
            (function
            | Ok ok -> ok
            | Error error ->
                failwith
                    $"Error while fetching data: {error.Response.StatusText} ({error.Response.Status}) - {error.Body}")