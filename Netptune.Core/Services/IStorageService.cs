using System.IO;
using System.Threading.Tasks;

using Netptune.Core.Responses.Common;

namespace Netptune.Core.Services
{
    public interface IStorageService
    {
        Task<ClientResponse> UploadFileAsync(Stream stream, string key = null);
    }
}
