using Microsoft.AspNetCore.Identity;

using Netptune.Core.Entities;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class UserSeeder : ISeeder
{
    public int Phase => 1;

    // joel@netptune.co.uk / password — local testing account, workspace owner
    private static readonly AppUser LoginUser = CreateLoginUser();

    private static readonly (string Id, string Firstname, string Lastname, string Email)[] Data =
    [
        ("a1b2c3d4-e5f6-7890-abcd-000000000001", "Alice",  "Thompson",    "alice.thompson@acme.corp"),
        ("a1b2c3d4-e5f6-7890-abcd-000000000002", "Ben",    "Rodriguez",   "ben.rodriguez@acme.corp"),
        ("a1b2c3d4-e5f6-7890-abcd-000000000003", "Clara",  "Nakamura",    "clara.nakamura@acme.corp"),
        ("a1b2c3d4-e5f6-7890-abcd-000000000004", "Daniel", "Okafor",      "d.okafor@acme.corp"),
        ("a1b2c3d4-e5f6-7890-abcd-000000000005", "Emma",   "Westergaard", "emma.westergaard@acme.corp"),
        ("a1b2c3d4-e5f6-7890-abcd-000000000006", "Finn",   "Larsson",     "finn.larsson@acme.corp"),
    ];

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        // Login user is prepended so index 0 → owner of the first workspace
        context.Users.Add(LoginUser);

        context.Users.AddRange(Data.Select((u, i) => new AppUser
        {
            Id = u.Id,
            Firstname = u.Firstname,
            Lastname = u.Lastname,
            Email = u.Email,
            UserName = u.Email,
            NormalizedEmail = u.Email.ToUpperInvariant(),
            NormalizedUserName = u.Email.ToUpperInvariant(),
            PictureUrl = $"https://i.pravatar.cc/150?u={i + 1}",
            SecurityStamp = Guid.NewGuid().ToString(),
        }));

        await dbContext.Users.AddRangeAsync(context.Users, ct);
    }

    private static AppUser CreateLoginUser()
    {
        var user = new AppUser
        {
            Id = "00000000-0000-0000-0000-000000000001",
            Firstname = "Joel",
            Lastname = "Netptune",
            Email = "joel@netptune.co.uk",
            UserName = "joel@netptune.co.uk",
            NormalizedEmail = "JOEL@NETPTUNE.CO.UK",
            NormalizedUserName = "JOEL@NETPTUNE.CO.UK",
            PictureUrl = "https://i.pravatar.cc/150?u=0",
            SecurityStamp = "00000000-0000-0000-0000-000000000001",
        };

        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, "Netptune_p4ssword-5432");

        return user;
    }
}
