using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Services.Import;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.App.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly ITaskImportService TaskImportService;

        public ImportController(ITaskImportService taskImportService)
        {
            TaskImportService = taskImportService;
        }

        // POST: api/tasks/export-workspace
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

            var stream = files.First().OpenReadStream();

            var result = await TaskImportService.ImportWorkspaceTasks(boardId, stream);

            return Ok(result);
        }
    }
}
