using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Netptune.Models.Models;
using Netptune.Models.Repositories;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]
namespace Netptune.Api.Controllers
{

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectTasksController : ControllerBase
    {

        private readonly ITaskRepository _taskRepository;
        private readonly UserManager<AppUser> _userManager;

        public ProjectTasksController(ITaskRepository taskRepository, UserManager<AppUser> userManager)
        {
            _taskRepository = taskRepository;
            _userManager = userManager;
        }

        // GET: api/Tasks
        [HttpGet]
        public async Task<IActionResult> GetTasks(int workspaceId)
        {
            var result = await _taskRepository.GetTasksAsync(workspaceId);
            return result.ToRestResult();
        }

        // GET: api/Tasks/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask([FromRoute] int id)
        {
            var result = await _taskRepository.GetTask(id);
            return result.ToRestResult();
        }

        // PUT: api/Tasks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTask([FromRoute] int id, [FromBody] ProjectTask task)
        {
            var result = await _taskRepository.UpdateTask(task);
            return result.ToRestResult();
        }

        // POST: api/Tasks
        [HttpPost]
        public async Task<IActionResult> PostTask([FromBody] ProjectTask task)
        {
            var user = await _userManager.GetUserAsync(User) as AppUser;
            var result = await _taskRepository.AddTask(task, user);
            return result.ToRestResult();
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask([FromRoute] int id)
        {
            var result = await _taskRepository.DeleteTask(id);
            return result.ToRestResult();
        }

        // GET: api/Tasks/GetProjectTaskCount/5
        [HttpGet]
        [Route("GetProjectTaskCount")]
        public async Task<IActionResult> GetProjectTaskCount(int projectId)
        {
            var result = await _taskRepository.GetProjectTaskCount(projectId);
            return result.ToRestResult();
        }
    }
}