using Netptune.Core.Entities;

namespace Netptune.TestData.Seeders;

internal static class UserSeeder
{
    internal static List<AppUser> Generate() =>
        SeedData.Users.Select((u, i) => new AppUser
        {
            Id = u.Id,
            Firstname = u.Firstname,
            Lastname = u.Lastname,
            Email = u.Email,
            PictureUrl = $"https://i.pravatar.cc/150?u={i + 1}",
            UserName = u.Email,
            NormalizedEmail = u.Email.ToUpper().Normalize(),
            NormalizedUserName = u.Email.ToUpper().Normalize(),
        }).ToList();
}
