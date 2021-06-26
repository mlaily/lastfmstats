namespace LastFmStats.Client

open LastFmStats.Client.Util
open LastFmStats.Client.LastFmApiTypes
open ApiModels
open FSharp.Control
open Fable.Core

module LastFmApi =

    // Application name    I love stats
    // API key b7cced3953cbc4d6c7404dfcdaaae5fc
    // Shared secret
    // Registered to Yaurthek
    let lastFmApiKey = "b7cced3953cbc4d6c7404dfcdaaae5fc"

    /// Recursively fetch all the pages
    let fetchAllTracks (log: ILogger) userName (from: int64) =
        let batchSize = 1000

        let fetchWithRetry page =
            let fetchPage () =
                fetchParse<GetRecentTracksJson>
                    $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=%s{userName}&api_key={lastFmApiKey}&format=json&limit=%d{batchSize}&page=%d{page}&from=%d{from}"
                    []
                |> unwrapOrFail

            retryPromise 10 (fun err -> log.LogAlways $"An error occured: {err.Exception.Message}\nRetrying... ({err.NextTryNumber}/{err.MaxRetries})") fetchPage

        let rec loop page =
            asyncSeq {
                let! data = fetchWithRetry page |> Async.AwaitPromise
                let currentPage = data.recenttracks.``@attr``.page |> int

                let refinedData =
                    data.recenttracks.track
                    |> Array.where
                        (fun x ->
                            try
                                x.``@attr``.nowplaying.ToUpperInvariant() = "FALSE"
                            with _ -> true)

                log.LogAlways $"Page {currentPage} - {refinedData.Length} tracks."

                let userDisplayName = data.recenttracks.``@attr``.user
                yield userDisplayName, refinedData

                if currentPage > 1 then
                    yield! loop (page - 1) // Recurse from oldest page (totalPages) to first page (1)
            }

        promise {
            log.LogDebug "Fetching last.fm 'tracks' page count..."
            let! firstPage = fetchWithRetry 1 // Only used for the total pages number (used as the initial page)

            let totalPages =
                firstPage.recenttracks.``@attr``.totalPages |> int

            log.LogAlways $"Fetching last.fm 'tracks' pages from {totalPages} (oldest) to 1 (most recent)..."
            return loop totalPages
        }

    let mapTrackToScrobbleData (track: GetRecentTracksJson.Recenttracks.Track) =
        { Artist = track.artist.``#text``
          Album = track.album.``#text``
          Timestamp = float track.date.uts
          Track = track.name }
