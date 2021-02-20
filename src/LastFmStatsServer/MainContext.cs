using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Dapper;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Diagnostics;

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

        public int InsertOrIgnore<TItem1>(IEntityType entityType, string targetPropertyNames, IEnumerable<TItem1> values)
            => InsertOrIgnoreImplementation(entityType, ValueTuple.Create(targetPropertyNames), values.Select(x => ValueTuple.Create(x)));
        public int InsertOrIgnore<TItem1, TItem2>(IEntityType entityType, (string, string) targetPropertyNames, IEnumerable<(TItem1, TItem2)> values)
            => InsertOrIgnoreImplementation(entityType, targetPropertyNames, values);
        public int InsertOrIgnore<TItem1, TItem2, TItem3>(IEntityType entityType, (string, string, string) targetPropertyNames, IEnumerable<(TItem1, TItem2, TItem3)> values)
            => InsertOrIgnoreImplementation(entityType, targetPropertyNames, values);
        public int InsertOrIgnore<TItem1, TItem2, TItem3, TItem4>(IEntityType entityType, (string, string, string, string) targetPropertyNames, IEnumerable<(TItem1, TItem2, TItem3, TItem4)> values)
            => InsertOrIgnoreImplementation(entityType, targetPropertyNames, values);

        private int InsertOrIgnoreImplementation<TTuple>(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<TTuple> values) where TTuple : ITuple
        {
            var columnNames = GetTupleItems(targetPropertyNames).Cast<string>().Select(x => GetColumnName(entityType, x)).ToList();
            var formattedColumnNames = string.Join(',', columnNames);
            var formattedParameterizedColumnNames = string.Join(',', columnNames.Select(x => $"@{x}"));

            // https://stackoverflow.com/questions/9006604/improve-performance-of-sqlite-bulk-inserts-using-dapper-orm
            var command = Database.GetDbConnection().CreateCommand();
            command.CommandText = $@"
INSERT OR IGNORE INTO {entityType.GetTableName()}
({formattedColumnNames})
VALUES ({formattedParameterizedColumnNames})";

            // Create parameters with their names:
            var parameters = columnNames.Select(x =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = x;
                command.Parameters.Add(parameter);
                return parameter;
            }).ToList();

            int inserted = 0;
            // Execute the command, reusing the parameters:
            foreach (var tuple in values)
            {
                for (int i = 0; i < columnNames.Count; i++)
                {
                    parameters[i].Value = tuple[i];
                }

                inserted += command.ExecuteNonQuery();
            }
            return inserted;
        }

        // Prettier code, but slower:
        //        public int InsertOrIgnore(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<ITuple> values)
        //        {
        //            if (targetPropertyNames.Length != values.FirstOrDefault().Length) throw new ArgumentException($"{nameof(targetPropertyNames)} and {nameof(values)} must have the same arity!");
        //            var columnNames = GetTupleItems(targetPropertyNames).Cast<string>().Select(x => GetColumnName(entityType, x)).ToList();
        //            var formattedColumnNames = string.Join(", ", columnNames);
        //            var formattedParameterizedColumnNames = string.Join(", ", columnNames.Select(x => $"@{x}"));

        //            int inserted = Database.GetDbConnection().Execute($@"
        //INSERT OR IGNORE INTO {entityType.GetTableName()}
        //({formattedColumnNames})
        //VALUES ({formattedParameterizedColumnNames})",
        //values.Select(tuple => GetTupleItems(tuple).Select((x, i) => KeyValuePair.Create(columnNames[i], x))));
        //            return inserted;
        //        }

        private static IEnumerable<object> GetTupleItems(ITuple tuple)
        {
            for (int i = 0; i < tuple.Length; i++)
                yield return tuple[i];
        }
    }
}
