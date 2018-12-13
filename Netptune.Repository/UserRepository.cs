using System.Linq;
using Netptune.Models.Entites;
using Netptune.Models.Models;
using Netptune.Models.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using Netptune.Models.Models.Relationships;

namespace Netptune.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;

        public UserRepository(DataContext dataContext)
        {
            _context = dataContext;
        }

        public AppUser GetUser(string userId)
        {
            return _context.Users.SingleOrDefault(x => x.Id == userId) as AppUser;
        }

        public IEnumerable<AppUser> GetWorkspaceUsers(int workspaceId)
        {

            var users = (from workspaceAppUsers in _context.WorkspaceAppUsers
                         where workspaceAppUsers.WorkspaceId == workspaceId
                         select workspaceAppUsers.User);
            return users;
        }

        public async Task<RepoResult<AppUser>> UpdateUserAsync(AppUser user)
        {
            var updatedUser = _context.AppUsers.SingleOrDefault(x => x.Id == user.Id);

            if (updatedUser == null)
            {
                return RepoResult<AppUser>.NotFound();
            }

            var userId = _userManager.GetUserId(HttpContext.User);

            if (userId != updatedUser.Id)
            {
                return RepoResult<AppUser>.Unauthorized();
            }

            updatedUser.PhoneNumber = user.PhoneNumber;

            updatedUser.FirstName = user.FirstName;
            updatedUser.LastName = user.LastName;

            if (updatedUser.Email != user.Email)
            {
                if (_context.AppUsers.Any(x => x.Email == user.Email))
                {
                    return RepoResult<AppUser>.BadRequest("Email address is already registered.");
                }

                updatedUser.Email = user.Email;
                updatedUser.UserName = user.UserName;
            }

            await _context.SaveChangesAsync();

            return RepoResult<AppUser>.Ok(updatedUser);
        }

        public async Task<RepoResult<AppUser>> InviteUserToWorkspace(string userId, int workspaceId)
        {

            var user = _context.AppUsers.SingleOrDefault(x => x.Id == userId);
            var workspace = _context.Workspaces.SingleOrDefault(x => x.Id == workspaceId);

            if (user == null)
                return RepoResult<AppUser>.NotFound("user not found");

            if (workspace == null)
                return RepoResult<AppUser>.NotFound("workspace not found");

            var alreadyExists = _context.WorkspaceAppUsers.Any(x => x.UserId == user.Id && x.WorkspaceId == workspace.Id);

            if (alreadyExists)
                return RepoResult<AppUser>.BadRequest("User is already a member of the workspace");

            var invite = new WorkspaceAppUser()
            {
                WorkspaceId = workspace.Id,
                UserId = user.Id
            };

            _context.WorkspaceAppUsers.Add(invite);

            await _context.SaveChangesAsync();

            return RepoResult<AppUser>.Ok(user);

        }
    }
}