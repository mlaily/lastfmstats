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
using Dapper;

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
            if (data == null)
            {
                return new JsonResult(new { SavedCount = 0 });
            }

            if (data.Length > 1000)
            {
                var result = new JsonResult(new { Error = "Too much data. Stay at or below 1000." });
                result.StatusCode = (int)HttpStatusCode.BadRequest;
                return result;
            }

            string NormalizeEmpty(string value)
                => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

            // Filter out bad data from last.fm
            var lowerTimestampLimit = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            var filteredData = (from datum in data
                                where datum.Timestamp > lowerTimestampLimit
                                let refined = new ScrobbleData(NormalizeEmpty(datum.Artist), NormalizeEmpty(datum.Album), datum.Timestamp, NormalizeEmpty(datum.Track))
                                // Disallow null values. This is to ensure UNIQUE constraints work as expected with SQLite...
                                where refined.Artist != null && refined.Album != null && refined.Track != null
                                select refined)
                               .ToList();

            var normalizedUserName = userName.ToLowerInvariant();

            using (var transaction = _mainContext.Database.BeginTransaction())
            {
                try
                {
                    var newUserCount = _mainContext.InsertOrIgnore(_mainContext.Users.EntityType, nameof(LastFmStatsServer.User.Name), new[] { normalizedUserName });
                    var user = _mainContext.Users.FirstOrDefault(x => x.Name == normalizedUserName);

                    var uniqueArtistNames = filteredData.Select(x => x.Artist).Distinct();
                    var newArtistNameCount = _mainContext.InsertOrIgnore(_mainContext.Artists.EntityType, nameof(Artist.Name), uniqueArtistNames);
                    var queriedArtists = _mainContext.Artists.Where(x => uniqueArtistNames.Contains(x.Name)).ToDictionary(x => x.Name);

                    var uniqueAlbumNames = filteredData.Select(x => x.Album).Distinct();
                    var newAlbumNameCount = _mainContext.InsertOrIgnore(_mainContext.Albums.EntityType, nameof(Album.Name), uniqueAlbumNames);
                    var queriedAlbums = _mainContext.Albums.Where(x => uniqueAlbumNames.Contains(x.Name)).ToDictionary(x => x.Name);

                    var uniqueTracks = filteredData.Select(x => (queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)).Distinct();
                    var newTrackCount = _mainContext.InsertOrIgnore(_mainContext.Tracks.EntityType, (nameof(Track.ArtistId), nameof(Track.AlbumId), nameof(Track.Name)), uniqueTracks);

                    var uniqueTrackNames = uniqueTracks.Select(x => x.Track).Distinct();
                    var queriedTracks = _mainContext.Tracks.Where(x => uniqueTrackNames.Contains(x.Name)).ToDictionary(x => (x.ArtistId, x.AlbumId, x.Name));

                    var uniqueScrobbles = filteredData.Select(x => (user.Id, queriedTracks[(queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)].Id, x.Timestamp)).Distinct();
                    var newScrobbleCount = _mainContext.InsertOrIgnore(_mainContext.Scrobbles.EntityType, (nameof(Scrobble.UserId), nameof(Scrobble.TrackId), nameof(Scrobble.Timestamp)), uniqueScrobbles);

                    //var uniqueTimestamps = filteredData.Select(x => x.Timestamp).Distinct();
                    //var queriedScrobbles = _mainContext.Scrobbles.Where(x => uniqueTimestamps.Contains(x.Timestamp)).ToDictionary(x => x.Timestamp);

                    transaction.Commit();

                    return new JsonResult(new
                    {
                        newUserCount,
                        newArtistNameCount,
                        newAlbumNameCount,
                        newTrackCount,
                        newScrobbleCount,
                    });
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static FormattableString Concat(string firstPart, FormattableString secondPart)
            => FormattableStringFactory.Create(firstPart + secondPart.Format, secondPart.GetArguments());
    }
}
