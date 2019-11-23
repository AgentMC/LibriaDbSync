using LibriaDbSync.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LibriaDbSync
{
    static class RssFactory
    {
        static readonly Guid BaseGuid = Guid.Parse("{76A03AEC-B4AE-4D1D-B5C4-48A08058EF1F}");

        public static IActionResult BuildFeed(List<RssEntry> entries, string titleSuffix)
        {
            var ch = new XElement("channel",
                        new XElement("title", $"Anilibria — так звучит аниме! [{titleSuffix}]"),
                        new XElement("link", "https://www.anilibria.tv"),
                        new XElement("description", "Неофициальная RSS-лента по сайту Anilibria.tv"),
                        new XElement("language", "ru-ru"),
                        new XElement("copyright", "Все права на контент в этом канале принадлежат сайту Anilibria.tv. Все права на код синхронизатора базы и генератора ленты принадлежат AgentMC."),
                        new XElement("webMaster", "agentmc@mail.ru (AgentMC)"),
                        new XElement("lastBuildDate", entries[0].Created.ToRssDateTimeString()),
                        new XElement("generator", "Azure Functions + Azure Sql + a bunch of C# :)"),
                        new XElement("docs", "http://validator.w3.org/feed/docs/rss2.html"),
                        new XElement("ttl", "15"),
                        new XElement("image",
                            new XElement("url", "https://static.anilibria.tv/img/footer.png"),
                            new XElement("title", "Anilibria — Спасибо, что выбираете нас!"),
                            new XElement("link", "https://www.anilibria.tv")));

            foreach (var episode in entries)
            {
                ch.Add(new XElement("item",
                            new XElement("title", Processors["{maintitle}"](episode)),
                            new XElement("link", Processors["{releaselink}"](episode)),
                            new XElement("guid", new XAttribute("isPermaLink", "false"), GetGlobalizedUid(episode, titleSuffix).ToString()),
                            new XElement("pubDate", episode.Created.ToRssDateTimeString()),
                            new XElement("source", new XAttribute("url", "https://getlibriarss.azurewebsites.net/api/RssOnline"), $"GetLibriaRss - {titleSuffix}"),
                            new XElement("description", BuildDescription(episode))));
            }

            return new OkObjectResult(new XDocument(new XElement("rss", new XAttribute("version", "2.0"), ch)).ToString())
            {
                Formatters = new FormatterCollection<IOutputFormatter>
                {
                    new RssMediaFormatter()
                }
            };

        }

        private static Guid GetGlobalizedUid(RssEntry episode, string suffix)
        {
            var tmp = BaseGuid.ToByteArray();
            var hash = SimpleStaticStringHashCode(suffix);
            tmp[08] = (byte)((hash & 0xFF000000) >> 24);
            tmp[09] = (byte)((hash & 0x00FF0000) >> 16);
            tmp[10] = (byte)((hash & 0x0000FF00) >> 8);
            tmp[11] = (byte)( hash & 0x000000FF);
            tmp[12] = (byte)((episode.Uid & 0xFF000000) >> 24);
            tmp[13] = (byte)((episode.Uid & 0x00FF0000) >> 16);
            tmp[14] = (byte)((episode.Uid & 0x0000FF00) >> 8);
            tmp[15] = (byte)( episode.Uid & 0x000000FF);
            return new Guid(tmp);
        }

        //dotnet core randomizes the base for GetHashCode, need custom impl.
        private static uint SimpleStaticStringHashCode(string source)
        {
            if (string.IsNullOrEmpty(source)) return 0;
            unchecked
            {
                var binmask = 0b10101010_01010101_10101010_01010101; //unnatural base
                binmask ^= (byte)source.Length * (uint)0x01010101;   //clobe 1 byte to 4 of int and xor to base
                for (int i = 0; i < source.Length; i++)
                {
                    uint character = (uint)(source[i] << i); //char code and position
                    binmask ^= character;  //xor lower or higher 16 bit of the result for even or odd chars respectively
                    binmask ^= character << 16; //and one more time, other bits
                }
                return binmask;
            }
        }

        private static readonly Dictionary<string, Func<RssEntry, string>> Processors = new Dictionary<string, Func<RssEntry, string>>
        {
            {"{maintitle}",     e => $"{e.Release.names[0]} · {e.Title}" },
            {"{releasetitle}",  e => string.Join(" / ", e.Release.names) },
            {"{episodetitle}",  e => e.Title },
            {"{season}",        e => $"{e.Release.season} {e.Release.year}" },
            {"{state}",         e => e.Release.StatusCode == 1 ? "в работе" : "завершён" },
            {"{genres}",        e => string.Join(", ", e.Release.genres) },
            //{"{voicers}",       e => string.Join(", ", e.Release.voices.Select(DeHtmlize)) },
            {"{description}",   e => e.Release.description},
            {"{releaselink}",   e => $"https://www.anilibria.tv/release/{e.Release.code}.html" },
            {"{poster}",        e => e.Release.poster },
            {"{torrentlinks}",  e => string.Concat(e.Release.torrents.Select(t=>$@"<li><a href=""https://static.anilibria.tv{t.url}"">{FactoryShared.BuildTorrentTitle(t)}</a></li>")) }
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

        private static string DeHtmlize(string source)
        {
            var lcs = source.ToLower();
            var res = new StringBuilder();
            for (int i = 0; i < lcs.Length; i++)
            {
                if(lcs[i] != '<')
                {
                    res.Append(source[i]);
                }
                else //i -> [<]
                {
                    int j = i + 1;
                    while (j < lcs.Length && lcs[j] != ' ' && lcs[j] != '>') j++; //searching first space, '>' or eos
                    if (j < lcs.Length && lcs[j] == '>') //tag detected, j -> [>]
                    {
                        if(lcs[j-1] == '/') //non-html self-closing tag
                        {
                            i = j; //shift the pointer to the next char after '>' on next iteration, i.e. ignore self-closed tag.
                        } 
                        else
                        {
                            var endTag = "</" + lcs.Substring(i + 1, j - i); //</tag>
                            int k = lcs.IndexOf(endTag, j + 1); 
                            if (k > -1) //tag closed, k -> [<]
                            {
                                i = k + endTag.Length -1; // i -> [>]
                            }
                            else //tag not closed, e.g. HTML <br>
                            {
                                i = j;
                            }
                        }
                        while (i < lcs.Length - 1 && lcs[i + 1] == ' ') i++; //skipping spaces after tags
                    }
                    else //no tag
                    {
                        res.Append(source[i]);
                    }
                }
            }
            return res.ToString();
        }
    }

    public static class FactoryShared
    {
        public static string BuildTorrentTitle(Torrent torrent) => $"{GetTorrentTitlePrefix(torrent.series)}{torrent.series} [{torrent.quality}]";

        private static string GetTorrentTitlePrefix(string episodeSetDescription)
        {
            int digitGroups = 0;
            bool isDigit = false;
            foreach (var c in episodeSetDescription)
            {
                if (char.IsDigit(c) != isDigit)
                {
                    isDigit = !isDigit;
                    if (isDigit)
                    {
                        digitGroups++;
                    }
                }
            }
            switch (digitGroups)
            {
                case 0:
                    return string.Empty;
                case 1:
                    return "Серия ";
                default:
                    return "Серии ";
            }
        }
    }

    class RssMediaFormatter : TextOutputFormatter
    {
        private const string MediaType = "application/rss+xml";

        public RssMediaFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaType));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            context.ContentType = MediaType;
            await context.HttpContext.Response.WriteAsync(context.Object.ToString());
        }
    }
}
