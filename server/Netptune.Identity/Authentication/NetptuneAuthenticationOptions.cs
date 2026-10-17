using System.ComponentModel.DataAnnotations;

namespace Netptune.Identity.Authentication;

public sealed class NetptuneAuthenticationOptions : IValidatableObject
{
    public const string SectionName = "Authentication";

    [Required]
    public string SigningKey { get; set; } = null!;

    public ExternalLoginOptions GitHub { get; set; } = new();

    public ExternalLoginOptions Google { get; set; } = new();

    public ExternalLoginOptions Microsoft { get; set; } = new();

    // DataAnnotations doesn't descend into nested objects, so the provider sections are checked here.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var providers = new[]
        {
            (Name: nameof(GitHub), Options: GitHub),
            (Name: nameof(Google), Options: Google),
            (Name: nameof(Microsoft), Options: Microsoft),
        };

        foreach (var (name, options) in providers)
        {
            foreach (var missing in options.MissingKeys())
            {
                yield return new ValidationResult($"{SectionName}:{name}:{missing} is required.");
            }
        }
    }
}

public sealed class ExternalLoginOptions
{
    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string CallbackPath { get; set; } = null!;

    public IEnumerable<string> MissingKeys()
    {
        var values = new[]
        {
            (Key: nameof(ClientId), Value: ClientId),
            (Key: nameof(ClientSecret), Value: ClientSecret),
            (Key: nameof(CallbackPath), Value: CallbackPath),
        };

        return values
            .Where(value => string.IsNullOrWhiteSpace(value.Value))
            .Select(value => value.Key);
    }
}
