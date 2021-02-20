using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite(ConfigurationManager.ConnectionStrings["Main"].ConnectionString);
        }

        public string GetColumnName(IEntityType entityType, string targetPropertyName)
            => entityType.FindProperty(targetPropertyName).GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema()));

        public int InsertOrIgnore_Bulk(IEntityType entityType, string targetPropertyName, IEnumerable<string> values)
    => InsertOrIgnore_Bulk(entityType, ValueTuple.Create(targetPropertyName), values.Select(x => (ITuple)ValueTuple.Create(x)));

        public ITuple CreateTuple<T>(T value) => ValueTuple.Create(value);
        public IEnumerable<ITuple> SelectTuples<T>(IEnumerable<T> values) => values.Select(x => (ITuple)ValueTuple.Create(x));

        public int InsertOrIgnore_Loop(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<ITuple> values)
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

        public int InsertOrIgnore_Bulk(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<ITuple> values)
        {
            if (targetPropertyNames.Length != values.FirstOrDefault().Length) throw new ArgumentException($"{nameof(targetPropertyNames)} and {nameof(values)} must have the same arity!");
            var columnNames = GetTupleItems(targetPropertyNames).Cast<string>().Select(x => GetColumnName(entityType, x)).ToList();
            var formattedColumnNames = string.Join(", ", columnNames);
            var formattedParameterizedColumnNames = string.Join(", ", columnNames.Select(x => $"@{x}"));

            int inserted = Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {entityType.GetTableName()}
({formattedColumnNames})
VALUES ({formattedParameterizedColumnNames})",
values.Select(tuple => GetTupleItems(tuple).Select((x, i) => KeyValuePair.Create(columnNames[i], x))));
            return inserted;
        }

        public int InsertOrIgnore_DynamicParameters(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<ITuple> values)
        {
            if (targetPropertyNames.Length != values.FirstOrDefault().Length) throw new ArgumentException($"{nameof(targetPropertyNames)} and {nameof(values)} must have the same arity!");
            var columnNames = GetTupleItems(targetPropertyNames).Cast<string>().Select(x => GetColumnName(entityType, x)).ToList();
            var formattedColumnNames = string.Join(", ", columnNames);
            var formattedParameterizedColumnNames = string.Join(", ", columnNames.Select(x => $"@{x}"));

            int inserted = 0;
            foreach (var columnValues in values)
            {
                var parameters = GetTupleItems(columnValues).Select((x, i) =>
                {
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add(columnNames[i], x);
                    return dynamicParameters;
                });

                inserted += Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {entityType.GetTableName()}
({formattedColumnNames})
VALUES ({formattedParameterizedColumnNames})",
parameters);
            }
            return inserted;
        }

        public int InsertOrIgnore_Alternative_With_CTE(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<ITuple> values)
        {
            if (targetPropertyNames.Length != values.FirstOrDefault().Length) throw new ArgumentException($"{nameof(targetPropertyNames)} and {nameof(values)} must have the same arity!");
            var columnNames = GetTupleItems(targetPropertyNames).Cast<string>().Select(x => GetColumnName(entityType, x)).ToList();
            var formattedColumnNames = string.Join(", ", columnNames);

            var (fragment, parameters) = GetParameters(values);

            return Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {entityType.GetTableName()}
({formattedColumnNames})
SELECT * FROM
  (WITH Cte({formattedColumnNames}) AS
    (VALUES {fragment})
  SELECT * FROM Cte) Cte",
  parameters);
        }

        public int InsertOrIgnore_Alternative_With_MultiValues(IEntityType entityType, ITuple targetPropertyNames, IEnumerable<ITuple> values)
        {
            if (targetPropertyNames.Length != values.FirstOrDefault().Length) throw new ArgumentException($"{nameof(targetPropertyNames)} and {nameof(values)} must have the same arity!");
            var columnNames = GetTupleItems(targetPropertyNames).Cast<string>().Select(x => GetColumnName(entityType, x)).ToList();
            var formattedColumnNames = string.Join(", ", columnNames);

            var (fragment, parameters) = GetParameters(values);

            return Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {entityType.GetTableName()}
({formattedColumnNames})
VALUES {fragment}",
  parameters);
        }

        private (string fragment, DynamicParameters parameters) GetParameters(IEnumerable<ITuple> values)
        {
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
            return (sb.ToString(), parameters);
        }

        private static IEnumerable<object> GetTupleItems(ITuple tuple)
            => from i in Enumerable.Range(0, tuple.Length) select tuple[i];
    }
}
