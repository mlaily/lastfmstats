using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace LastFmStatsServer.Dapper
{
    public class UtcDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
        {
            // If a TZ info is present, use it. Otherwise, default to UTC
            return DateTimeOffset.Parse((string)value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal);
        }

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            // Warning: Dapper cannot read a DateTimeOffset value, but is able to write one,
            // so this code is actually never called...
            parameter.Value = value;
        }
    }
}
