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
    public abstract class BenchmarkBase
    {
        public const int ArtistAlreadyInDbCount = 2500;
        public const int ArtistNotInDbCount = 2500;
        public const int AllArtistsCount = 5000;
        protected IReadOnlyCollection<(long Id, string Name)> _artistsAlreadyInDb;
        protected IReadOnlyCollection<(long Id, string Name)> _artistsNotInDb;
        protected IReadOnlyCollection<(long Id, string Name)> _allArtists => _artistsAlreadyInDb.Concat(_artistsNotInDb).ToList();

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
                _artistsAlreadyInDb = TestData.GetArtists().Take(ArtistAlreadyInDbCount).ToList();
                Context.Artists.AddRange(_artistsAlreadyInDb.Select(x => new Artist { Name = x.Name }));
                Context.SaveChanges();
                transaction.Commit();
            }

            // Reset context
            Context.ChangeTracker.Clear();
            Context.Database.CloseConnection();
            Context.Database.OpenConnection();

            // Prepare reusable test data
            _artistsNotInDb = TestData.GetArtists().Skip(ArtistAlreadyInDbCount).Take(ArtistNotInDbCount).ToList();
        }

        [IterationSetup]
        public virtual void SetupIteration() { }
    }
}
