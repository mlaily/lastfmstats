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
    [Description("Dapper")]
    public class BenchmarkDapper_Insert : BenchmarkBase
    {
        public override void SetupIteration()
        {
            base.SetupIteration();
            InitializeDb();
        }

        [Benchmark(Description = "INSERT Artist")]
        public void Execute()
        {
            System.Threading.Thread.Sleep(1);
            return;
            using (var transaction = Context.Database.BeginTransaction())
            {
                foreach (var item in _artistsToInsert)
                {
                    Context.Database.GetDbConnection().Execute($@"
INSERT INTO {Context.Artists.EntityType.GetTableName()}
({Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name))})
VALUES (@Name)",
new { Name = item.Name });
                }
                transaction.Commit();
            }
        }
    }

    [Description("Dapper")]
    public class BenchmarkDapper_Select : BenchmarkBase
    {
        [Benchmark(Description = "SELECT Artist")]
        public object Execute()
        {
            var result = Context.Database.GetDbConnection()
                .Query<(long Id, string Name)>($@"SELECT * FROM {Context.Artists.EntityType.GetTableName()}")
                .ToList();
            BenchmarkDebug.Assert(result.Count == 2500);
            return result;
        }
    }
}
