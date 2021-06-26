using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
using LastFmStatsServer.Business;
using ApiModels;
using LastFmStatsServer.Plumbing;

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

        [HttpGet("{userName}/resume-from")]
        public GetResumeTimestampResponse GetResumeTimestamp(string userName)
        {
            var lastKnownTimestamp = (from scrobble in _mainContext.Scrobbles
                                      where scrobble.User.Name == Utils.NormalizeUserName(userName)
                                      orderby scrobble.Timestamp
                                      select scrobble.Timestamp).LastOrDefault();

            // Last.fm allows uploading scrobbles up to two weeks back in time, I think,
            // so one month back should be more than enough to be safe...
            var backInTimeSeconds = TimeSpan.FromDays(30).TotalSeconds;
            var fromValue = lastKnownTimestamp - backInTimeSeconds;
            if (fromValue < 0) fromValue = 0;

            return new GetResumeTimestampResponse(resumeFrom: (long)fromValue);
        }

        [HttpGet("{userName}/display-name")]
        public GetUserNameResponse GetDisplayName(string userName)
        {
            var user = _mainContext.Users.SingleOrDefault(x => x.Name == Utils.NormalizeUserName(userName));
            if (user == null)
            {
                throw new HttpResponseException<GenericError>
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Value = new GenericError("User not found."),
                };
            }
            else
            {
                return new GetUserNameResponse(displayName: user.DisplayName);
            }
        }

        [HttpPost("{userName}")]
        public InsertScrobblesResponse Post(string userName, FlatScrobble[] scrobbleData)
        {
            if (scrobbleData == null)
                return new InsertScrobblesResponse(0, 0, 0, 0, 0);

            if (scrobbleData.Length > 1000)
            {
                throw new HttpResponseException<GenericError>
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Value = new GenericError("Too much data. Stay at or below 1000."),
                };
            }

            var filteredData = (from scrobble in scrobbleData
                                where scrobble.Timestamp > Utils.LastFmScrobbleDateLowerBound
                                let refined = new FlatScrobble(
                                    // Disallow null values. This is to ensure UNIQUE constraints work as expected with SQLite...
                                    Utils.NormalizeEmpty(scrobble.Artist),
                                    Utils.NormalizeEmpty(scrobble.Album),
                                    Utils.NormalizeEmpty(scrobble.Track),
                                    scrobble.Timestamp)
                                select refined)
                               .ToList();

            var originalUserName = Utils.NormalizeEmpty(userName);
            var normalizedUserName = Utils.NormalizeUserName(userName);

            using (var transaction = _mainContext.Database.BeginTransaction())
            {
                try
                {
                    // Use the normalized name (lower case) to ensure unique user names, but use the original value as the display name to have the correct casing.
                    var newUserCount = _mainContext.InsertOrIgnore(
                        _mainContext.Users.EntityType,
                        (nameof(LastFmStatsServer.User.Name), nameof(LastFmStatsServer.User.DisplayName)),
                        new[] { (normalizedUserName, originalUserName) });
                    var user = _mainContext.Users.FirstOrDefault(x => x.Name == normalizedUserName);

                    if (user.DisplayName != originalUserName)
                    {
                        user.DisplayName = originalUserName;
                        _mainContext.SaveChanges();
                    }

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

                    return new InsertScrobblesResponse(
                        newUsers: newUserCount,
                        newArtists: newArtistNameCount,
                        newAlbums: newAlbumNameCount,
                        newTracks: newTrackCount,
                        newScrobbles: newScrobbleCount);
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        [HttpGet("{userName}")]
        public GetChartDataResponse Get(
            string userName,
            [FromQuery] long? nextPageToken = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] ColorChoice color = default,
            [FromQuery] string timeZone = null,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            if (nextPageToken == null)
                // nextPageToken is actually the timestamp limit in the query
                nextPageToken = long.MaxValue;

            var maxPageSize = 1_000_000; //50000;
            if (pageSize == null || pageSize > maxPageSize)
                pageSize = maxPageSize;

            var normalizedUserName = Utils.NormalizeUserName(userName);

            var actualStartDate = startDate?.ToUnixTimeSeconds() ?? 0;
            var actualEndDate = endDate?.ToUnixTimeSeconds() ?? long.MaxValue;
            var scrobblesQueryBase = from scrobble in _mainContext.Scrobbles
                                     where scrobble.User.Name == normalizedUserName
                                     where scrobble.Timestamp >= actualStartDate
                                     where scrobble.Timestamp <= actualEndDate
                                     where scrobble.Timestamp < nextPageToken
                                     orderby scrobble.Timestamp descending
                                     select new FlatScrobble(
                                         scrobble.Track.Artist.Name,
                                         scrobble.Track.Album.Name,
                                         scrobble.Track.Name,
                                         scrobble.Timestamp);

            var scrobbles = scrobblesQueryBase.Take(pageSize.Value).AsNoTracking();

            var totalCount = scrobblesQueryBase.Count();

            var timestamps = new List<string>();
            var colors = new List<string>();
            var texts = new List<string>();

            var targetTimeZone = Utils.GetTimeZoneOrUTC(timeZone);

            var oldestTimestamp = double.MaxValue;
            foreach (var scrobble in scrobbles)
            {
                if (scrobble.Timestamp < oldestTimestamp)
                    oldestTimestamp = scrobble.Timestamp;

                timestamps.Add(ConvertAndFormat((long)scrobble.Timestamp));

                var displayColor = color switch
                {
                    ColorChoice.None => "#400d73",
                    ColorChoice.Artist => Utils.MapToArrayValue(scrobble.Artist, Utils.Colors),
                    ColorChoice.Album => Utils.MapToArrayValue(scrobble.Album, Utils.Colors),
                    _ => throw new NotSupportedException(),
                };
                colors.Add(displayColor);
                texts.Add($"{scrobble.Artist} - {scrobble.Track}{(string.IsNullOrWhiteSpace(scrobble.Album) ? "" : " (" + scrobble.Album + ")")}");
            }

            return new GetChartDataResponse(
                timestamps: timestamps.ToArray(),
                colors: colors.ToArray(),
                texts: texts.ToArray(),
                nextPageToken: oldestTimestamp,
                totalCount: totalCount);

            string ConvertAndFormat(long timestamp)
            {
                var utcTimePlayed = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(timestamp), targetTimeZone);
                return utcTimePlayed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
        }
    }
}
