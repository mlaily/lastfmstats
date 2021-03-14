module Business

open LastFmStatsServer
open Utils
open System
open System.Linq
open ApiModels
open Microsoft.AspNetCore.Http
open Giraffe
open Microsoft.EntityFrameworkCore
open System.Globalization

let createMainApi (httpContext: HttpContext) : IMainApi =

    let dbContext = httpContext.GetService<MainContext>()

    let mainApi = {
        getResumeTimestamp = fun (UserName userName) ->
            async {
                let lastKnownTimestamp =
                    query { for scrobble in dbContext.Scrobbles do
                            where (scrobble.User.Name = normalizeUserName userName)
                            sortBy scrobble.Timestamp
                            select scrobble.Timestamp
                            lastOrDefault }
                // Last.fm allows uploading scrobbles up to two weeks back in time, I think,
                // so one month back should be more than enough to be safe...
                let backInTimeSeconds = TimeSpan.FromDays(30.).TotalSeconds |> int64
                let fromValue = lastKnownTimestamp - backInTimeSeconds
                //Error { Message = "error"; Detail = None}
                if fromValue < 0L
                then return { ResumeFrom = 0L }
                else return { ResumeFrom = fromValue }
            }

        //insertScrobbles: UserName -> FlatScrobble list -> Async<InsertScrobblesResponse>
        insertScrobbles = fun (UserName userName) scrobbleData ->
            async {
                if scrobbleData.Length > 1000
                then return Error "Too much data. Stay at or below 1000."
                else
                    // Filter out bad data from last.fm
                    let lowerTimestampLimit = DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()
                    let filteredData =
                        query { for datum in scrobbleData do
                                where (datum.Timestamp > lowerTimestampLimit)
                                // Disallow null values.
                                // This is important to ensure UNIQUE constraints work as expected with SQLite...
                                let refined = {
                                    Artist = datum.Artist |> normalizeEmpty
                                    Album = datum.Album |> normalizeEmpty
                                    Track = datum.Track |> normalizeEmpty
                                    Timestamp = datum.Timestamp }
                                select refined }
                        |> List.ofSeq

                    use transaction = dbContext.Database.BeginTransaction()
                    try
                        let normalizedUserName = normalizeUserName userName
                        let newUserCount =
                            dbContext.InsertOrIgnore(
                                dbContext.Users.EntityType,
                                nameof Unchecked.defaultof<User>.Name,
                                normalizedUserName)
                        let user = dbContext.Users.First(fun x -> x.Name = normalizedUserName)

                        let uniqueArtistNames =
                            filteredData
                            |> List.map (fun x -> x.Artist)
                            |> List.distinct
                        let newArtistCount =
                            dbContext.InsertOrIgnore(
                                dbContext.Artists.EntityType,
                                nameof Unchecked.defaultof<Artist>.Name,
                                uniqueArtistNames)
                        let queriedArtists =
                            query { for artist in dbContext.Artists do
                                    if uniqueArtistNames.Contains artist.Name
                                    then select artist }
                            |> Seq.map (fun x -> x.Name, x)
                            |> Map.ofSeq

                        let uniqueAlbumNames =
                            filteredData
                            |> List.map (fun x -> x.Album)
                            |> List.distinct

                        let newAlbumCount =
                            dbContext.InsertOrIgnore(
                                dbContext.Albums.EntityType,
                                nameof Unchecked.defaultof<Album>.Name,
                                uniqueAlbumNames)
                        let queriedAlbums =
                            query { for album in dbContext.Albums do
                                    if uniqueAlbumNames.Contains album.Name
                                    then select album }
                            |> Seq.map (fun x -> x.Name, x)
                            |> Map.ofSeq

                        let uniqueTracks =
                            filteredData
                            |> List.map (fun x ->
                                struct (queriedArtists.[x.Artist].Id,
                                        queriedAlbums.[x.Album].Id,
                                        x.Track))
                            |> List.distinct
                        let newTrackCount =
                            dbContext.InsertOrIgnore(
                                dbContext.Tracks.EntityType,
                                struct (nameof Unchecked.defaultof<Artist>.Id,
                                        nameof Unchecked.defaultof<Album>.Id,
                                        nameof Unchecked.defaultof<Track>.Name),
                                uniqueTracks)
                        let uniqueTrackNames =
                            uniqueTracks
                            |> List.map (fun struct (artistId, albumId, track) -> track)
                            |> List.distinct
                        let queriedTracks =
                            query { for track in dbContext.Tracks do
                                    if uniqueTrackNames.Contains track.Name
                                    then select track }
                            |> Seq.map (fun x -> (x.ArtistId, x.AlbumId, x.Name), x)
                            |> Map.ofSeq

                        let uniqueScrobbles =
                            filteredData
                            |> List.map (fun x ->
                                let trackTuple =
                                    (queriedArtists.[x.Artist].Id, queriedAlbums.[x.Album].Id, x.Track)
                                struct (user.Id,
                                        queriedTracks.[trackTuple].Id,
                                        x.Timestamp))
                            |> List.distinct
                        let newScrobbleCount =
                            dbContext.InsertOrIgnore(
                                dbContext.Scrobbles.EntityType,
                                struct (nameof Unchecked.defaultof<Scrobble>.UserId,
                                        nameof Unchecked.defaultof<Scrobble>.TrackId,
                                        nameof Unchecked.defaultof<Scrobble>.Timestamp),
                                uniqueScrobbles)

                        transaction.Commit()

                        return Ok { NewUsers = newUserCount
                                    NewArtists = newArtistCount
                                    NewAlbums = newAlbumCount
                                    NewTracks = newTrackCount
                                    NewScrobbles = newScrobbleCount }
                    with | ex ->
                        transaction.Rollback()
                        return raise ex
            }

        //getChartData: UserName -> GetChartRequestOptions -> Async<GetChartDataResponse>
        getChartData = fun (UserName userName) requestOptions ->
            async {
                let notAfter =
                    requestOptions.PageToken
                    |> Option.defaultValue Int64.MaxValue
                let pageSize =
                    requestOptions.PageSize
                    |> Option.defaultValue 1_000_000
                let normalizedUserName = normalizeUserName userName
                let data =
                    (query { for scrobble in dbContext.Scrobbles do
                             where (scrobble.User.Name = normalizedUserName)
                             where (scrobble.Timestamp < notAfter)
                             sortByDescending scrobble.Timestamp
                             take pageSize
                             select { Artist = scrobble.Track.Artist.Name
                                      Album = scrobble.Track.Album.Name
                                      Track = scrobble.Track.Name
                                      Timestamp = scrobble.Timestamp } }).AsNoTracking()

                let totalCount =
                    query { for scrobble in dbContext.Scrobbles do
                            where (scrobble.User.Name = normalizedUserName)
                            count }

                let targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfo.Local.Id)
                let convertAndFormat timestamp =
                    let utcConverted =
                        TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(timestamp), targetTimeZone)
                    utcConverted.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)

                // http://colorbrewer2.org/#type=qualitative&scheme=Paired&n=12
                let availableColors = [ "#a6cee3"
                                        "#1f78b4"
                                        "#b2df8a"
                                        "#33a02c"
                                        "#fb9a99"
                                        "#e31a1c"
                                        "#fdbf6f"
                                        "#ff7f00"
                                        "#cab2d6"
                                        "#6a3d9a"
                                        "#fff137" // changed. too light when opacity is not 1
                                        "#b15928" ]
                let getColorForValue value =
                    // https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
                    let deterministicHash (str: string) = 0 // TODO
                    let hash = deterministicHash value
                    let index = hash % availableColors.Length
                    availableColors.[index]

                let result =
                    ({|
                        OldestTimestamp = Int64.MaxValue
                        Timestamps = []
                        Colors = []
                        Texts = []
                    |},
                    data)
                    ||> Seq.fold (fun state flatScrobble ->
                        {|
                            OldestTimestamp =
                                if flatScrobble.Timestamp < state.OldestTimestamp
                                then flatScrobble.Timestamp
                                else state.OldestTimestamp
                            Timestamps =
                                convertAndFormat flatScrobble.Timestamp
                                :: state.Timestamps
                            Colors =
                                getColorForValue flatScrobble.Artist
                                :: state.Colors
                            Texts =
                                let parenthesis =
                                    if flatScrobble.Album = "" then ""
                                    else $"({flatScrobble.Album})"
                                $"{flatScrobble.Artist} - {flatScrobble.Track}{parenthesis}"
                                :: state.Texts
                        |})

                return { Timestamps = result.Timestamps |> Array.ofList
                         Colors = result.Colors |> Array.ofList
                         Texts = result.Texts |> Array.ofList
                         NextPageToken = result.OldestTimestamp
                         TotalCount = totalCount }
            }
    }

    mainApi
