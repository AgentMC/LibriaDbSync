using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibriaDbSync
{
    public static class RssTorrent
    {
        [FunctionName("RssTorrent")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            var content = new List<RssEntry>();
            using (var conn = await Shared.OpenConnection(log))
            {
                using (var loader = conn.CreateCommand())
                {
                    loader.CommandText = @"SELECT TOP 24 Torrents.Id as Uid, Created, Titles, Code, Description, Poster, StatusCode, Genres, Voicers, Year, Season, Torrents
                                           FROM Torrents JOIN Releases ON Releases.Id = ReleaseId
                                           ORDER BY Created DESC";
                    using (var rdr = await loader.ExecuteReaderAsync())
                    {
                        while (rdr.Read())
                        {
                            content.Add(new RssEntry
                            {
                                Uid = (int)rdr["Uid"],
                                Created = (long)rdr["Created"],
                                Release = Shared.ReadRelease(rdr)
                            });
                        }
                    }
                }
            }
            content.ForEach(r => { var tor = r.Release.torrents.Single(t => t.id == r.Uid); r.Title = $"Серии {tor.series} [{tor.quality}]"; });

            return RssFactory.BuildFeed(content, "торренты");
        }
    }
}
