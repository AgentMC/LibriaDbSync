using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
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

        internal static string LibApi(NameValueCollection parameters)
        {
            //const string endpoint = "https://anilibriasmartservice.azurewebsites.net/public/api/index.php";
            const string endpoint = "https://www.anilibria.tv/public/api/index.php";
            var bytes = new WebClient().UploadValues(endpoint, parameters);
            return Encoding.UTF8.GetString(bytes);
        }
    }
    public class BlockedInfo
    {
        public bool blocked { get; set; }
        public object reason { get; set; }
        public bool bakanim { get; set; }
    }

    public class Episode
    {
        public int id { get; set; }
        public string title { get; set; }
        public string sd { get; set; }
        public string hd { get; set; }
        public string fullhd { get; set; }
        public string srcSd { get; set; }
        public string srcHd { get; set; }
    }

    public class Torrent
    {
        public int id { get; set; }
        public string hash { get; set; }
        public int leechers { get; set; }
        public int seeders { get; set; }
        public int completed { get; set; }
        public string quality { get; set; }
        public string series { get; set; }
        public long size { get; set; }
        public string url { get; set; }
        public long ctime { get; set; }
    }

    public class Favorite
    {
        public int rating { get; set; }
        public bool added { get; set; }
    }

    public class Release
    {
        public int id { get; set; }
        public string code { get; set; }
        public List<string> names { get; set; }
        public string series { get; set; }
        public string poster { get; set; }
        public string last { get => LastModified.ToString(); set => LastModified = long.TryParse(value, out long l) ? l : 0; }
        public long LastModified { get; set; }
        public string moon { get; set; }
        public object announce { get; set; }
        public string status { get; set; }
        public string statusCode { get => StatusCode.ToString(); set => StatusCode = byte.TryParse(value, out byte s) ? s : (byte)1; }
        public byte StatusCode { get; set; }
        public string type { get; set; }
        public List<string> genres { get; set; }
        public List<string> voices { get; set; }
        public string year { get => Year.ToString(); set => Year = short.TryParse(value, out short s) ? s : (short)DateTime.Now.Year; }
        public short Year { get; set; }
        public string season { get; set; }
        public string day { get; set; }
        public string description { get; set; }
        public BlockedInfo blockedInfo { get; set; }
        public List<Episode> playlist { get; set; }
        public List<Torrent> torrents { get; set; }
        public Favorite favorite { get; set; }

        public long GetLastTorrentUpdateEpochSeconds()
        {
            return torrents.Count == 0 ? 0 : torrents.Max(t => t.ctime);
        }
    }

    public class Pagination
    {
        public int page { get; set; }
        public int perPage { get; set; }
        public int allPages { get; set; }
        public int allItems { get; set; }
    }

    public class IndexData
    {
        public List<Release> items { get; set; }
        public Pagination pagination { get; set; }
    }

    public class LibriaIndexModel : LibriaModelBase<IndexData> { }

    public class LibriaReleaseModel : LibriaModelBase<Release> { }

    public class LibriaModelBase<T>
    {
        public bool status { get; set; }
        public T data { get; set; }
        public object error { get; set; }
    }


    class RssEntry
    {
        public int Uid { get; set; }

        public string Title { get; set; }

        public Release Release { get; set; }

        public long Created { get; set; }

        public bool EnableEpisodeSpecificData {get;set;}
    }
}
