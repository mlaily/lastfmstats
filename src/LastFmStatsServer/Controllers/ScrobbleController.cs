using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Runtime.CompilerServices;

namespace LastFmStatsServer.Controllers
{
    [ApiController]
    [Route("api/scrobbles")]
    public class ScrobbleController : ControllerBase
    {
        private readonly MainContext _mainContext;
        private readonly ILogger<ScrobbleController> _logger;

        public ScrobbleController(MainContext mainContext, ILogger<ScrobbleController> logger)
        {
            _mainContext = mainContext;
            _logger = logger;
        }

        [HttpPost("{userName}")]
        public IActionResult Post(string userName, ScrobbleData[] data)
        {
            /*
             * TODO:
             * 
             * we don't want to update already good data, because we don't trust last.fm not to modify previous scrobbles,
             * so we have to make sure the timestamps for this user don't exist.
             * 
             * 
             * 
             * 
             * old code:
             * 
             * using (var transaction = connection.BeginTransaction())
                {
                    // Create the user here once we know it's a valid last.fm user
                    if (user == null)
                    {
                        user = await GetOrInsertAsync<User>(username);
                    }

                    var scrobbles = page
                    // Filter out bad data from last.fm
                    .Where(x => x.TimePlayed > unknownDateFilter)
                    .Select(x => new PopulatedScrobble()
                    {
                        ArtistName = x.ArtistName,
                        AlbumName = x.AlbumName,
                        TrackName = x.Name,
                        UserName = username,
                        TimePlayed = x.TimePlayed.Value
                    });

                    foreach (var scrobble in scrobbles)
                    {
                        var artist = await GetOrInsertAsync<Artist>(scrobble.ArtistName);
                        var album = scrobble.AlbumName != null ? await GetOrInsertAsync<Album>(scrobble.AlbumName) : null;

                        var track = await connection.FindOrDefaultAsync(scrobble.TrackName, artist.Id, album?.Id);
                        if (track == null)
                        {
                            track = new Track()
                            {
                                Name = scrobble.TrackName,
                                ArtistId = artist.Id,
                                AlbumId = album?.Id
                            };
                            var id = await connection.InsertAsync(track);
                            track.Id = id;
                        }

                        await connection.InsertAsync(new Scrobble()
                        {
                            UserId = user.Id,
                            TrackId = track.Id,
                            TimePlayed = scrobble.TimePlayed
                        });
                    }

                    transaction.Commit();
                }
             */

            if (data == null)
            {
                return new JsonResult(new { SavedCount = 0 });
            }

            if (data.Length > 1000)
            {
                var result = new JsonResult(new { Error = "Too much data. Stay below 1000." });
                result.StatusCode = (int)HttpStatusCode.BadRequest;
                return result;
            }

            // Filter out bad data from last.fm
            var lowerTimestampLimit = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            var filteredData = data.Where(x => x.Timestamp > lowerTimestampLimit);

            var normalizedUserName = userName.ToLowerInvariant();
            var user = _mainContext.Users.SingleOrDefault(x => x.Name == normalizedUserName) ?? _mainContext.Users.Add(new User { Name = normalizedUserName }).Entity;

            //// Preload things that can easily and safely be preloaded:
            //var existingArtists = _mainContext.Artists.Where(x => data.Select(x => x.Artist).Distinct().Contains(x.Name)).ToList();
            //var existingAlbums = _mainContext.Albums.Where(x => data.Select(x => x.Album).Distinct().Contains(x.Name)).ToList();
            //// Note the limit, because since we can't match on multiple columns, we might bring back more data than intended:
            //var existingTracks = _mainContext.Tracks.Where(x => data.Select(x => x.Track).Distinct().Contains(x.Name)).Take(data.Length).ToList();
            //var existingScrobbles = _mainContext.Scrobbles.Where(x => x.User.Name == userName && data.Select(x => x.Timestamp).Contains(x.Timestamp)).Take(data.Length).ToList();

            string GetColumnName(IEntityType entityType, string targetPropertyName)
                => entityType.FindProperty(targetPropertyName).GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema()));

            int InsertOrIgnore(IEntityType entityType, string targetPropertyName, params string[] values)
            {
                int inserted = 0;
                foreach (var value in values)
                    inserted += _mainContext.Database.ExecuteSqlInterpolated(Concat(
                        $"INSERT OR IGNORE INTO {entityType.GetTableName()} ({GetColumnName(entityType, targetPropertyName)}) ",
                        $"VALUES ({value})"));
                return inserted;
            }

            // _mainContext.Tracks.EntityType.FindNavigation("Artist").ForeignKey.Properties.Single().GetColumnName(StoreObjectIdentifier.Table(_mainContext.Tracks.EntityType.GetTableName(), _mainContext.Tracks.EntityType.GetSchema()))

