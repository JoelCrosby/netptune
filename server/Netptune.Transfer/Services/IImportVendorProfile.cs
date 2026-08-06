using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;

namespace Netptune.Transfer.Services;

public interface IImportVendorProfile
{
    ImportVendorProfile Vendor { get; }

    // How strongly this profile recognises the file, in [0, 1]. Zero means "not mine".
    double Fingerprint(ImportSourceProfile profile);

    ImportMappingModel BuildMapping(ImportSourceProfile profile);
}
