namespace LastFMStats.Client

open LastFMStats.Client.Util
open ApiModels
open Fetch
open FSharp.Control
open Fable.Core

module LastFmApi =

  // Application name    I love stats
  // API key b7cced3953cbc4d6c7404dfcdaaae5fc
  // Shared secret 	
  // Registered to Yaurthek

  // GET https://ws.audioscrobbler.com/2.0/?method=user.getinfo&user=yaurthek&api_key=b7cced3953cbc4d6c7404dfcdaaae5fc&format=json
  type UserGetInfoJson = Fable.JsonProvider.Generator<"""
  {
    "user": {
      "playlists": "0",
      "playcount": "146221",
      "gender": "n",
      "name": "Yaurthek",
      "subscriber": "0",
      "url": "https://www.last.fm/user/Yaurthek",
      "country": "France",
      "image": [
        {
          "size": "small",
          "#text": "https://lastfm.freetls.fastly.net/i/u/34s/0b0039fa33fcafcbdf55b4ba8dc61ad5.png"
        },
        {
          "size": "medium",
          "#text": "https://lastfm.freetls.fastly.net/i/u/64s/0b0039fa33fcafcbdf55b4ba8dc61ad5.png"
        },
        {
          "size": "large",
          "#text": "https://lastfm.freetls.fastly.net/i/u/174s/0b0039fa33fcafcbdf55b4ba8dc61ad5.png"
        },
        {
          "size": "extralarge",
          "#text": "https://lastfm.freetls.fastly.net/i/u/300x300/0b0039fa33fcafcbdf55b4ba8dc61ad5.png"
        }
      ],
      "registered": {
        "unixtime": "1151509586",
        "#text": 1151509586
      },
      "type": "user",
      "age": "0",
      "bootstrap": "0",
      "realname": "Melvyn"
    }
  }
  """>

  // https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=yaurthek&api_key=b7cced3953cbc4d6c7404dfcdaaae5fc&from=1&format=json&limit=1000&page=1
  type GetRecentTracksJson = Fable.JsonProvider.Generator<"""
  {
    "recenttracks": {
      "@attr": {
        "page": "1",
        "perPage": "50",
        "user": "Yaurthek",
        "total": "146226",
        "totalPages": "2925"
      },
      "track": [
        {
          "artist": {
            "mbid": "f91ec397-c4be-4672-8faa-2d2983682b1c",
            "#text": "Avalon Emerson"
          },
          "@attr": {
            "nowplaying": "true"
          },
          "mbid": "4033d456-8dcf-437c-950c-b15e35b6bfe7",
          "album": {
            "mbid": "a4272a46-766a-4a7d-9bf4-fb90063ac82b",
            "#text": "Whities 006"
          },
          "streamable": "0",
          "date": {
          "uts": "1606246277",
          "#text": "24 Nov 2020, 19:31"
          }
          "url": "https://www.last.fm/music/Avalon+Emerson/_/The+Frontier",
          "name": "The Frontier",
          "image": [
            {
              "size": "small",
              "#text": "https://lastfm.freetls.fastly.net/i/u/34s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "medium",
              "#text": "https://lastfm.freetls.fastly.net/i/u/64s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "large",
              "#text": "https://lastfm.freetls.fastly.net/i/u/174s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "extralarge",
              "#text": "https://lastfm.freetls.fastly.net/i/u/300x300/7a814dee59d1e498684d5e9c172944d1.png"
            }
          ]
        },
        {
          "artist": {
            "mbid": "f91ec397-c4be-4672-8faa-2d2983682b1c",
            "#text": "Avalon Emerson"
          },
          "album": {
            "mbid": "a4272a46-766a-4a7d-9bf4-fb90063ac82b",
            "#text": "Whities 006"
          },
          "image": [
            {
              "size": "small",
              "#text": "https://lastfm.freetls.fastly.net/i/u/34s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "medium",
              "#text": "https://lastfm.freetls.fastly.net/i/u/64s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "large",
              "#text": "https://lastfm.freetls.fastly.net/i/u/174s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "extralarge",
              "#text": "https://lastfm.freetls.fastly.net/i/u/300x300/7a814dee59d1e498684d5e9c172944d1.png"
            }
          ],
          "streamable": "0",
          "date": {
            "uts": "1606246277",
            "#text": "24 Nov 2020, 19:31"
          },
          "url": "https://www.last.fm/music/Avalon+Emerson/_/The+Frontier",
          "name": "The Frontier",
          "mbid": "4033d456-8dcf-437c-950c-b15e35b6bfe7"
        }
      ]
    }
  }{
    "recenttracks": {
      "@attr": {
        "page": "1",
        "perPage": "50",
        "user": "Yaurthek",
        "total": "146226",
        "totalPages": "2925"
      },
      "track": [
        {
          "artist": {
            "mbid": "f91ec397-c4be-4672-8faa-2d2983682b1c",
            "#text": "Avalon Emerson"
          },
          "@attr": {
            "nowplaying": "true"
          },
          "mbid": "4033d456-8dcf-437c-950c-b15e35b6bfe7",
          "album": {
            "mbid": "a4272a46-766a-4a7d-9bf4-fb90063ac82b",
            "#text": "Whities 006"
          },
          "streamable": "0",
          "url": "https://www.last.fm/music/Avalon+Emerson/_/The+Frontier",
          "name": "The Frontier",
          "image": [
            {
              "size": "small",
              "#text": "https://lastfm.freetls.fastly.net/i/u/34s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "medium",
              "#text": "https://lastfm.freetls.fastly.net/i/u/64s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "large",
              "#text": "https://lastfm.freetls.fastly.net/i/u/174s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "extralarge",
              "#text": "https://lastfm.freetls.fastly.net/i/u/300x300/7a814dee59d1e498684d5e9c172944d1.png"
            }
          ]
        },
        {
          "artist": {
            "mbid": "f91ec397-c4be-4672-8faa-2d2983682b1c",
            "#text": "Avalon Emerson"
          },
          "album": {
            "mbid": "a4272a46-766a-4a7d-9bf4-fb90063ac82b",
            "#text": "Whities 006"
          },
          "image": [
            {
              "size": "small",
              "#text": "https://lastfm.freetls.fastly.net/i/u/34s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "medium",
              "#text": "https://lastfm.freetls.fastly.net/i/u/64s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "large",
              "#text": "https://lastfm.freetls.fastly.net/i/u/174s/7a814dee59d1e498684d5e9c172944d1.png"
            },
            {
              "size": "extralarge",
              "#text": "https://lastfm.freetls.fastly.net/i/u/300x300/7a814dee59d1e498684d5e9c172944d1.png"
            }
          ],
          "streamable": "0",
          "date": {
            "uts": "1606246277",
            "#text": "24 Nov 2020, 19:31"
          },
          "url": "https://www.last.fm/music/Avalon+Emerson/_/The+Frontier",
          "name": "The Frontier",
          "mbid": "4033d456-8dcf-437c-950c-b15e35b6bfe7"
        }
      ]
    }
  }
  """>

  /// Fetch.fetch throws on non 200 status codes
  let saneFetch url props = GlobalFetch.fetch (RequestInfo.Url url, requestProps props)

  let fetchJson url props parser =
      promise {
          try
              let! response = saneFetch url props
              let! body = response.text()
              if response.Ok then
                  let parsed = parser body // Ignore exceptions here for now
                  return Ok parsed
              else return Error {| Response = response; Body = body |}
          with | ex -> return raise ex
      }

  let fetchTracks (username: string) (limit: int) (page: int) (from: int64) =
      promise {
          let url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={username}&api_key=b7cced3953cbc4d6c7404dfcdaaae5fc&from=1&format=json&limit={limit}&page={page}&from={from}"
          let! result = fetchJson url [] GetRecentTracksJson
          return result
      }

  let fetchAllTracks userName from =
      let batchSize = 1000

      let fetchPage page =
          let fetchOne () =
              fetchTracks userName batchSize page from
              |> Promise.map
                  (function
                  | Ok ok -> ok
                  | Error error ->
                      failwith
                          $"Error while fetching tracks: {error.Response.StatusText} ({error.Response.Status}) - {error.Body}")

          retryPromise 10 (fun ex -> log $"An error occured: {ex.Message}\nRetrying...") fetchOne

      let rec fetchAllTracks' page =
          asyncSeq {
              let! data = fetchPage page |> Async.AwaitPromise
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
                  yield! fetchAllTracks' (page - 1) // Recurse from oldest page (totalPages) to first page (1)
          }

      promise {
          let! data = fetchPage 1 // Only used for the total pages number (used as the initial page)

          let totalPages =
              data.recenttracks.``@attr``.totalPages |> int

          log $"Enumerating pages from {totalPages} to 1..."
          return fetchAllTracks' totalPages
      }

  let mapScrobbleData (track: GetRecentTracksJson.Recenttracks.Track) =
      { Artist = track.artist.``#text``
        Album = track.album.``#text``
        Timestamp = int64 track.date.uts
        Track = track.name }