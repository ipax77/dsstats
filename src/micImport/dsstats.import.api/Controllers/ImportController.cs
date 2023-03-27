using dsstats.import.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.import.api.Controllers
{
    [ApiController]
    [Route("/api/v1/[controller]")]
    [ServiceFilter(typeof(AuthenticationFilterAttribute))]
    public class ImportController
    {
        private readonly ImportService importService;

        public ImportController(ImportService importService)
        {
            this.importService = importService;
        }

        [HttpGet]
        public async Task<ActionResult> StartImportJob()
        {
            //ImportRequest request = new()
            //{
            //    Replayblobs = new() { "/data/ds/replayblobs/42cd7b31-2e0b-4931-becf-bdbbef500848/20230103-190356.base64" },

            //};

            ImportRequest request = new()
            {
                Replayblobs = new() { "/data/ds/replayblobs/d0579fb3-5c5e-4cb0-86e6-4e23f0f66d05/20230103-181742.base64" }
            };

            await importService.Import(request);
            return new OkResult();
        }
    }
}
