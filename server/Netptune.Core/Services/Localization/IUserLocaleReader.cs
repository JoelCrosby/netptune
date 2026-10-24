namespace Netptune.Core.Services.Localization;

public interface IUserLocaleReader
{
    Task<string> GetLocale(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetLocales(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default);
}
