using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [BenchmarkCategory(nameof(InsertOrIgnoreArtist))]
    public class InsertOrIgnoreArtist : BenchmarkBase
    {
        public override void SetupIteration()
        {
            base.SetupIteration();
            InitializeDb();
        }

        [Benchmark(OperationsPerInvoke = AllArtistsCount, Baseline = true)]
        public void Dapper()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                int inserted = 0;
                foreach (var item in _allArtists)
                {
                    inserted += Context.Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {Context.Artists.EntityType.GetTableName()}
({Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name))})
VALUES (@Name)",
new { Name = item.Name });
                }
                BenchmarkDebug.Assert(inserted == ArtistNotInDbCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = AllArtistsCount)]
        public void Ef()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var alreadyExisting = Context.Artists.Where(x => _allArtists.Select(x => x.Name).Contains(x.Name)).ToDictionary(x => x.Name);
                BenchmarkDebug.Assert(alreadyExisting.Count == ArtistAlreadyInDbCount);
                var toInsert = _allArtists.Where(x => alreadyExisting.ContainsKey(x.Name) == false).Select(x => new Artist { Name = x.Name });
                Context.Artists.AddRange(toInsert);
                int inserted = Context.SaveChanges();
                BenchmarkDebug.Assert(inserted == ArtistNotInDbCount);
                transaction.Commit();
            }
        }
    }
}
