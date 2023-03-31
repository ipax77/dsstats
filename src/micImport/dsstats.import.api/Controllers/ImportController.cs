using dsstats.import.api.Services;
using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;

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

        [HttpPost]
        public async Task<ActionResult> StartImportJob([FromBody] ImportRequest request)
        {
            await importService.Import(request);
            return new OkResult();
        }

        [HttpGet]
        public ActionResult<ImportResult> GetImportResult()
        {
            return importService.GetImportResult();
        }
    }
}
