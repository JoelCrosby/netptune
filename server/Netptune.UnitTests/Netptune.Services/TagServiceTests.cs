using AutoFixture;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services;

using NSubstitute;

namespace Netptune.UnitTests.Netptune.Services;

public class TagServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly TagService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public TagServiceTests()
    {
        Service = new(UnitOfWork, Identity);
    }
}
