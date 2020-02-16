namespace LibriaDbSync
{
    public struct PackedId
    {
        public readonly int Version;
        public readonly int ReleaseId;
        public readonly int EpisodeId;

        public PackedId(int releaseId, int episodeId) : this(1, releaseId, episodeId) { }

        private PackedId(int version, int releaseId, int episodeId)
        {
            Version = version;
            ReleaseId = releaseId;
            EpisodeId = episodeId;
        }

        public int Pack()
        {
            switch (Version)
            {
                case 1:
                    // 1  12345678901  12345678901234567890
                    //[V][EEEEEEEEEEE][RRRRRRRRRRRRRRRRRRRR]
                    return (Version << 31) | ((EpisodeId & 0b111_1111_1111) << 20) | (ReleaseId & 0xF_FFFF);
                default:
                    //[EEEEEEEEEEEEEEEE][RRRRRRRRRRRRRRRR]
                    return (EpisodeId << 16) + ReleaseId;
            }
        }

        public static PackedId Unpack(int packed)
        {
            switch (((uint)packed) >> 31)
            {
                case 1: //version 1: 1 bit version flag, 11 bit episodeid (0-2047), 20 bit releaseid (0-1,048,575‬)
                    return new PackedId(
                        version: 1,
                        episodeId: (packed >> 20) & 0b111_1111_1111,
                        releaseId: packed & 0xF_FFFF);
                default: //version 0: 16 bit episodeid, 16 bit releaseid
                    return new PackedId(
                        version: 0,
                        episodeId: packed >> 16,
                        releaseId: packed & 0xFFFF);
            }
        }
    }
}
