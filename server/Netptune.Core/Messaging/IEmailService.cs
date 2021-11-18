using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models.Messaging;

namespace Netptune.Core.Messaging;

public interface IEmailService
{
    Task Send(SendEmailModel model);

    Task Send(IEnumerable<SendEmailModel> models);

    Task EnqueueSend(SendEmailModel model);

    Task EnqueueSend(IEnumerable<SendEmailModel> models);
}