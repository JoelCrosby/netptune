using Netptune.Entities.EntityMaps;

namespace Netptune.Repositories.Sql;

public static class ActivityEntryScripts
{
    public static readonly string UpsertActivityEntry = SqlScripts.UpsertActivityEntry.Replace(
        ActivityEntryEntityMap.OpenEntryIndexFilterToken,
        ActivityEntryEntityMap.OpenEntryIndexFilter);

    public static readonly string CloseExpiredActivityEntries = SqlScripts.CloseExpiredActivityEntries;
}
