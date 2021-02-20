using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [BenchmarkCategory(nameof(InsertOrIgnoreScrobbleData))]
    [IterationCount(10)]
    public class InsertOrIgnoreScrobbleData : BenchmarkBase
    {
        // Prepopulated data:
        private Dictionary<string, Artist> queriedArtists;
        private Dictionary<string, Album> queriedAlbums;

        public override void SetupIteration()
        {
            base.SetupIteration();
            InitializeDb();
        }

        private void InsertUserArtistsAndAlbums()
        {
            // We need all the related data already inserted before we can insert scrobbles:
            var user = Context.Users.Add(new User { Id = 1, Name = "Melvyn" });
            Context.SaveChanges();

            var uniqueArtistNames = TestData.ScrobbleData.Select(x => x.Artist).Distinct();
            var newArtistNameCount = Context.InsertOrIgnore_Bulk(Context.Artists.EntityType, nameof(Artist.Name), uniqueArtistNames);
            queriedArtists = Context.Artists.Where(x => uniqueArtistNames.Contains(x.Name)).ToDictionary(x => x.Name);

            var uniqueAlbumNames = TestData.ScrobbleData.Select(x => x.Album).Distinct();
            var newAlbumNameCount = Context.InsertOrIgnore_Bulk(Context.Albums.EntityType, nameof(Album.Name), uniqueAlbumNames);
            queriedAlbums = Context.Albums.Where(x => uniqueAlbumNames.Contains(x.Name)).ToDictionary(x => x.Name);
        }
        [Benchmark(OperationsPerInvoke = TestData.ScrobbleDataCount)]
        public void DapperCTE()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                InsertUserArtistsAndAlbums();

                var uniqueTracks = TestData.ScrobbleData.Select(x => (queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)).Distinct();
                var newTrackCount = Context.InsertOrIgnore_Bulk(Context.Tracks.EntityType, (nameof(Track.ArtistId), nameof(Track.AlbumId), nameof(Track.Name)), uniqueTracks.Cast<ITuple>());

                //

                var inserted = InsertScrobblesOrIgnoreWithCTE(1, TestData.ScrobbleData.Select(x => (queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track, x.Timestamp)));
                BenchmarkDebug.Assert(inserted == TestData.ScrobbleDataCount);
                transaction.Commit();
            }
        }

        private int InsertScrobblesOrIgnoreWithCTE(long userId, IEnumerable<(long ArtistId, long AlbumId, string TrackName, long Timestamp)> data)
        {
            var scrobbleUserIdColumn = Context.GetColumnName(Context.Scrobbles.EntityType, nameof(Scrobble.UserId));
            var scrobbleTrackIdColumn = Context.GetColumnName(Context.Scrobbles.EntityType, nameof(Scrobble.TrackId));
            var scrobbleTimestampColumn = Context.GetColumnName(Context.Scrobbles.EntityType, nameof(Scrobble.Timestamp));

            var trackIdColumn = Context.GetColumnName(Context.Tracks.EntityType, nameof(Track.Id));
            var trackArtistIdColumn = Context.GetColumnName(Context.Tracks.EntityType, nameof(Track.ArtistId));
            var trackAlbumIdColumn = Context.GetColumnName(Context.Tracks.EntityType, nameof(Track.AlbumId));
            var trackNameColumn = Context.GetColumnName(Context.Tracks.EntityType, nameof(Track.Name));

            var sb = new StringBuilder();
            int currentParamId = 0;
            DynamicParameters parameters = new DynamicParameters();
            string parameterPrefix = "@a";
            bool firstRow = true;
            foreach (var item in data)
            {
                if (firstRow)
                    firstRow = false;
                else
                    sb.Append(",");

                void AddParameter(object value, bool addTrailingComma = false)
                {
                    var name = $"{parameterPrefix}{currentParamId}";
                    parameters.Add(name, value);
                    sb.Append(name);
                    if (addTrailingComma) sb.Append(",");
                    currentParamId++;
                }

                sb.Append("(");
                AddParameter(item.ArtistId, true);
                AddParameter(item.AlbumId, true);
                AddParameter(item.TrackName, true);
                AddParameter(item.Timestamp);
                sb.Append(")");
            }

            parameters.Add("UserId", userId);

            //EXPLAIN QUERY PLAN
            //INSERT OR IGNORE INTO Scrobbles
            //(UserId, TrackId, Timestamp)
            //SELECT 1 AS UserId, t.Id as TrackId, Cte.Timestamp FROM Tracks t
            //INNER JOIN
            //(SELECT * FROM
            //  (WITH Cte(ArtistId,AlbumId,Name,Timestamp) AS 
            //    (VALUES (1,1,'Mutiny',1),(1,1,'Granite',2),(1,1,'Granite',3))
            //  SELECT * FROM Cte)) Cte
            //ON Cte.ArtistId = t.ArtistId AND Cte.AlbumId = t.AlbumId AND Cte.Name = t.Name

            return Context.Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {Context.Scrobbles.EntityType.GetTableName()}
({scrobbleUserIdColumn}, {scrobbleTrackIdColumn}, {scrobbleTimestampColumn})
SELECT @UserId AS {scrobbleUserIdColumn}, t.{trackIdColumn} as {scrobbleTrackIdColumn}, Cte.{scrobbleTimestampColumn} FROM {Context.Tracks.EntityType.GetTableName()} t
INNER JOIN
(SELECT * FROM
  (WITH Cte({trackArtistIdColumn},{trackAlbumIdColumn},{trackNameColumn},{scrobbleTimestampColumn}) AS
    (VALUES {sb})
  SELECT * FROM Cte)) Cte
ON Cte.{trackArtistIdColumn} = t.{trackArtistIdColumn} AND Cte.{trackAlbumIdColumn} = t.{trackAlbumIdColumn} AND Cte.{trackNameColumn} = t.{trackNameColumn}",
parameters);
        }

        [Benchmark(OperationsPerInvoke = TestData.ScrobbleDataCount)]
        public void DapperMultiValues()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                InsertUserArtistsAndAlbums();

                var uniqueTracks = TestData.ScrobbleData.Select(x => (queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)).Distinct();
                var uniqueTrackNames = uniqueTracks.Select(x => x.Track).Distinct();
                var newTrackCount = Context.InsertOrIgnore_Bulk(Context.Tracks.EntityType, (nameof(Track.ArtistId), nameof(Track.AlbumId), nameof(Track.Name)), uniqueTracks.Cast<ITuple>());

                //

                // Since we can't WHERE IN on a tuple list, we query duplicates here (A track is not only a name, but also an artist and album).
                // The benchmark is here to find if this is actually better to query too much and filter in app, or do everything in the db...
                var queriedTracks = Context.Tracks.Where(x => uniqueTrackNames.Contains(x.Name)).ToDictionary(x => (x.ArtistId, x.AlbumId, x.Name));

                var uniqueScrobbles = TestData.ScrobbleData.Select(x => ((long)1, queriedTracks[(queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)].Id, x.Timestamp)).Distinct();

                var inserted = InsertScrobblesOrIgnoreWithMultiValues(uniqueScrobbles);
                BenchmarkDebug.Assert(inserted == TestData.ScrobbleDataCount);
                transaction.Commit();
            }
        }

        private int InsertScrobblesOrIgnoreWithMultiValues(IEnumerable<(long UserId, long TrackId, long Timestamp)> data)
        {
            var scrobbleUserIdColumn = Context.GetColumnName(Context.Scrobbles.EntityType, nameof(Scrobble.UserId));
            var scrobbleTrackIdColumn = Context.GetColumnName(Context.Scrobbles.EntityType, nameof(Scrobble.TrackId));
            var scrobbleTimestampColumn = Context.GetColumnName(Context.Scrobbles.EntityType, nameof(Scrobble.Timestamp));

            var sb = new StringBuilder();
            int currentParamId = 0;
            DynamicParameters parameters = new DynamicParameters();
            string parameterPrefix = "@a";
            bool firstRow = true;
            foreach (var item in data)
            {
                if (firstRow)
                    firstRow = false;
                else
                    sb.Append(",");

                void AddParameter(object value, bool addTrailingComma = false)
                {
                    var name = $"{parameterPrefix}{currentParamId}";
                    parameters.Add(name, value);
                    sb.Append(name);
                    if (addTrailingComma) sb.Append(",");
                    currentParamId++;
                }

                sb.Append("(");
                AddParameter(item.UserId, true);
                AddParameter(item.TrackId, true);
                AddParameter(item.Timestamp);
                sb.Append(")");
            }

            //INSERT INTO 'tablename' ('column1', 'column2') VALUES
            //    ('data1', 'data2'),
            //    ('data1', 'data2'),
            //    ('data1', 'data2'),
            //    ('data1', 'data2');

            return Context.Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {Context.Scrobbles.EntityType.GetTableName()}
({scrobbleUserIdColumn}, {scrobbleTrackIdColumn}, {scrobbleTimestampColumn})
VALUES {sb}",
parameters);
        }

        [Benchmark(OperationsPerInvoke = TestData.ScrobbleDataCount)]
        public void DapperGenericLoop()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                InsertUserArtistsAndAlbums();

                var uniqueTracks = TestData.ScrobbleData.Select(x => (queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)).Distinct();
                var uniqueTrackNames = uniqueTracks.Select(x => x.Track).Distinct();
                var newTrackCount = Context.InsertOrIgnore_Loop(Context.Tracks.EntityType, (nameof(Track.ArtistId), nameof(Track.AlbumId), nameof(Track.Name)), uniqueTracks.Cast<ITuple>());

                //

                // Since we can't WHERE IN on a tuple list, we query duplicates here (A track is not only a name, but also an artist and album).
                // The benchmark is here to find if this is actually better to query too much and filter in app, or do everything in the db...
                var queriedTracks = Context.Tracks.Where(x => uniqueTrackNames.Contains(x.Name)).ToDictionary(x => (x.ArtistId, x.AlbumId, x.Name));

                var uniqueScrobbles = TestData.ScrobbleData.Select(x => (1, queriedTracks[(queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)].Id, x.Timestamp)).Distinct();
                var inserted = Context.InsertOrIgnore_Loop(Context.Scrobbles.EntityType, (nameof(Scrobble.UserId), nameof(Scrobble.TrackId), nameof(Scrobble.Timestamp)), uniqueScrobbles.Cast<ITuple>());

                BenchmarkDebug.Assert(inserted == TestData.ScrobbleDataCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.ScrobbleDataCount, Baseline = true)]
        public void DapperGenericBulk()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                InsertUserArtistsAndAlbums();

                var uniqueTracks = TestData.ScrobbleData.Select(x => (queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)).Distinct();
                var uniqueTrackNames = uniqueTracks.Select(x => x.Track).Distinct();
                var newTrackCount = Context.InsertOrIgnore_Bulk(Context.Tracks.EntityType, (nameof(Track.ArtistId), nameof(Track.AlbumId), nameof(Track.Name)), uniqueTracks.Cast<ITuple>());

                //

                // Since we can't WHERE IN on a tuple list, we query duplicates here (A track is not only a name, but also an artist and album).
                // The benchmark is here to find if this is actually better to query too much and filter in app, or do everything in the db...
                var queriedTracks = Context.Tracks.Where(x => uniqueTrackNames.Contains(x.Name)).ToDictionary(x => (x.ArtistId, x.AlbumId, x.Name));

                var uniqueScrobbles = TestData.ScrobbleData.Select(x => (1, queriedTracks[(queriedArtists[x.Artist].Id, queriedAlbums[x.Album].Id, x.Track)].Id, x.Timestamp)).Distinct();
                var inserted = Context.InsertOrIgnore_Bulk(Context.Scrobbles.EntityType, (nameof(Scrobble.UserId), nameof(Scrobble.TrackId), nameof(Scrobble.Timestamp)), uniqueScrobbles.Cast<ITuple>());

                BenchmarkDebug.Assert(inserted == TestData.ScrobbleDataCount);
                transaction.Commit();
            }
        }
    }
}
