using AutoFixture;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services;

using NSubstitute;

namespace Netptune.UnitTests.Netptune.Services;

public class BoardGroupServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly BoardGroupService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public BoardGroupServiceTests()
    {
        Service = new(UnitOfWork, Identity);
    }
}
