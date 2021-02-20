using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [BenchmarkCategory("Data access")]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public abstract class BenchmarkBase
    {
        public const int ArtistBatchCount = 2500;
        protected IReadOnlyCollection<(long Id, string Name)> _artistsToInsert;

        public MainContext Context { get; private set; }

        [GlobalSetup]
        public virtual void InitializeDb()
        {
            // Restart with an empty db
            Context?.Dispose();
            Context = new MainContext();
            Context.Database.EnsureDeleted();
            Context.Database.EnsureCreated();

            // Populate it with some data
            using (var transaction = Context.Database.BeginTransaction())
            {
                var first2500Artists = TestData.GetArtists().Take(ArtistBatchCount).ToList();
                Context.Artists.AddRange(first2500Artists.Select(x => new Artist { Name = x.Name }));
                Context.SaveChanges();
                transaction.Commit();
            }

            // Reset context
            Context.ChangeTracker.Clear();
            Context.Database.CloseConnection();
            Context.Database.OpenConnection();

            // Prepare reusable test data
            _artistsToInsert = TestData.GetArtists().Skip(ArtistBatchCount).Take(ArtistBatchCount).ToList();
        }

        [IterationSetup]
        public virtual void SetupIteration() { }
    }
}
