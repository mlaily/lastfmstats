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
    [Description("Dapper")]
    public class BenchmarkDapper_Insert : BenchmarkBase
    {
        public override void SetupIteration()
        {
            base.SetupIteration();
            InitializeDb();
        }

        [BenchmarkCategory("INSERT Artist")]
        [Benchmark(OperationsPerInvoke = ArtistBatchCount, Baseline = true)]
        public void Execute()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                int inserted = 0;
                foreach (var item in _artistsToInsert)
                {
                    inserted += Context.Database.GetDbConnection().Execute($@"
INSERT INTO {Context.Artists.EntityType.GetTableName()}
({Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name))})
VALUES (@Name)",
new { Name = item.Name });
                }
                BenchmarkDebug.Assert(inserted == ArtistBatchCount);
                transaction.Commit();
            }
        }
    }

    [Description("Dapper")]
    public class BenchmarkDapper_Select : BenchmarkBase
    {
        [BenchmarkCategory("SELECT Artist")]
        [Benchmark(OperationsPerInvoke = ArtistBatchCount, Baseline = true)]
        public object Execute()
        {
            var result = Context.Database.GetDbConnection()
                .Query<(long Id, string Name)>($@"SELECT * FROM {Context.Artists.EntityType.GetTableName()}")
                .ToList();
            BenchmarkDebug.Assert(result.Count == ArtistBatchCount);
            return result;
        }
    }
}
