using BenchmarkDotNet.Running;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            // https://github.com/StackExchange/Dapper/pull/720
            SqlMapper.AssumeColumnsAreStronglyTyped = false;

            //Testing.Debug();

            //var bla = new Bla();
            //bla.Setup();
            //bla.SetupIteration();
            //bla.Test1();

            BenchmarkRunner.Run(typeof(BenchmarkBase).Assembly, new Config());

        }
    }

    static class BenchmarkDebug
    {
        //[Conditional("NEVER")] // Uncomment to disable assertions
        public static void Assert(bool condition, string onConditionFalse = null)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed!\n{onConditionFalse}");
            }
        }
    }

    static class Testing
    {
        public static void Debug()
        {

        }
    }
}
