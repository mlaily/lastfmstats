namespace LastFMStats.Client

open System
open Browser.Dom
open Fetch
open Fable.Core
open Fable.Core.Util
open System.Collections.Generic

module Util =

    module Async =
        let map f computation =
            async.Bind(computation, f >> async.Return)
            
        let tap f computation =
            computation |> map (fun x -> f x; x)

    type FetchResponse = { Response: Response; Body: obj }

    type Hash<'Value>() =
        let storage = new Dictionary<string,'Value>()
        [<Emit("$0[$1]{{=$2}}")>]
        member __.Item with get(key: string): 'Value = storage.[key]
                        and set(key: string) (value: 'Value): unit = storage.[key] <- value
        [<Emit("Object.entries($0)")>]
        member this.Entries() : (string * 'Value)[] =
            storage
            |> Seq.map (fun x -> x.Key, x.Value)
            |> Array.ofSeq
        [<Emit("Object.keys($0)")>]
        member this.Keys() : string[] = storage.Keys |> Array.ofSeq
        [<Emit("Object.values($0)")>]
        member this.Values() : 'Value[] = storage.Values |> Array.ofSeq
        [<Emit("$0.hasOwnProperty($1)")>]
        member this.HasKey(key: string) : bool = true
        [<Emit("for (var k in $0) if ($0.hasOwnProperty(k)) delete $0[k]")>]
        member this.Clear() = storage.Clear()

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

    let fetchParse<'Response> url props =
        promise {
            try
                let! response = saneFetch url props
                let! jsonBody = response.json ()
                let parsed = unbox<'Response> jsonBody
                if response.Ok then
                    return Ok parsed
                else
                    return Error { Response = response; Body = jsonBody }
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

