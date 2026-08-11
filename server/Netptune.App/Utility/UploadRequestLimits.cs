using Microsoft.AspNetCore.Http.Features;

using Netptune.Core.Storage;

namespace Netptune.App.Utility;

public static class UploadRequestLimits
{
    // Endpoint metadata is static, so it can only carry the largest limit any workspace is allowed
    // to configure. Narrowing the request here, before the body is read, keeps a workspace on a
    // small limit from having a much larger body buffered on its behalf. The default multipart
    // section limit of 128 MB is replaced for the same reason the archive import replaces it.
    public static void ApplyMaxFileSize(this HttpRequest request, long maxFileSize)
    {
        var maxRequestSize = maxFileSize + UploadLimits.RequestOverheadBytes;
        var bodySizeFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (bodySizeFeature is { IsReadOnly: false })
        {
            bodySizeFeature.MaxRequestBodySize = maxRequestSize;
        }

        request.HttpContext.Features.Set<IFormFeature>(new FormFeature(request, new FormOptions
        {
            MultipartBodyLengthLimit = maxRequestSize,
        }));
    }
}
