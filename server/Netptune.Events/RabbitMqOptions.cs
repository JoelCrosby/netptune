namespace Netptune.Events;

public record RabbitMqOptions
{
    public string? ConnectionString { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }
}
