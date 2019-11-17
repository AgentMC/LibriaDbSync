using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LibriaDbSync
{
    public static class RssOnline
    {
        public const string ClassId = "эпизоды";

        [FunctionName("RssOnline")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log) => await new OnlineEpisodesRssConstructor().Run(log);
    }
}
