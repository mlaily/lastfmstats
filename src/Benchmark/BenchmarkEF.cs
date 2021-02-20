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

        [Benchmark(Description = "INSERT Artist")]
        public void Execute()
        {
            System.Threading.Thread.Sleep(1);
            return;
            using (var transaction = Context.Database.BeginTransaction())
            {
                Context.Artists.AddRange(_artistsToInsert.Select(x => new Artist { Name = x.Name }));
                Context.SaveChanges();
                transaction.Commit();
            }
        }
    }

    [Description("EF")]
    public class BenchmarkEF_Select : BenchmarkBase
    {
        [Benchmark(Description = "SELECT Artist")]
        public object Execute()
        {
            var result = Context.Artists.ToList();
            BenchmarkDebug.Assert(result.Count == 2500);
            return result;
        }
    }
}
