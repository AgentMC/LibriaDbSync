using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibriaDbSync
{
    public static class RssOnline
    {
        public const string ClassId = "эпизоды";

        [FunctionName("RssOnline")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            var content = new List<RssEntry>();
            using (var conn = await Shared.OpenConnection(log))
            {
                using (var loader = conn.CreateCommand())
                {
                    loader.CommandText = @"SELECT TOP 24 Episodes.Id as Uid, Title, Created, Titles, Code, Description, Poster, StatusCode, Genres, Voicers, Year, Season, Torrents
                                           FROM Episodes JOIN Releases ON Releases.Id = ReleaseId
                                           ORDER BY Created DESC";
                    using (var rdr = await loader.ExecuteReaderAsync())
                    {
                        while (rdr.Read())
                        {
                            content.Add(new RssEntry
                            {
                                Uid = (int)rdr["Uid"],
                                Title = (string)rdr["Title"],
                                Created = (long)rdr["Created"],
                                Release = Shared.ReadRelease(rdr)
                            });
                        }
                    }
                }
            }

            return RssFactory.BuildFeed(content, ClassId);
        }
    }
}
