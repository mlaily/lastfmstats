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
    [BenchmarkCategory(nameof(SelectArtist))]
    public class SelectArtist : BenchmarkBase
    {
        [Benchmark(OperationsPerInvoke = ArtistBatchCount, Baseline = true)]
        public object Dapper()
        {
            var result = Context.Database.GetDbConnection()
                .Query<Artist>($@"SELECT * FROM {Context.Artists.EntityType.GetTableName()}")
                .ToList();
            BenchmarkDebug.Assert(result.Count == ArtistBatchCount);
            return result;
        }

        [Benchmark(OperationsPerInvoke = ArtistBatchCount)]
        public object Ef()
        {
            var result = Context.Artists.ToList();
            BenchmarkDebug.Assert(result.Count == ArtistBatchCount);
            return result;
        }
    }
}
