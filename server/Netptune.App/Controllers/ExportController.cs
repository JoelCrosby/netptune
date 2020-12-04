using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Export;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    public class ExportController : ControllerBase
    {
        private readonly ITaskExportService TaskExportService;

        public ExportController(ITaskExportService taskExportService)
        {
            TaskExportService = taskExportService;
        }

        // GET: api/export/tasks/export-workspace
        [HttpGet]
        [Route("tasks/export-workspace")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> ExportWorkspaceTasks()
        {
            var result = await TaskExportService.ExportWorkspaceTasks();

            return File(result.Stream, result.ContentType, result.Filename);
        }
    }
}
