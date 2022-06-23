using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace LibriaDbSync.LibApi.V1
{
    class LibriaExtractorV1 : ILibriaExtractor
    {
        public async Task<(LibriaModel, string)> Extract(int quantity)
        {
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
                { "perPage", quantity.ToString() }
            };
#pragma warning disable SYSLIB0014 // Type or member is obsolete: it's a legacy backup extractor
            var wc = new System.Net.WebClient();
#pragma warning restore SYSLIB0014 // Type or member is obsolete
            var bytes = await wc.UploadValuesTaskAsync(endpoint, col);
            var jText = Encoding.UTF8.GetString(bytes);

            //finale
            return (JsonConvert.DeserializeObject<LibriaModel>(jText), jText);
        }
    }
}
