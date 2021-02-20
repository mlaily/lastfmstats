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
    [Description("EF")]
    public class BenchmarkEF_Insert : BenchmarkBase
    {
        public override void SetupIteration()
        {
            InitializeDb();
        }

        [BenchmarkCategory("INSERT Artist")]
        [Benchmark(OperationsPerInvoke = ArtistBatchCount)]
        public void Execute()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                Context.Artists.AddRange(_artistsToInsert.Select(x => new Artist { Name = x.Name }));
                int inserted = Context.SaveChanges();
                BenchmarkDebug.Assert(inserted == ArtistBatchCount);
                transaction.Commit();
            }
        }
    }

    [Description("EF")]
    public class BenchmarkEF_Select : BenchmarkBase
    {
        [BenchmarkCategory("SELECT Artist")]
        [Benchmark(OperationsPerInvoke = ArtistBatchCount)]
        public object Execute()
        {
            var result = Context.Artists.ToList();
            BenchmarkDebug.Assert(result.Count == ArtistBatchCount);
            return result;
        }
    }
}
