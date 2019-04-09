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
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectRepository _projectRepository;
        private readonly UserManager<AppUser> _userManager;

        public ProjectsController(IProjectRepository projectRepository, UserManager<AppUser> userManager)
        {
            _projectRepository = projectRepository;
            _userManager = userManager;
        }

        // GET: api/Projects/5
        [HttpGet]
        public async Task<IActionResult> GetProjects(int workspaceId)
        {
            var result = await _projectRepository.GetProjects(workspaceId);
            return result.ToRestResult();
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject([FromRoute] int id)
        {
            var result = await _projectRepository.GetProject(id);
            return result.ToRestResult();
        }

        // PUT: api/Projects/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject([FromRoute] int id, [FromBody] Project project)
        {
            var user = await _userManager.GetUserAsync(User) as AppUser;
            var result = await _projectRepository.UpdateProject(project, user);
            return result.ToRestResult();
        }

        // POST: api/Projects
        [HttpPost]
        public async Task<IActionResult> PostProject([FromBody] Project project)
        {
            var user = await _userManager.GetUserAsync(User) as AppUser;
            var result = await _projectRepository.AddProject(project, user);
            return result.ToRestResult();
        }

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject([FromRoute] int id)
        {
            var result = await _projectRepository.DeleteProject(id);
            return result.ToRestResult();
        }

    }
}