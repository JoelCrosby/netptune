namespace Netptune.TestData;

public record TestUser
{
    public required string Id { get; init; }
    public required string Firstname { get; init; }
    public required string Lastname { get; init; }
    public required string Email { get; init; }
}

public static class SeedData
{
    public static readonly List<TestUser> Users =
    [
        new() { Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890", Firstname = "joel",  Lastname = "crosby", Email = "joelcrosby@live.co.uk" },
        new() { Id = "b2c3d4e5-f6a7-8901-bcde-f12345678901", Firstname = "admin", Lastname = "user",   Email = "admin@netptune.co.uk" },
        new() { Id = "c3d4e5f6-a7b8-9012-cdef-123456789012", Firstname = "john",  Lastname = "smith",  Email = "johnsmith@gmail.com" },
    ];
}
