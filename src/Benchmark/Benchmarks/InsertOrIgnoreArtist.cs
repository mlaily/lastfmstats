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
    [IterationCount(20)]
    public class InsertOrIgnoreArtist : BenchmarkBase
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

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void DapperRawLoop()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var nameColumn = Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name));
                int inserted = 0;
                foreach (var item in TestData.PrepopulatedAndNewArtists)
                {
                    inserted += Context.Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {Context.Artists.EntityType.GetTableName()}
({nameColumn})
VALUES (@Name)",
new { Name = item.Name });
                }
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void DapperRawBulk()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var nameColumn = Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name));
                var inserted = Context.Database.GetDbConnection().Execute($@"
INSERT OR IGNORE INTO {Context.Artists.EntityType.GetTableName()}
({nameColumn})
VALUES (@Name)",
TestData.PrepopulatedAndNewArtists.Select(x => new { Name = x.Name }));

                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void DapperGenericBulk()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var nameColumn = Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name));
                var inserted = Context.InsertOrIgnore_Bulk(
                    Context.Artists.EntityType,
                    Context.CreateTuple(nameof(Artist.Name)),
                    Context.SelectTuples(TestData.PrepopulatedAndNewArtists.Select(x => x.Name)));

                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void DapperGenericBulkArray()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var nameColumn = Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name));
                var inserted = Context.InsertOrIgnore_BulkArray(
                    Context.Artists.EntityType,
                    new[] { nameof(Artist.Name) },
                    TestData.PrepopulatedAndNewArtists.Select(x => new[] { x.Name }));

                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount, Baseline = true)]
        public void GenericDbCommand()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var nameColumn = Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name));
                var inserted = Context.InsertOrIgnore_DbCommand(
                    Context.Artists.EntityType,
                    new[] { nameof(Artist.Name) },
                    TestData.PrepopulatedAndNewArtists.Select(x => new[] { x.Name }));

                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void DapperGenericLoop()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var inserted = Context.InsertOrIgnore_Loop(
                    Context.Artists.EntityType,
                    Context.CreateTuple(nameof(Artist.Name)),
                    Context.SelectTuples(TestData.PrepopulatedAndNewArtists.Select(x => x.Name)));
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void DapperGenericLoopDynamicParameters()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var inserted = Context.InsertOrIgnore_DynamicParameters(
                    Context.Artists.EntityType,
                    Context.CreateTuple(nameof(Artist.Name)),
                    Context.SelectTuples(TestData.PrepopulatedAndNewArtists.Select(x => x.Name)));
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void DapperCTE()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var inserted = Context.InsertOrIgnore_Alternative_With_CTE(
                    Context.Artists.EntityType,
                    Context.CreateTuple(nameof(Artist.Name)),
                    Context.SelectTuples(TestData.PrepopulatedAndNewArtists.Select(x => x.Name)));
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }


        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void DapperMultiValues()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var inserted = Context.InsertOrIgnore_Alternative_With_MultiValues(
                    Context.Artists.EntityType,
                    Context.CreateTuple(nameof(Artist.Name)),
                    Context.SelectTuples(TestData.PrepopulatedAndNewArtists.Select(x => x.Name)));
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = TestData.PrepopulatedAndNewArtistsCount)]
        public void Ef()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                var alreadyExisting = Context.Artists.Where(x => TestData.PrepopulatedAndNewArtists.Select(x => x.Name).Contains(x.Name)).ToDictionary(x => x.Name);
                BenchmarkDebug.Assert(alreadyExisting.Count == TestData.PrepopulatedArtistsCount);
                var toInsert = TestData.PrepopulatedAndNewArtists.Where(x => alreadyExisting.ContainsKey(x.Name) == false).Select(x => new Artist { Name = x.Name });
                Context.Artists.AddRange(toInsert);
                int inserted = Context.SaveChanges();
                BenchmarkDebug.Assert(inserted == TestData.NewArtistsCount);
                transaction.Commit();
            }
        }
    }
}
