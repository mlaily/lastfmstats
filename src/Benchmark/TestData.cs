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
        public static SqliteConnection GetConnection()
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection(ConfigurationManager.ConnectionStrings["TestData"].ConnectionString);
            }
            _connection.Open();
            return _connection;
        }

        public static IReadOnlyCollection<(long Id, string Name)> GetArtists()
        {
            return GetConnection().Query<(long Id, string Name)>(@"SELECT * FROM Artists ORDER BY Id").ToList();
        }

        public static IReadOnlyCollection<(string ArtistName, string AlbumName, string TrackName, long Timestamp)> GetScrobbleData()
        {
            return GetConnection().Query<(string ArtistName, string AlbumName, string TrackName, long Timestamp)>(
                @"SELECT ar.Name, al.Name, t.Name, s.Timestamp
FROM Scrobbles s
JOIN Tracks t ON t.Id=s.TrackId
JOIN Artists ar ON ar.Id=t.ArtistId
JOIN Albums al ON al.Id=t.AlbumId
ORDER BY s.Timestamp").ToList();
        }
    }
}
