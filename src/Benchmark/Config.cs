using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    public class Config : ManualConfig
    {
        public Config()
        {
            AddLogger(ConsoleLogger.Default);

            AddExporter(CsvExporter.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddExporter(HtmlExporter.Default);

            var md = MemoryDiagnoser.Default;
            AddDiagnoser(md);
            //AddColumn(CategoriesColumn.Default);
            AddColumn(TargetMethodColumn.Type);
            AddColumn(TargetMethodColumn.Method);
            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(StatisticColumn.Error);
            AddColumn(BaselineRatioColumn.RatioMean);
            AddColumn(new OperationsPerInvokeColumn());
            AddColumnProvider(DefaultColumnProviders.Metrics);

            AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);

            AddJob(Job.ShortRun
                   .WithLaunchCount(1)
                   .WithWarmupCount(5)
                   .WithIterationCount(20)
            );
            Orderer = new DefaultOrderer(summaryOrderPolicy: SummaryOrderPolicy.Declared);
            Options |= ConfigOptions.JoinSummary | ConfigOptions.StopOnFirstError;
        }
    }

    public class OperationsPerInvokeColumn : IColumn
    {
        public string Id => nameof(OperationsPerInvokeColumn);
        public string ColumnName => "Operations";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Metric;
        public int PriorityInCategory => -10;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Size;
        public string Legend => "Operations Per Invoke";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => benchmarkCase.Descriptor.OperationsPerInvoke.ToString();

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => benchmarkCase.Descriptor.OperationsPerInvoke.ToString();

        public bool IsAvailable(Summary summary) => true;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => benchmarkCase.Descriptor.OperationsPerInvoke == 1;
    }
}
