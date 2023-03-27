using System.Threading.Tasks;

using Netptune.Core.ViewModels.Web;

namespace Netptune.Core.Services;

public interface IWebService
{
    Task<MetaInfoResponse> GetMetaDataFromUrl(string url);
}
