using System.IO;
using System.Threading.Tasks;

using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;

namespace Netptune.Core.Services;

public interface IStorageService
{
    Task<ClientResponse<UploadResponse>> UploadFileAsync(Stream stream, string name, string key = null);
}
