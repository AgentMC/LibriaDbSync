using LibriaDbSync.LibApi;
using LibriaDbSync.LibApi.V1;
using LibriaDbSync.LibApi.V2;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace LibriaDbSync
{
    static class Shared
    {
        internal static async Task<SqlConnection> OpenConnection(ILogger log)
        {
            var connection = new SqlConnection(Environment.GetEnvironmentVariable("sqldb_connection"));
            await connection.OpenAsync();
            log.LogInformation($"Connection to DB opened successfully.");
            return connection;
        }

        internal static DateTime ToDateTime(this long seconds) => new DateTime(1970, 1, 1).AddSeconds(seconds);

        internal static string ToRssDateTimeString(this long seconds) => ToDateTime(seconds).ToString("R");

        internal static long ToUnixTimeStamp(this DateTime dateTime) => (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;

        private static readonly Func<ILibriaExtractor>[] Extractors = new Func<ILibriaExtractor>[] 
        { 
            () => new LibriaExtractorV1(), 
            () => new LibriaExtractorV2() 
        };

        internal static ILibriaExtractor GetExtractor() => Extractors[^1]();

        internal const int Threshold = 50;
    }
}
