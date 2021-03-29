using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastFmStatsServer.Plumbing
{
    public class HttpResponseException<TError> : Exception
    {
        public int Status { get; set; } = 500;

        public TError Value { get; set; }
    }
}
