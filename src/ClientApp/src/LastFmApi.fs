namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.LastFmApiTypes
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
    let fetchAllTracks userName (from: int64) =
        let batchSize = 1000

        let fetchWithRetry page =
            let fetchPage () =
                fetchParse<GetRecentTracksJson>
                    $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=%s{userName}&api_key={lastFmApiKey}&format=json&limit=%d{batchSize}&page=%d{page}&from=%d{from}"
                    []
                |> unwrapOrFail

            retryPromise 10 (fun ex -> log $"An error occured: {ex.Message}\nRetrying...") fetchPage

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

                yield refinedData

                log $"Page {currentPage} - {refinedData.Length} tracks."

                if currentPage > 1 then
                    yield! loop (page - 1) // Recurse from oldest page (totalPages) to first page (1)
            }

        promise {
            log "Fetching last.fm tracks first page to get the total page count..."
            let! firstPage = fetchWithRetry 1 // Only used for the total pages number (used as the initial page)

            let totalPages =
                firstPage.recenttracks.``@attr``.totalPages |> int

            log $"Enumerating last.fm tracks pages backward from {totalPages} to 1..."
            return loop totalPages
        }

    let mapTrackToScrobbleData (track: GetRecentTracksJson.Recenttracks.Track) =
        { Artist = track.artist.``#text``
          Album = track.album.``#text``
          Timestamp = int64 track.date.uts
          Track = track.name }
