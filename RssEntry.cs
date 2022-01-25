using LibriaDbSync.LibApi.V1;

namespace LibriaDbSync
{
    class RssEntry
    {
        public int Uid { get; set; }

        public string Title { get; set; }

        public Release Release { get; set; }

        public long Created { get; set; }
    }
}
