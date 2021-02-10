using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            var normalizedUserName = userName.ToLowerInvariant();
            var user = _mainContext.Users.FirstOrDefault(x => x.Name == normalizedUserName) ?? _mainContext.Users.Add(new User { Name = normalizedUserName }).Entity;

            var artists = data
                .Select(x => x.Artist)
                .Distinct()
                .Select(artistName =>
            {
                var existing = _mainContext.Artists.FirstOrDefault(x => x.Name == artistName);
                return existing ?? _mainContext.Artists.Add(new Artist { Name = artistName }).Entity;
            }).ToDictionary(x => x.Name);

            var albums = data
                .Select(x => x.Album)
                .Distinct()
                .Select(albumName =>
            {
                var existing = _mainContext.Albums.FirstOrDefault(x => x.Name == albumName);
                return existing ?? _mainContext.Albums.Add(new Album { Name = albumName }).Entity;
            }).ToDictionary(x => x.Name);

            var tracks = data
                .Select(x => (Track: x.Track, Artist: x.Artist, Album: x.Album))
                .Distinct()
                .Select(scrobble =>
            {
                var existing = _mainContext.Tracks.FirstOrDefault(x =>
                    x.Name == scrobble.Track
                    && x.Artist.Name == scrobble.Artist
                    && x.Album.Name == scrobble.Album);
                return existing ?? _mainContext.Tracks.Add(
                    new Track
                    {
                        Name = scrobble.Track,
                        Artist = artists[scrobble.Artist],
                        Album = albums[scrobble.Album],
                    }).Entity;
            }).ToDictionary(x => (Track: x.Name, Artist: x.Artist.Name, Album: x.Album.Name));

            var scrobbles = data.Select(scrobble =>
            {
                var existing = _mainContext.Scrobbles.FirstOrDefault(x =>
                    x.Timestamp == scrobble.Timestamp
                    && x.Track.Name == scrobble.Track
                    && x.Track.Artist.Name == scrobble.Artist
                    && x.Track.Album.Name == scrobble.Album);
                return existing ?? _mainContext.Scrobbles.Add(
                    new Scrobble
                    {
                        User = user,
                        Timestamp = scrobble.Timestamp,
                        Track = tracks[(scrobble.Track, scrobble.Artist, scrobble.Album)]
                    }).Entity;
            }).ToList();

            _mainContext.SaveChanges();

            return new JsonResult(new { SavedCount = data.Length });
        }
    }
}
