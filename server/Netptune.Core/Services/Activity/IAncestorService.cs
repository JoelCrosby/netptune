using System.Threading.Tasks;

using Netptune.Core.Models.Activity;

namespace Netptune.Core.Services.Activity;

public interface IAncestorService
{
    Task<ActivityAncestors> GetTaskAncestors(int entityId);
}
