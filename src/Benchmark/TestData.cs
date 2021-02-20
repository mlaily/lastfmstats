using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    public static class TestData
    {
        private static SqliteConnection _connection;
        private static object _initializationLock = new object();
        private static bool _initialized = false;

        public const int PrepopulatedAndNewArtistsCount = 5000;
        public const int PrepopulatedArtistsCount = 2500;
        public const int NewArtistsCount = 2500;

        /// <summary>
        /// Too high values quickly break parameterized queries because of SQLITE_MAX_VARIABLE_NUMBER (defaults to 32766)
        /// </summary>
        public const int ScrobbleDataCount = 1000;

        public static IReadOnlyCollection<(long Id, string Name)> PrepopulatedAndNewArtists { get; private set; }
        public static IReadOnlyCollection<string> PrepopulatedArtistNames { get; private set; }
        public static IReadOnlyCollection<string> NewArtistNames { get; private set; }

        public static IReadOnlyCollection<(string Artist, string Album, string Track, long Timestamp)> ScrobbleData { get; private set; }

        public static void Initialize()
        {
            lock (_initializationLock)
            {
                if (_initialized)
                {
                    return;
                }

                PrepopulatedAndNewArtists = GetArtists().Take(PrepopulatedAndNewArtistsCount).ToList();
                PrepopulatedArtistNames = PrepopulatedAndNewArtists.Take(PrepopulatedArtistsCount).Select(x => x.Name).ToList();
                NewArtistNames = PrepopulatedAndNewArtists.Skip(PrepopulatedArtistsCount).Take(NewArtistsCount).Select(x => x.Name).ToList();

                ScrobbleData = GetScrobbleData().Take(ScrobbleDataCount).ToList();

                _initialized = true;
            }
        }

        private static SqliteConnection GetConnection()
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection(ConfigurationManager.ConnectionStrings["TestData"].ConnectionString);
            }
            _connection.Open();
            return _connection;
        }

        private static IReadOnlyCollection<(long Id, string Name)> GetArtists()
        {
            return GetConnection().Query<(long Id, string Name)>(@"SELECT * FROM Artists ORDER BY Id").ToList();
        }

        private static IReadOnlyCollection<(long Id, string Name)> GetAlbums()
        {
            return GetConnection().Query<(long Id, string Name)>(@"SELECT * FROM Albums ORDER BY Id").ToList();
        }

        private static IReadOnlyCollection<(string Artist, string Album, string Track, long Timestamp)> GetScrobbleData()
        {
            return GetConnection().Query<(string Artist, string Album, string Track, long Timestamp)>(
                @"SELECT ar.Name, al.Name, t.Name, s.Timestamp
FROM Scrobbles s
JOIN Tracks t ON t.Id=s.TrackId
JOIN Artists ar ON ar.Id=t.ArtistId
JOIN Albums al ON al.Id=t.AlbumId
ORDER BY s.Timestamp").ToList();
        }
    }
}
