using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LibriaDbSync
{
    public static class MainSyncFunction
    {
        [FunctionName("MainSyncFunction")]
        public static void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}.");

            //initial upload
            //const string FileName = @"D:\hackd\Desk\libriabase.json";
            //var jText = File.ReadAllText(FileName, Encoding.Unicode);

            //read from server
            //const string endpoint = "https://anilibriasmartservice.azurewebsites.net/public/api/index.php";
            const string endpoint = "https://www.anilibria.tv/public/api/index.php";
            var col = new System.Collections.Specialized.NameValueCollection
            {
                { "query", "list" },
                { "page", "1" },
                { "perPage", "50" }
            };
            var wc = new System.Net.WebClient();
            var bytes = wc.UploadValues(endpoint, col);
            var jText = Encoding.UTF8.GetString(bytes);

            //finale
            log.LogInformation($"Text content received, length {jText.Length}.");
            var model = JsonConvert.DeserializeObject<LibriaModel>(jText);
            SyncDb(model, log);
            log.LogInformation($"Synchronization complete.");
        }

        private static void SyncDb(LibriaModel model, ILogger log)
        {
            using (var conn = Shared.OpenConnection(log).Result)
            {
                foreach (var release in model.data.items)
                {
                    log.LogInformation($"+Processing release {release.id}, '{(release.names != null && release.names.Count > 0 ? release.names[0] : "Undefined")}', {release.playlist.Count} live episodes.");
                    var maxEpisodeUpdate = Math.Max(release.GetLastTorrentUpdateEpochSeconds(), release.LastModified);
                    var lastUpdated = GetLastUpdated(conn, release.id);
                    var releasesCount = GetReleasesCount(conn, release.id);
                    if (lastUpdated == null)
                    {
                        log.LogInformation("++Proceeding. Reson: new.");
                        CreateRelease(conn, release.id, log);
                        UpdateRelease(conn, release, log, ref maxEpisodeUpdate);
                        foreach (var episode in release.playlist)
                        {
                            CreateEpisode(conn, episode, release.id, maxEpisodeUpdate, log);
                        }
                    }
                    else if (lastUpdated.Value != release.LastModified || releasesCount != release.playlist.Count)
                    {
                        log.LogInformation($"++Proceeding. Reson: {(lastUpdated.Value != release.LastModified ? "release updated" : "episodes count changed")}.");
                        var exisitng = UpdateRelease(conn, release, log, ref maxEpisodeUpdate);
                        foreach (var episode in release.playlist.Where(e => !exisitng.Contains(e.id)))
                        {
                            CreateEpisode(conn, episode, release.id, maxEpisodeUpdate, log);
                        }
                    }
                }
            }
        }

        private static long? GetLastUpdated(SqlConnection conn, int id)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "Select LastModified from Releases Where Id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                return (long?)cmd.ExecuteScalar();
            }
        }

        private static int GetReleasesCount(SqlConnection conn, int id)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "Select count(id) from Episodes Where ReleaseId=@id";
                cmd.Parameters.AddWithValue("@id", id);
                return (int)cmd.ExecuteScalar();
            }
        }

        private static void CreateRelease(SqlConnection conn, int id, ILogger log)
        {
            log.LogInformation("++Creating the release entry...");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "Insert into Releases (Id, LastModified) Values (@id, 0)";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static List<int> UpdateRelease(SqlConnection conn, Release release, ILogger log, ref long lastEpisodeTimestamp)
        {
            log.LogInformation("++Updating the release entry...");
            var res = new List<int>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE Releases 
                                    SET Titles = @titles,
                                        Poster = @poster,
                                        LastModified = @lastmodified,
                                        StatusCode = @statuscode,
                                        Genres = @genres,
                                        Voicers = @voicers,
                                        Year = @year,
                                        Season = @season,
                                        Description = @description,
                                        Torrents = @torrents,
                                        Rating = @rating,
                                        Code = @code
                                    WHERE Id = @id
                                    SELECT Id FROM Episodes WHERE ReleaseId = @id";
                cmd.Parameters.AddWithValue("@id", release.id);
                cmd.Parameters.AddWithValue("@titles", JsonConvert.SerializeObject(release.names));
                cmd.Parameters.AddWithValue("@poster", release.poster);
                cmd.Parameters.AddWithValue("@lastmodified", release.LastModified);
                cmd.Parameters.AddWithValue("@statuscode", release.StatusCode);
                cmd.Parameters.AddWithValue("@genres", JsonConvert.SerializeObject(release.genres));
                cmd.Parameters.AddWithValue("@voicers", JsonConvert.SerializeObject(release.voices));
                cmd.Parameters.AddWithValue("@year", release.Year);
                cmd.Parameters.AddWithValue("@season", release.season);
                cmd.Parameters.AddWithValue("@description", release.description);
                cmd.Parameters.AddWithValue("@torrents", JsonConvert.SerializeObject(release.torrents));
                cmd.Parameters.AddWithValue("@rating", release.favorite.rating);
                cmd.Parameters.AddWithValue("@code", release.code);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        res.Add(rdr.GetInt32(0) >> 16);
                    }
                }
            }

            if ((DateTime.Now - lastEpisodeTimestamp.ToDateTime()).TotalHours > 36)
            {
                lastEpisodeTimestamp = DateTime.Now.ToUnixTimeStamp();
                log.LogInformation($"+++No usable timestamp for new episodes. Using Now ({lastEpisodeTimestamp}).");
            }

            log.LogInformation($"++Update complete. {res.Count} episodes found.");
            return res;
        }

        private static void CreateEpisode(SqlConnection conn, Episode episode, int releaseId, long createdStamp, ILogger log)
        {
            log.LogInformation($"+++Creating an episode {episode.id}.");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "Insert into Episodes (Id, ReleaseId, Title, Links, Created) Values (@id, @release, @title, @links, @created)";
                cmd.Parameters.AddWithValue("@id", releaseId + (episode.id << 16));
                cmd.Parameters.AddWithValue("@release", releaseId);
                cmd.Parameters.AddWithValue("@title", episode.title);
                cmd.Parameters.AddWithValue("@links", JsonConvert.SerializeObject(episode));
                cmd.Parameters.AddWithValue("@created", createdStamp);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
