using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Dapper;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;

namespace LastFmStatsServer
{
    public partial class MainContext : DbContext
    {
        public MainContext()
        {
        }

        public MainContext(DbContextOptions<MainContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Album> Albums { get; set; }
        public virtual DbSet<Artist> Artists { get; set; }
        public virtual DbSet<Scrobble> Scrobbles { get; set; }
        public virtual DbSet<Track> Tracks { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Album>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Artist>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Scrobble>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.TrackId, e.Timestamp })
                    .IsUnique();
                entity.HasIndex(e => e.Timestamp);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Timestamp)
                    .IsRequired();

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.Scrobbles)
                    .HasForeignKey(d => d.TrackId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Scrobbles)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Track>(entity =>
            {
                entity.HasIndex(e => new { e.ArtistId, e.AlbumId, e.Name })
                    .IsUnique();
                entity.HasIndex(e => e.AlbumId);
                entity.HasIndex(e => e.ArtistId);
                entity.HasIndex(e => e.Name);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Name).IsRequired();

                entity.HasOne(d => d.Album)
                    .WithMany(p => p.Tracks)
                    .HasForeignKey(d => d.AlbumId);

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.Tracks)
                    .HasForeignKey(d => d.ArtistId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Name).IsRequired();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);


        public string GetColumnName(IEntityType entityType, string targetPropertyName)
            => entityType.FindProperty(targetPropertyName).GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema()));

        public int InsertOrIgnore(IEntityType entityType, string targetPropertyName, IEnumerable<string> values)
            => InsertOrIgnore(entityType, ValueTuple.Create(targetPropertyName), values.Select(x => (ITuple)ValueTuple.Create(x)));

        public int InsertOrIgnore(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<ITuple> values)
        {
            if (targetPropertyNames.Length != values.FirstOrDefault().Length) throw new ArgumentException($"{nameof(targetPropertyNames)} and {nameof(values)} must have the same arity!");
            var columnNames = GetTupleItems(targetPropertyNames).Cast<string>().Select(x => GetColumnName(entityType, x)).ToList();
            var formattedColumnNames = string.Join(", ", columnNames);
            var formattedParameterizedColumnNames = string.Join(", ", columnNames.Select(x => $"@{x}"));

            int inserted = 0;
            foreach (var columnValues in values)
            {
                inserted += Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {entityType.GetTableName()}
({formattedColumnNames})
VALUES ({formattedParameterizedColumnNames})",
GetTupleItems(columnValues).Select((x, i) => KeyValuePair.Create(columnNames[i], x)));
            }
            return inserted;
        }

        public int InsertOrIgnore_Alternative_With_CTE(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<ITuple> values)
        {
            if (targetPropertyNames.Length != values.FirstOrDefault().Length) throw new ArgumentException($"{nameof(targetPropertyNames)} and {nameof(values)} must have the same arity!");
            var columnNames = GetTupleItems(targetPropertyNames).Cast<string>().Select(x => GetColumnName(entityType, x)).ToList();
            var formattedColumnNames = string.Join(", ", columnNames);

            var sb = new StringBuilder();
            int currentParamId = 0;
            DynamicParameters parameters = new DynamicParameters();
            string parameterPrefix = "@a";
            bool firstRow = true;
            foreach (var item in values)
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
                for (int i = 0; i < item.Length; i++)
                {
                    AddParameter(item[i], i + 1 != item.Length);
                }
                sb.Append(")");
            }

            return Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {entityType.GetTableName()}
({formattedColumnNames})
SELECT * FROM
  (WITH Cte({formattedColumnNames}) AS
    (VALUES {sb})
  SELECT * FROM Cte) Cte",
  parameters);
        }

        public int InsertScrobblesOrIgnore(long userId, IEnumerable<(long ArtistId, long AlbumId, string TrackName, long Timestamp)> data)
        {
            var scrobbleUserIdColumn = GetColumnName(Scrobbles.EntityType, nameof(Scrobble.UserId));
            var scrobbleTrackIdColumn = GetColumnName(Scrobbles.EntityType, nameof(Scrobble.TrackId));
            var scrobbleTimestampColumn = GetColumnName(Scrobbles.EntityType, nameof(Scrobble.Timestamp));

            var trackIdColumn = GetColumnName(Tracks.EntityType, nameof(Track.Id));
            var trackArtistIdColumn = GetColumnName(Tracks.EntityType, nameof(Track.ArtistId));
            var trackAlbumIdColumn = GetColumnName(Tracks.EntityType, nameof(Track.AlbumId));
            var trackNameColumn = GetColumnName(Tracks.EntityType, nameof(Track.Name));

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

            return Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {Scrobbles.EntityType.GetTableName()}
({scrobbleUserIdColumn}, {scrobbleTrackIdColumn}, {scrobbleTimestampColumn})
SELECT @UserId AS {scrobbleUserIdColumn}, t.{trackIdColumn} as {scrobbleTrackIdColumn}, Cte.{scrobbleTimestampColumn} FROM {Tracks.EntityType.GetTableName()} t
INNER JOIN
(SELECT * FROM
  (WITH Cte({trackArtistIdColumn},{trackAlbumIdColumn},{trackNameColumn},{scrobbleTimestampColumn}) AS
    (VALUES {sb})
  SELECT * FROM Cte)) Cte
ON Cte.{trackArtistIdColumn} = t.{trackArtistIdColumn} AND Cte.{trackAlbumIdColumn} = t.{trackAlbumIdColumn} AND Cte.{trackNameColumn} = t.{trackNameColumn}",
parameters);
        }

        private static IEnumerable<object> GetTupleItems(ITuple tuple)
            => from i in Enumerable.Range(0, tuple.Length) select tuple[i];
    }
}