            using (var transaction = _mainContext.Database.BeginTransaction())
            {
                try
                {
                    var uniqueArtistNames = filteredData.Select(x => x.Artist).Distinct().ToArray();
                    var uniqueAlbumNames = filteredData.Select(x => x.Album).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                    var newArtistNameCount = InsertOrIgnore(_mainContext.Artists.EntityType, nameof(Artist.Name), uniqueArtistNames);
                    var newAlbumNameCount = InsertOrIgnore(_mainContext.Albums.EntityType, nameof(Album.Name), uniqueAlbumNames);
                    var scrobbleArtists = _mainContext.Artists.Where(x => uniqueArtistNames.Contains(x.Name)).ToDictionary(x => x.Name);
                    var scrobbleAlbums = _mainContext.Albums.Where(x => uniqueAlbumNames.Contains(x.Name)).ToDictionary(x => x.Name);

                    foreach (var scrobble in filteredData)
                    {
                        var dummyTrack = new Track
                        {
                            Name = scrobble.Track,
                            Artist = scrobbleArtists[scrobble.Artist],
                            Album = scrobbleAlbums.GetValueOrDefault(scrobble.Album, null),
                        };
                        var track = _mainContext.Tracks
                            .SingleOrDefault(x =>
                                x.Name == dummyTrack.Name
                                && x.Artist.Id == dummyTrack.Artist.Id
                                && x.Album.Id == (dummyTrack.Album == null ? null : dummyTrack.Album.Id));
                        if (track == null)
                        {
                            track = _mainContext.Tracks.Add(dummyTrack).Entity;
                        }

                        var dummyScrobble = new Scrobble
                        {
                            Timestamp = scrobble.Timestamp,
                            Track = track,
                            User = user,
                        };
                        if (!_mainContext.Scrobbles.Any(x => x.Timestamp == scrobble.Timestamp))
                        {
                            _mainContext.Scrobbles.Add(dummyScrobble);
                        }
                    }

                    _mainContext.SaveChanges();
                    transaction.Commit();

                    return new JsonResult(new { SavedCount = data.Length });
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }


            //var artists = data
            //    .Select(x => x.Artist)
            //    .Distinct()
            //    .Select(artistName =>
            //    {
            //        var existing = _mainContext.Artists.FirstOrDefault(x => x.Name == artistName);
            //        // TODO: handle concurrency conflict exception
            //        return existing ?? _mainContext.Artists.Add(new Artist { Name = artistName }).Entity;
            //    }).ToDictionary(x => x.Name);

            //var albums = data
            //    .Select(x => x.Album)
            //    .Distinct()
            //    .Select(albumName =>
            //    {
            //        var existing = _mainContext.Albums.FirstOrDefault(x => x.Name == albumName);
            //        // TODO: handle concurrency conflict exception
            //        return existing ?? _mainContext.Albums.Add(new Album { Name = albumName }).Entity;
            //    }).ToDictionary(x => x.Name);

            //var tracks = data
            //    .Select(x => (x.Track, x.Artist, x.Album))
            //    .Distinct()
            //    .Select(trackTuple =>
            //    {
            //        var existing = _mainContext.Tracks.FirstOrDefault(x =>
            //            x.Name == trackTuple.Track
            //            && x.Artist.Name == trackTuple.Artist
            //            && x.Album.Name == trackTuple.Album);
            //        // TODO: handle concurrency conflict exception
            //        return existing ?? _mainContext.Tracks.Add(
            //            new Track
            //            {
            //                Name = trackTuple.Track,
            //                Artist = artists[trackTuple.Artist],
            //                Album = albums[trackTuple.Album],
            //            }).Entity;
            //    }).ToDictionary(x => (Track: x.Name, Artist: x.Artist.Name, Album: x.Album.Name));

            //var scrobbles = data.Select(scrobble =>
            //    {
            //        var existing = _mainContext.Scrobbles.FirstOrDefault(x =>
            //            x.User.Name == userName
            //            && x.Timestamp == scrobble.Timestamp
            //            && x.Track.Name == scrobble.Track
            //            && x.Track.Artist.Name == scrobble.Artist
            //            && x.Track.Album.Name == scrobble.Album);
            //        // TODO: handle concurrency conflict exception
            //        return existing ?? _mainContext.Scrobbles.Add(
            //            new Scrobble
            //            {
            //                User = user,
            //                Timestamp = scrobble.Timestamp,
            //                Track = tracks[(scrobble.Track, scrobble.Artist, scrobble.Album)]
            //            }).Entity;
            //    }).ToList();

            //_mainContext.SaveChanges();

            //return new JsonResult(new { SavedCount = data.Length });
        }

        private static FormattableString Concat(string firstPart, FormattableString secondPart)
            => FormattableStringFactory.Create(firstPart + secondPart.Format, secondPart.GetArguments());
    }
}
