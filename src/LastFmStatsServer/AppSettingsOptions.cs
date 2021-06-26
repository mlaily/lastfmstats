using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastFmStatsServer
{
    public class GeneralOptions
    {
        public const string SectionName = "General";

        public string? DatabaseConnectionString { get; set; }
    }
}
