using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Import;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    public class ImportController : ControllerBase
    {
        private readonly ITaskImportService TaskImportService;

        public ImportController(ITaskImportService taskImportService)
        {
            TaskImportService = taskImportService;
        }

        // POST: api/import/tasks/export-workspace
        [HttpPost]
        [Route("tasks/{boardId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> ImportWorkspaceTasks([FromRoute] string boardId, List<IFormFile> files)
        {
            if (files is null || files.Count != 1)
            {
                return BadRequest("Import File must be provided. Only one file can be uploaded at a time.");
            }

            var stream = files[0].OpenReadStream();

            var result = await TaskImportService.ImportWorkspaceTasks(boardId, stream);

            return Ok(result);
        }
    }
}
