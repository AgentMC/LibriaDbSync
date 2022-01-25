using LibriaDbSync.LibApi.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LibriaDbSync
{
    abstract class RssConstructor
    {
        protected abstract string ClassId { get; }

        protected abstract string SqlCommand { get; }

        protected abstract RssEntry InitializeRssEntryTopFields(SqlDataReader sqlReader);

        protected virtual void PostProcess(RssEntry entry) { }

        protected const string SqlMandatoryFields = "Releases.Id as Rid, Titles, Code, Description, Poster, StatusCode, Genres, Voicers, Year, Season, Torrents, Baka";

        public async Task<IActionResult> Run(ILogger log)
        {
            var content = new List<RssEntry>();
            using (var conn = await Shared.OpenConnection(log))
            {
                using (var loader = conn.CreateCommand())
                {
                    loader.CommandText = SqlCommand;
                    using (var rdr = await loader.ExecuteReaderAsync())
                    {
                        while (rdr.Read())
                        {
                            var entry = InitializeRssEntryTopFields(rdr);
                            entry.Release = ReadRelease(rdr);
                            content.Add(entry);
                        }
                    }
                }
            }

            content.ForEach(PostProcess);

            return RssFactory.BuildFeed(content, ClassId);
        }

        private Release ReadRelease(SqlDataReader rdr)
        {
            return new Release
            {
                id = (int)rdr["Rid"],
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
                blockedInfo = new BlockedInfo { bakanim = rdr["Baka"] as bool? ?? false }
            };
        }
    }

    internal class OnlineEpisodesRssConstructor : RssConstructor
    {
        protected override string ClassId => "эпизоды";

        protected override string SqlCommand => $@"SELECT TOP {Shared.Threshold} Episodes.Id as Uid, Title, Created, {SqlMandatoryFields}
                                                   FROM Episodes JOIN Releases ON Releases.Id = ReleaseId
                                                   ORDER BY Created DESC";

        protected override RssEntry InitializeRssEntryTopFields(SqlDataReader sqlReader)
        {
            return new RssEntry
            {
                Uid = (int)sqlReader["Uid"],
                Title = (string)sqlReader["Title"],
                Created = (long)sqlReader["Created"]
            };
        }
    }

    internal class UploadedTorrentsRssConstructor : RssConstructor
    {
        protected override string ClassId => "торренты";

        protected override string SqlCommand => $@"SELECT TOP {Shared.Threshold * 2} Torrents.Id as Uid, Created, {SqlMandatoryFields}
                                                   FROM Torrents JOIN Releases ON Releases.Id = ReleaseId
                                                   ORDER BY Created DESC";

        protected override RssEntry InitializeRssEntryTopFields(SqlDataReader sqlReader)
        {
            return new RssEntry
            {
                Uid = (int)sqlReader["Uid"],
                Created = (long)sqlReader["Created"]
            };
        }

        protected override void PostProcess(RssEntry entry)
        {
            entry.Title = FactoryShared.BuildTorrentTitle(entry.Release.torrents.First(t => t.id == entry.Uid));
        }
    }
}
