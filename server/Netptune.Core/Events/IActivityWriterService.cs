using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Events;

public interface IActivityWriterService
{
    Task WriteActivity(IEnumerable<IActivityEvent> events);
}
