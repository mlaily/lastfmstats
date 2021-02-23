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
    [BenchmarkCategory("INSERT INTO Artists")]
    [IterationCount(20)]
    public class InsertArtist : BenchmarkBase
    {
        protected override void PopulateDb()
        {
            base.PopulateDb();
            using (var transaction = Context.Database.BeginTransaction())
            {
                Context.Artists.AddRange(TestData.PrepopulatedArtistNames.Select(x => new Artist { Name = x }));
                Context.SaveChanges();
                transaction.Commit();
            }
        }

        public override void SetupIteration()
        {
            base.SetupIteration();
            InitializeDb();
        }

        [Benchmark(Description = "Hard-coded loop", OperationsPerInvoke = TestData.NewArtistsCount)]
        public void DapperLoop()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var nameColumn = Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name));
                int inserted = 0;
                foreach (var item in TestData.NewArtistNames)
                {
                    inserted += Context.Database.GetDbConnection().Execute($@"
INSERT INTO {Context.Artists.EntityType.GetTableName()}
({nameColumn})
VALUES (@Name)",
new { Name = item });
                }
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(Description = "Hard-coded bulk", OperationsPerInvoke = TestData.NewArtistsCount, Baseline = true)]
        public void DapperRawBulk()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var nameColumn = Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name));
                int inserted = Context.Database.GetDbConnection().Execute($@"
INSERT INTO {Context.Artists.EntityType.GetTableName()}
({nameColumn})
VALUES (@Name)",
TestData.NewArtistNames.Select(x => new { Name = x }));
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(Description = "EF Core", OperationsPerInvoke = TestData.NewArtistsCount)]
        public void EF()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                Context.Artists.AddRange(TestData.NewArtistNames.Select(x => new Artist { Name = x }));
                int inserted = Context.SaveChanges();
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }
    }
}
