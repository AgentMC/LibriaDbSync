using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using LibriaDbSync.Properties;

namespace LibriaDbSync
{
    public static class RssOnline
    {
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
                                Release = new Release
                                {
                                    names = JsonConvert.DeserializeObject<List<string>>((string)rdr["Titles"]),
                                    code = (string)rdr["Code"],
                                    description = (string)rdr["Description"],
                                    poster = (string)rdr["Poster"],
                                    StatusCode = (byte)rdr["StatusCode"],
                                    genres = JsonConvert.DeserializeObject<List<string>>((string)rdr["Genres"]),
                                    voices = JsonConvert.DeserializeObject<List<string>>((string)rdr["Voicers"]),
                                    Year = (short)rdr["Year"],
                                    season = (string)rdr["Season"],
                                    torrents = JsonConvert.DeserializeObject<List<Torrent>>((string)rdr["Torrents"]),
                                }
                            });
                        }
                    }
                }
            }

            var ch = new XElement("channel",
                        new XElement("title", "Anilibria — так звучит аниме!"),
                        new XElement("link", "https://www.anilibria.tv"),
                        new XElement("description", "Неофициальная RSS-лента по сайту Anilibria.tv"),
                        new XElement("language", "ru-ru"),
                        new XElement("copyright", "Все права на контент в этом канале принадлежат сайту Anilibria.tv. Все права на код синхронизатора базы и генератора ленты принадлежат AgentMC."),
                        new XElement("webMaster", "agentmc@mail.ru (AgentMC)"),
                        new XElement("lastBuildDate", content[0].Created.ToDateTime().ToString("R")),
                        new XElement("generator", "Azure Functions + Azure Sql + a bunch of C# :)"),
                        new XElement("docs", "http://validator.w3.org/feed/docs/rss2.html"),
                        new XElement("ttl", "15"),
                        new XElement("image", 
                            new XElement("url", "https://static.anilibria.tv/img/footer.png"),
                            new XElement("title", "Anilibria — Спасибо, что выбираете нас!"),
                            new XElement("link", "https://www.anilibria.tv")));

            foreach (var episode in content)
            {
                ch.Add(new XElement("item",
                        new XElement("title", Processors["{maintitle}"](episode)),
                        new XElement("link", Processors["{releaselink}"](episode)),
                        new XElement("guid", new XAttribute("isPermaLink", "false"), episode.Uid.ToString()),
                        new XElement("pubDate", episode.Created.ToDateTime().ToString("R")),
                        new XElement("source", new XAttribute("url", "https://getlibriarss.azurewebsites.net/api/RssOnline"), "GetLibriaRss - online"),
                        new XElement("description", BuildDescription(episode))));
            }

            return new OkObjectResult(new XDocument(new XElement("rss", new XAttribute("version", "2.0"), ch)).ToString());
        }

        private static readonly Dictionary<string, Func<RssEntry, string>> Processors = new Dictionary<string, Func<RssEntry, string>>
        {
            {"{maintitle}",     e => $"{e.Release.names[0]} : {e.Title}" },
            {"{releasetitle}",  e => string.Join(" / ", e.Release.names) },
            {"{episodetitle}",  e => e.Title },
            {"{season}",        e => $"{e.Release.season} {e.Release.year}" },
            {"{state}",         e => e.Release.StatusCode == 1 ? "в работе" : "завершён" },
            {"{genres}",        e => string.Join(", ", e.Release.genres) },
            {"{voicers}",       e => string.Join(", ", e.Release.voices) },
            {"{description}",   e => e.Release.description},
            {"{releaselink}",   e => $"https://www.anilibria.tv/release/{e.Release.code}.html" },
            {"{poster}",        e => e.Release.poster },
            {"{torrentlinks}",  e => string.Concat(e.Release.torrents.Select(t=>$@"<li><a href=""https://static.anilibria.tv{t.url}"">Серии {t.series} [{t.quality}]</a></li>")) }
        };

        private static string BuildDescription(RssEntry episode)
        {
            var item = Resources.ItemTemplate;
            foreach (var processor in Processors)
            {
                item = item.Replace(processor.Key, processor.Value(episode));
            }
            return item;
        }
    }
}
