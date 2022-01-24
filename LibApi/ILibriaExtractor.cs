using LibriaDbSync.LibApi.V1;
using System.Threading.Tasks;

namespace LibriaDbSync.LibApi
{
    interface ILibriaExtractor
    {
        public Task<(LibriaModel, string)> Extract(int quantity);
    }
}
