﻿using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LibriaDbSync.LibApi.V2
{
    public class Changes : List<Release> { }
    public class Release
    {
        public int id { get; set; }
        public string code { get; set; }
        public Dictionary<string, string> names { get; set; }
        public string announce { get; set; }
        public Statusable status { get; set; }
        public Posters posters { get; set; }
        public long? updated { get; set; }
        public long? last_change { get; set; }
        public ReleaseType type { get; set; }
        public List<string> genres { get; set; }
        public Team team { get; set; }
        public Season season { get; set; }
        public string description { get; set; }
        public int? in_favorites { get; set; }
        public V1.BlockedInfo blocked { get; set; }
        public Player player { get; set; }
        public TorrentList torrents { get; set; }

    }
    public class Statusable
    {
        public string @string { get; set; }
        public byte? code { get; set; }
    }
    public class Imaginable
    {
        public string url { get; set; }
        public object raw_base64_file { get; set; }
    }
    public class Posters
    {
        public Imaginable small { get; set; }
        public Imaginable medium { get; set; }
        public Imaginable original { get; set; }
    }
    public class ReleaseType : Statusable
    {
        public string full_string { get; set; }
        public string length { get; set; }
        public int? series { get; set; }
    }
    public class Team
    {
        public List<string> voice { get; set; }
        public List<string> translator { get; set; }
        public List<string> editing { get; set; }
        public List<string> decor { get; set; }
        public List<string> timing { get; set; }
    }
    public class Season : Statusable
    {
        public short? year { get; set; }
        public byte? week_day { get; set; }
    }
    public class Player
    {
        public string alternative_player { get; set; }
        public string host { get; set; }
        public JToken series { get; set; }
        public SeriesInfo Series { get { return (series as JObject)?.ToObject<SeriesInfo>() ?? SeriesInfo.Empty; } }
        public Dictionary<string, EpisodeInfo> playlist { get; set; }

    }
    public class SeriesInfo
    {
        public static readonly SeriesInfo Empty = new();
        public float? first { get; set; }
        public float? last { get; set; }
        public string @string { get; set; }

    }
    public class EpisodeInfo
    {
        public float? serie { get; set; }
        public long created_timestamp { get; set; }
        public Dictionary<string, string> hls { get; set; }
    }
    public class Torrent : Imaginable
    {
        public int torrent_id { get; set; }
        public JToken series { get; set; }
        public SeriesInfo Series { get { return (series as JObject)?.ToObject<SeriesInfo>() ?? SeriesInfo.Empty; } }
        public VideoQuality quality { get; set; }
        public int? leechers { get; set; }
        public int? seeders { get; set; }
        public int? downloads { get; set; }
        public long total_size { get; set; }
        public long uploaded_timestamp { get; set; }
        public string hash { get; set; }
        public object metadata { get; set; }
    }
    public class VideoQuality
    {
        public string @string { get; set; }
        public string type { get; set; }
        public string resolution { get; set; }
        public string encoder { get; set; }
        public bool? lq_audio { get; set; }
    }
    public class TorrentList
    {
        public JToken series { get; set; }
        public SeriesInfo Series { get { return (series as JObject)?.ToObject<SeriesInfo>() ?? SeriesInfo.Empty; } }
        public List<Torrent> list { get; set; }
    }


    public class ErrorModel
    {
        public Error error { get; set; }
        public override string ToString()
        {
            return $"[{(error == null ? "in" : string.Empty)}valid error] code '{error?.code}', message '{error?.Message}'.";
        }
    }
    public class Error
    {
        public int? code { get; set; }
        public string Message { get; set; }
    }
}
