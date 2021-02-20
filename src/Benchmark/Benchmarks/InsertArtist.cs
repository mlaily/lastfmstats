﻿using BenchmarkDotNet.Attributes;
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
    [BenchmarkCategory(nameof(InsertArtist))]
    public class InsertArtist : BenchmarkBase
    {
        public override void SetupIteration()
        {
            base.SetupIteration();
            InitializeDb();
        }

        [Benchmark(OperationsPerInvoke = ArtistNotInDbCount, Baseline = true)]
        public void Dapper()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                int inserted = 0;
                foreach (var item in _artistsNotInDb)
                {
                    inserted += Context.Database.GetDbConnection().Execute($@"
INSERT INTO {Context.Artists.EntityType.GetTableName()}
({Context.GetColumnName(Context.Artists.EntityType, nameof(Artist.Name))})
VALUES (@Name)",
new { Name = item.Name });
                }
                BenchmarkDebug.Assert(inserted == ArtistNotInDbCount);
                transaction.Commit();
            }
        }

        [Benchmark(OperationsPerInvoke = ArtistNotInDbCount)]
        public void Ef()
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                Context.Artists.AddRange(_artistsNotInDb.Select(x => new Artist { Name = x.Name }));
                int inserted = Context.SaveChanges();
                BenchmarkDebug.Assert(inserted == ArtistNotInDbCount);
                transaction.Commit();
            }
        }
    }
}