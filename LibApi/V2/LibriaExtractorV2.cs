using LibriaDbSync.LibApi.V1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LibriaDbSync.LibApi.V2
{
    class LibriaExtractorV2 : ILibriaExtractor
    {
        public async Task<(LibriaModel, string)> Extract(int quantity)
        {
            const string apiDomain = "api.anilibria.tv", patch = "v2.13.1";

            var result = new LibriaModel();
            var endpoint = $"https://{apiDomain}/{patch}/getChanges?limit={quantity}";

            var client = new HttpClient();
            var response = await client.GetAsync(endpoint);
            var jText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var releaseList = JsonConvert.DeserializeObject<Changes>(jText);
                MapV1Model(result, releaseList);
            }
            else
            {
                var error = JsonConvert.DeserializeObject<ErrorModel>(jText);
                result.status = false;
                result.error = error;
            }

            return (result, jText);
        }

        private void MapV1Model(LibriaModel model, Changes v2ChangesModel)
        {
            model.status = true;
            model.data = new Data
            {
                pagination = new Pagination
                {
                    allItems = v2ChangesModel.Count,
                    allPages = 1,
                    page = 1,
                    perPage = v2ChangesModel.Count
                },
                items = v2ChangesModel.Select(c => new V1.Release
                {
                    announce = c.announce,
                    blockedInfo = c.blocked,
                    code = c.code,
                    day = c.season.week_day.ToString(),
                    description = c.description,
                    favorite = new Favorite { rating = c.in_favorites.GetValueOrDefault() },
                    genres = c.genres,
                    id = c.id,
                    //last =
                    LastModified = c.updated ?? c.last_change ?? Shared.ToUnixTimeStamp(DateTime.Now),
                    moon = c.player.alternative_player,
                    names = new List<string>
                    {
                        c.names.GetValueOrDefault("ru"),
                        c.names.GetValueOrDefault("en")
                    },
                    playlist = PlayerDataToPLaylist(c.player),
                    poster = c.posters.medium.url,
                    season = c.season.@string,
                    series = c.player.series.@string,
                    status = c.status.@string,
                    //statusCode= 
                    StatusCode = c.status.code.GetValueOrDefault(),
                    torrents = c.torrents.list.Select(t => new V1.Torrent
                    {
                        completed = t.downloads.GetValueOrDefault(),
                        ctime = t.uploaded_timestamp,
                        hash = t.hash,
                        id = t.torrent_id,
                        leechers = t.leechers.GetValueOrDefault(),
                        quality = t.quality.@string,
                        seeders = t.seeders.GetValueOrDefault(),
                        series = t.series.@string,
                        size = t.total_size,
                        url = t.url
                    }).ToList(),
                    type = c.type.full_string,
                    voices = c.team.voice,
                    //year = 
                    Year = c.season.year.GetValueOrDefault()
                }).ToList()
            };
        }

        private List<Episode> PlayerDataToPLaylist(Player player)
        {
            string tryGetUrl(EpisodeInfo e, string key) => e.hls.TryGetValue(key, out var url) ? $"https://{player.host}{url}" : null;
            return player.playlist.Select(e => new Episode
            {
                id = e.Value.serie,
                title = int.TryParse(e.Key, out _) ? $"Серия {e.Key}" : e.Key,
                fullhd = tryGetUrl(e.Value, "fhd"),
                hd = tryGetUrl(e.Value, "hd"),
                sd = tryGetUrl(e.Value, "sd")
            }).ToList();
        }
    }
}
