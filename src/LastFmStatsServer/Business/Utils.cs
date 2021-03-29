using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastFmStatsServer.Business
{
    public static class Utils
    {
        public static long LastFmScrobbleDateLowerBound { get; } = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        /// <summary>
        /// http://colorbrewer2.org/#type=qualitative&scheme=Paired&n=12
        /// </summary>
        public static IReadOnlyList<string> Colors { get; } = new[]
        {
            "#a6cee3",
            "#1f78b4",
            "#b2df8a",
            "#33a02c",
            "#fb9a99",
            "#e31a1c",
            "#fdbf6f",
            "#ff7f00",
            "#cab2d6",
            "#6a3d9a",
            "#fff137", // changed. too light when opacity is not 1
            "#b15928",
        };

        public static string NormalizeEmpty(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        public static string NormalizeUserName(string userName) => NormalizeEmpty(userName?.ToLowerInvariant());

        /// <summary>
        /// https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
        /// </summary>
        private static uint GetDeterministicHashCode(string str)
        {
            unchecked
            {
                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        public static T MapToArrayValue<T>(string input, IReadOnlyList<T> values)
        {
            uint hash = GetDeterministicHashCode(input);
            var index = unchecked((int)(hash % (uint)values.Count));
            return values[index];
        }
    }
}
