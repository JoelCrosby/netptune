using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Localization;
using Netptune.Core.Preferences;
using Netptune.Core.Services.Localization;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Localization;

public sealed class UserLocaleReader : IUserLocaleReader
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public UserLocaleReader(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async Task<string> GetLocale(string userId, CancellationToken cancellationToken = default)
    {
        var locales = await GetLocales([userId], cancellationToken);

        return locales[userId];
    }

    public async Task<IReadOnlyDictionary<string, string>> GetLocales(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var values = await UnitOfWork.UserPreferences.GetGlobalValues(
            userIds,
            PreferenceKeys.AppearanceLocale,
            cancellationToken);
        var storedByUser = values
            .GroupBy(value => value.UserId)
            .ToDictionary(group => group.Key, group => ReadLocale(group.First()));

        return userIds
            .Distinct()
            .ToDictionary(userId => userId, userId => storedByUser.GetValueOrDefault(userId) ?? AppLocales.Default);
    }

    private static string? ReadLocale(UserPreferenceValue value)
    {
        var element = value.Value.RootElement;
        var isText = element.ValueKind == JsonValueKind.String;

        return isText ? AppLocales.Match(element.GetString()) : null;
    }
}
