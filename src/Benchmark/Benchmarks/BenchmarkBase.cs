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
        public MainContext Context { get; private set; }

        [GlobalSetup]
        public virtual void InitializeDb()
        {
            TestData.Initialize();

            // Restart with an empty db
            Context?.Dispose();
            Context = new MainContext();
            Context.Database.EnsureDeleted();
            Context.Database.EnsureCreated();

            // Populate it with some data
            PopulateDb();

            // Reset context
            Context.ChangeTracker.Clear();
            Context.Database.CloseConnection();
            Context.Database.OpenConnection();
        }

        protected virtual void PopulateDb()
        {
        }

        [IterationSetup]
        public virtual void SetupIteration() { }
    }
}
