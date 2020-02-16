using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Linq;

namespace LibriaDbSync
{
    public static class StreamProxy
    {
        [FunctionName("StreamProxy")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("StreamProxy: enter.");
            string releaseId = req.Query["release"];
            string episodeId = req.Query["episode"];
            string hd = req.Query["hd"];

            if (string.IsNullOrWhiteSpace(releaseId))
            {
                log.LogInformation($"StreamProxy: bad releaseId parameter: '{releaseId}'.");
                return new BadRequestObjectResult("Parameter releaseId is not specified.");
            }
            if (string.IsNullOrWhiteSpace(episodeId))
            {
                log.LogInformation($"StreamProxy: bad episodeId parameter: '{episodeId}'.");
                return new BadRequestObjectResult("Parameter episodeId is not specified.");
            }
            if (string.IsNullOrWhiteSpace(hd))
            {
                log.LogInformation($"StreamProxy: bad hd parameter: '{hd}'.");
                return new BadRequestObjectResult("Parameter hd is not specified.");
            }
            bool isHd;
            if (!bool.TryParse(hd, out isHd))
            {
                log.LogInformation($"StreamProxy: can't parse hd: '{hd}'.");
                return new BadRequestObjectResult("Parameter hd should be boolean.");
            }
            int epId;
            if (!int.TryParse(episodeId, out epId))
            {
                log.LogInformation($"StreamProxy: can't parse episodeId: '{episodeId}'.");
                return new BadRequestObjectResult("Parameter episodeId should be integer.");
            }

            log.LogInformation("StreamProxy: get release from server.");
            var col = new NameValueCollection
            {
                { "query", "release" },
                { "id", releaseId }
            };
            var release = Shared.LibApi(col);

            log.LogInformation($"StreamProxy: response received, length {release.Length}.");
            var jRelease = JsonConvert.DeserializeObject<LibriaReleaseModel>(release);
            if (!jRelease.status)
            {
                log.LogInformation($"StreamProxy: request failed: '{jRelease.error}' for release '{releaseId}'.");
                return new NotFoundObjectResult("Unable to retrieve data for the requested release from Anilibria.");
            }

            log.LogInformation($"StreamProxy: looking for the required episode.");
            var episode = jRelease.data.playlist.SingleOrDefault(e => e.id == epId);

            if (episode == null)
            {
                log.LogInformation($"StreamProxy: episode not found: '{episodeId}'.");
                return new NotFoundObjectResult("Unable to locate the requested episode.");
            }
            //http://localhost:7071/api/StreamProxy?release=8500&episode=18&hd=true
            
            log.LogInformation($"StreamProxy: episode found, redirecting client. Function complete.");
            return new RedirectResult(isHd ? episode.srcHd : episode.srcSd);
        }
    }
}
