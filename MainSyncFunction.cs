using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using LibriaDbSync.LibApi.V1;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LibriaDbSync
{
    public static class MainSyncFunction
    {
        [FunctionName("MainSyncFunction")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Azure API")]
        public static async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}.");

            var extractor = Shared.GetExtractor();
            log.LogInformation($"Extractor invoking: {extractor.GetType()}.");

            var (model, responseText) = await extractor.Extract(50);
            log.LogInformation($"Text content received, length {responseText.Length}.");

            if (model?.data?.items == null)
            {
                log.LogError($"Bad response. The response text is {responseText}.");
                throw new Exception("Unable to sync the DB. Response received contains invalid data.");
            }
            if (model.data.items.Count == 0)
            {
                log.LogWarning($"No releases returned. The response text is {responseText}.");
            }
            else
            {
                await SyncDb(model.data, log);
            }
            log.LogInformation("Synchronization complete.");
        }

        private static async Task SyncDb(Data data, ILogger log)
        {
            using (var conn = await Shared.OpenConnection(log))
            {
                foreach (var release in data.items)
                {
                    log.LogInformation($"+Processing release {release.id}, '{(release.names != null && release.names.Count > 0 ? release.names[0] : "Undefined")}', {release.playlist.Count} live episodes.");
                    var maxEpisodeUpdate = Math.Max(release.GetLastTorrentUpdateEpochSeconds(), release.LastModified);
                    var releaseMeta = GetMetadata(conn, release.id);
                    List<int> torrentIds = null;
                    if (releaseMeta == null)
                    {
                        log.LogInformation("++Proceeding. Reson: new.");
                        CreateRelease(conn, release.id, log);
                        UpdateRelease(conn, release, log, ref maxEpisodeUpdate, torrentIds);
                        foreach (var episode in release.playlist)
                        {
                            CreateEpisode(conn, episode, release.id, maxEpisodeUpdate, log);
                        }
                    }
                    else if (releaseMeta.Value.Item1 != release.LastModified)
                    {
                        ProceedUpdate(conn, release, log, maxEpisodeUpdate, torrentIds, "release updated");
                    }
                    else if (releaseMeta.Value.Item2 != (release.blockedInfo?.bakanim ?? false))
                    {
                        ProceedUpdate(conn, release, log, maxEpisodeUpdate, torrentIds, "baka flag updated");
                    }
                    else if (GetEpisodesCount(conn, release.id) != release.playlist.Count)
                    {
                        ProceedUpdate(conn, release, log, maxEpisodeUpdate, torrentIds, "episodes count changed");
                    }
                    else if (!(torrentIds = GetTorrentIds(conn, release.id)).SequenceEqual(release.torrents.Select(t => t.id).OrderBy(i => i)))
                    {
                        ProceedUpdate(conn, release, log, maxEpisodeUpdate, torrentIds, "torrents set changed");
                    }
                }
            }
        }

        private static void ProceedUpdate(SqlConnection conn, Release release, ILogger log, long maxEpisodeUpdate, List<int> torrentIds, string reason)
        {
            log.LogInformation($@"++Proceeding. Reason: {reason}.");
            var exisitng = UpdateRelease(conn, release, log, ref maxEpisodeUpdate, torrentIds);
            foreach (var episode in release.playlist.Where(pe => !exisitng.Any(ee => ee.EpisodeId == pe.id)))
            {
                CreateEpisode(conn, episode, release.id, maxEpisodeUpdate, log);
            }
            foreach (var episodeId in exisitng.Where(ee => !release.playlist.Any(pe => pe.id == ee.EpisodeId)))
            {
                DeleteEpisode(conn, episodeId, log);
            }
        }

        private static (long, bool)? GetMetadata(SqlConnection conn, int releaseId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "Select LastModified, Baka from Releases Where Id=@id";
                cmd.Parameters.AddWithValue("@id", releaseId);
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        return ((long)rdr["LastModified"], rdr["Baka"] as bool? ?? false);
                    }
                }
            }
            return null;
        }

        private static int GetEpisodesCount(SqlConnection conn, int releaseId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "Select count(id) from Episodes Where ReleaseId=@id";
                cmd.Parameters.AddWithValue("@id", releaseId);
                return (int)cmd.ExecuteScalar();
            }
        }

        private static List<int> GetTorrentIds(SqlConnection conn, int releaseId)
        {
            var res = new List<int>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "Select Id from Torrents Where ReleaseId=@id order by Id";
                cmd.Parameters.AddWithValue("@id", releaseId);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        res.Add(rdr.GetInt32(0));
                    }
                }
            }
            return res;
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

        private static List<PackedId> UpdateRelease(SqlConnection conn, Release release, ILogger log, ref long lastEpisodeTimestamp, List<int> existingTorrentIds)
        {
            log.LogInformation("++Updating the release entry...");
            var res = new List<PackedId>();
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
                                        Code = @code,
                                        Baka = @bakanim
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
                cmd.Parameters.AddWithValue("@bakanim", release.blockedInfo?.bakanim ?? false);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        res.Add(PackedId.Unpack(rdr.GetInt32(0)));
                    }
                }
            }

            lastEpisodeTimestamp = CheckTimeStamp(lastEpisodeTimestamp, 36, "No usable timestamp for new episodes", log);

            SynchronizeTorrentIndex(conn, release, existingTorrentIds ?? GetTorrentIds(conn, release.id), log);

            log.LogInformation($"++Update complete. {res.Count} episodes found.");
            return res;
        }

        private static long CheckTimeStamp(long testTimeStamp, float hoursThreshold, string thresholdMessage, ILogger log)
        {
            if ((DateTime.Now - testTimeStamp.ToDateTime()).TotalHours > hoursThreshold)
            {
                var properTimestamp = DateTime.Now.ToUnixTimeStamp();
                log.LogInformation($"+++{thresholdMessage}. Using Now ({properTimestamp}).");
                return properTimestamp;
            }
            return testTimeStamp;
        }

        private static void SynchronizeTorrentIndex(SqlConnection conn, Release release, List<int> existingIds, ILogger log)
        {
            var outdatedIds = existingIds.Where(id => !release.torrents.Select(t => t.id).Contains(id)).ToList();
            if (outdatedIds.Count > 0)
            {
                var tids = string.Join(",", outdatedIds.Select(i => $"'{i}'"));
                log.LogInformation($"++-Deleting torrent IDs {tids}.");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"Delete from Torrents Where Id in ({tids})";
                    cmd.ExecuteNonQuery();
                }
            }
            foreach (var torrent in release.torrents.Where(t => !existingIds.Contains(t.id)))
            {
                log.LogInformation($"+++Creating torrent {torrent.id}.");
                var createdTimestamp = CheckTimeStamp(torrent.ctime, 36, "Torrent date out of range. Clamping", log);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"Insert into Torrents (Id, ReleaseId, Created) Values (@id, @release, @created)";
                    cmd.Parameters.AddWithValue("@id", torrent.id);
                    cmd.Parameters.AddWithValue("@release", release.id);
                    cmd.Parameters.AddWithValue("@created", createdTimestamp);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void CreateEpisode(SqlConnection conn, Episode episode, int releaseId, long createdStamp, ILogger log)
        {
            log.LogInformation($"+++Creating an episode {episode.id}.");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "Insert into Episodes (Id, ReleaseId, Title, Links, Created) Values (@id, @release, @title, @links, @created)";
                cmd.Parameters.AddWithValue("@id", new PackedId(releaseId: releaseId, episodeId: episode.id).Pack());
                cmd.Parameters.AddWithValue("@release", releaseId);
                cmd.Parameters.AddWithValue("@title", episode.title);
                cmd.Parameters.AddWithValue("@links", JsonConvert.SerializeObject(episode));
                cmd.Parameters.AddWithValue("@created", createdStamp);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteEpisode(SqlConnection conn, PackedId episode, ILogger log)
        {
            var epDbId = episode.Pack();
            log.LogInformation($"++-Deleting an obsolete episode {episode.EpisodeId} of release {episode.ReleaseId} (DB id {epDbId}).");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Episodes WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", epDbId);
                var rows = cmd.ExecuteNonQuery();
                log.LogInformation($"++- ==> {(rows == 1 ? "OK" : "FAILURE")}.");
            }
        }
    }
}
