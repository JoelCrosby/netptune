using System.Threading.Tasks;

using Netptune.Core.Models.Activity;

namespace Netptune.Core.Events;

public interface IAncestorService
{
    Task<ActivityAncestors> GetTaskAncestors(int entityId);
}