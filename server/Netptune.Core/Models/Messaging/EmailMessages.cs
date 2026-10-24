using System.Globalization;

using Netptune.Core.Localization;

namespace Netptune.Core.Models.Messaging;

public static class EmailMessages
{
    public static SendEmailModel PasswordReset(SendTo sendTo, string firstName, string link)
    {
        return new SendEmailModel
        {
            SendTo = sendTo,
            Name = firstName,
            Greeting = Localized.Format(AppText.EmailGreeting, firstName),
            Subject = AppText.PasswordResetEmailSubject,
            PreHeader = AppText.PasswordResetEmailPreHeader,
            Message = AppText.PasswordResetEmailMessage,
            Action = AppText.PasswordResetEmailAction,
            Link = link,
            RawTextContent = Localized.Format(AppText.PasswordResetEmailText, link),
            Footer = AppText.PasswordResetEmailFooter,
            Tagline = AppText.EmailTagline,
            Language = CultureInfo.CurrentUICulture.Name,
        };
    }

    public static SendEmailModel Welcome(SendTo sendTo, string firstName, string link)
    {
        return new SendEmailModel
        {
            SendTo = sendTo,
            Name = firstName,
            Greeting = Localized.Format(AppText.EmailGreeting, firstName),
            Subject = AppText.WelcomeEmailSubject,
            PreHeader = AppText.WelcomeEmailPreHeader,
            Message = AppText.WelcomeEmailMessage,
            Action = AppText.WelcomeEmailAction,
            Link = link,
            RawTextContent = Localized.Format(AppText.WelcomeEmailText, link),
            Footer = AppText.WelcomeEmailFooter,
            Tagline = AppText.EmailTagline,
            Language = CultureInfo.CurrentUICulture.Name,
        };
    }

    public static SendEmailModel WorkspaceInvite(string address, string workspaceName, string link)
    {
        return new SendEmailModel
        {
            SendTo = new SendTo { Address = address, DisplayName = address },
            Name = address,
            Greeting = AppText.EmailGreetingWithoutName,
            Subject = AppText.InviteEmailSubject,
            PreHeader = Localized.Format(AppText.InviteEmailPreHeader, workspaceName),
            Message = Localized.Format(AppText.InviteEmailMessage, workspaceName),
            Action = AppText.InviteEmailAction,
            Link = link,
            RawTextContent = Localized.Format(AppText.InviteEmailText, workspaceName, link),
            Footer = AppText.InviteEmailFooter,
            Tagline = AppText.EmailTagline,
            Language = CultureInfo.CurrentUICulture.Name,
        };
    }
}
