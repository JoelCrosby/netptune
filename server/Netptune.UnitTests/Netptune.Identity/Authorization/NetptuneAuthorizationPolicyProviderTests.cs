using FluentAssertions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;

using Netptune.Core.Authorization;
using Netptune.Identity.Authorization;
using Netptune.Identity.Authorization.Requirements;

using Xunit;

namespace Netptune.UnitTests.Netptune.Identity.Authorization;

public class NetptuneAuthorizationPolicyProviderTests
{
    [Fact]
    public async Task GetPolicyAsync_ShouldReturnFallbackPolicy_WhenPolicyIsConfigured()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("ConfiguredPolicy", builder => builder.RequireClaim("configured"));
        var provider = new NetptuneAuthorizationPolicyProvider(Options.Create(options));

        var policy = await provider.GetPolicyAsync("ConfiguredPolicy");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<ClaimsAuthorizationRequirement>()
            .Should()
            .ContainSingle(requirement => requirement.ClaimType == "configured");
        policy.Requirements.OfType<WorkspacePermissionRequirement>().Should().BeEmpty();
    }

    [Fact]
    public async Task GetPolicyAsync_ShouldCreateWorkspacePermissionPolicy_WhenPolicyIsNotConfigured()
    {
        const string policyName = NetptunePermissions.Tasks.Delete;
        var provider = new NetptuneAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync(policyName);

        policy.Should().NotBeNull();
        policy!.AuthenticationSchemes.Should().ContainSingle(JwtBearerDefaults.AuthenticationScheme);
        policy.Requirements.OfType<WorkspaceRequirement>().Should().ContainSingle();
        policy.Requirements.OfType<WorkspacePermissionRequirement>()
            .Should()
            .ContainSingle(requirement => requirement.Permission == policyName);
    }
}
