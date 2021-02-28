namespace LastFMStats.Client

open System
open Browser.Dom

module Util =

    let outputHtml =
        document.querySelector ("#output") :?> Browser.Types.HTMLParagraphElement

    let log msg =
        // outputHtml.innerHTML <- $"{outputHtml.innerHTML}<br>{msg}"
        console.log msg
        

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