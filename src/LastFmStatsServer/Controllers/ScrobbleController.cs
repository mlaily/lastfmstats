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
using System.Text;
using System.Globalization;

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


        private static string NormalizeEmpty(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        private static string GetNormalizedUserName(string userName) => NormalizeEmpty(userName?.ToLowerInvariant());

        // http://colorbrewer2.org/#type=qualitative&scheme=Paired&n=12
        private static readonly string[] Colors = new[]
        {
            "#a6cee3",
            "#1f78b4",
            "#b2df8a",
            "#33a02c",
            "#fb9a99",
            "#e31a1c",
            "#fdbf6f",
            "#ff7f00",
            "#cab2d6",
            "#6a3d9a",
            "#fff137", // changed. too light when opacity is not 1
            "#b15928",
        };
        private static string GetColorForValue(string value)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
            var index = hash[0] % Colors.Length; //  Colors.Length is assumed to be < 256 (a byte)
            return Colors[index];
        }

        private enum ColorChoice
        {
            None,
            Album,
            Artist,
        }

        [HttpGet("{userName}")]
        public IActionResult Get(string userName)
        {
            var data = (from scrobble in _mainContext.Scrobbles
                        where scrobble.User.Name == GetNormalizedUserName(userName)
                        orderby scrobble.Timestamp
                        select new ScrobbleData(scrobble.Track.Artist.Name, scrobble.Track.Album.Name, scrobble.Timestamp, scrobble.Track.Name));

            var time = new List<string>();
            var color = new List<string>();
            var displayValue = new List<string>();

            var timeZone = "";
            var targetTimeZone = string.IsNullOrWhiteSpace(timeZone) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZone);

            var colorChoice = ColorChoice.Artist;

            foreach (var scrobble in data)
            {
                var utcTimePlayed = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(scrobble.Timestamp), targetTimeZone);
                time.Add(utcTimePlayed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                var displayColor = colorChoice switch
                {
                    ColorChoice.None => "#400d73",
                    ColorChoice.Album => GetColorForValue(scrobble.Album),
                    ColorChoice.Artist => GetColorForValue(scrobble.Artist),
                    _ => throw new NotSupportedException(),
                };
                color.Add(displayColor);
                //displayValue.Add(displayColor.DisplayValue);
            }

            return new JsonResult(new { time, color, displayValue });
        }

        [HttpGet("{userName}/resume-from")]
        public IActionResult GetResumeTimestamp(string userName)
        {
            var lastKnownTimestamp = (from scrobble in _mainContext.Scrobbles
                                      where scrobble.User.Name == GetNormalizedUserName(userName)
                                      orderby scrobble.Timestamp
                                      select scrobble.Timestamp).LastOrDefault();

            // Last.fm allows uploading scrobbles up to two weeks back in time, I think...
            var backInTimeSeconds = TimeSpan.FromDays(30).TotalSeconds;
            var fromValue = lastKnownTimestamp - backInTimeSeconds;
            if (fromValue < 0) fromValue = 0;

            return new JsonResult(new { from = fromValue });
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


            // Filter out bad data from last.fm
            var lowerTimestampLimit = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            var filteredData = (from datum in data
                                where datum.Timestamp > lowerTimestampLimit
                                let refined = new ScrobbleData(NormalizeEmpty(datum.Artist), NormalizeEmpty(datum.Album), datum.Timestamp, NormalizeEmpty(datum.Track))
                                // Disallow null values. This is to ensure UNIQUE constraints work as expected with SQLite...
                                where refined.Artist != null && refined.Album != null && refined.Track != null
                                select refined)
                               .ToList();

            using (var transaction = _mainContext.Database.BeginTransaction())
            {
                try
                {
                    var newUserCount = _mainContext.InsertOrIgnore(_mainContext.Users.EntityType, nameof(LastFmStatsServer.User.Name), new[] { GetNormalizedUserName(userName) });
                    var user = _mainContext.Users.FirstOrDefault(x => x.Name == GetNormalizedUserName(userName));

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
