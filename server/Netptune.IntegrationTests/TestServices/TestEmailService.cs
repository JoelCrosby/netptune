using System.Collections.Concurrent;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;

namespace Netptune.IntegrationTests.TestServices;

public sealed class TestEmailService : IEmailService
{
    public const string ConfirmEmailPath = "/auth/confirm";

    public const string ResetPasswordPath = "/auth/reset-password";

    public const string RegisterPath = "/auth/register";

    public const string AcceptInvitePath = "/auth/accept-invite";

    private readonly ConcurrentQueue<SendEmailModel> Sent = new();

    public Task Send(SendEmailModel model)
    {
        Sent.Enqueue(model);

        return Task.CompletedTask;
    }

    public Task EnqueueSend(SendEmailModel model) => Send(model);

    public List<SendEmailModel> SentTo(string address)
    {
        return Sent.Where(email => email.SendTo.Address == address).ToList();
    }

    public List<SendEmailModel> SentTo(string address, string linkPath)
    {
        return SentTo(address).Where(email => LinkPathOf(email) == linkPath).ToList();
    }

    public static Dictionary<string, StringValues> LinkQuery(SendEmailModel email)
    {
        var link = new Uri(email.Link!);

        return QueryHelpers.ParseQuery(link.Query);
    }

    private static string? LinkPathOf(SendEmailModel email)
    {
        if (email.Link is null)
        {
            return null;
        }

        return new Uri(email.Link).AbsolutePath;
    }
}
