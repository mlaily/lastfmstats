using BenchmarkDotNet.Attributes;
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
    [BenchmarkCategory("SELECT FROM Artists")]
    [IterationCount(100)]
    public class SelectArtist : BenchmarkBase
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

        [Benchmark(Description = "Dapper query", OperationsPerInvoke = TestData.PrepopulatedArtistsCount, Baseline = true)]
        public object Dapper()
        {
            var tableName = Context.Artists.EntityType.GetTableName();
            var result = Context.Database.GetDbConnection()
                .Query<Artist>($@"SELECT * FROM {tableName}")
                .ToList();
            BenchmarkDebug.Assert(result.Count == TestData.PrepopulatedArtistsCount);
            return result;
        }

        [Benchmark(Description = "EF Core query", OperationsPerInvoke = TestData.PrepopulatedArtistsCount)]
        public object EF()
        {
            var result = Context.Artists.ToList();
            BenchmarkDebug.Assert(result.Count == TestData.PrepopulatedArtistsCount);
            return result;
        }

        private static Func<MainContext, IEnumerable<Artist>> _getAllArtistsCompiled = Microsoft.EntityFrameworkCore.EF.CompileQuery((MainContext context) => context.Artists);

        [Benchmark(Description = "EF Core query (compiled)", OperationsPerInvoke = TestData.PrepopulatedArtistsCount)]
        public object EFCompiled()
        {
            var result = _getAllArtistsCompiled(Context).ToList();
            BenchmarkDebug.Assert(result.Count == TestData.PrepopulatedArtistsCount);
            return result;
        }
    }
}
