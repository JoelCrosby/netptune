using Netptune.Transfer.Records;

namespace Netptune.Transfer.Catalog;

public sealed record ArchiveFieldBinding<TEntity>
{
    public required TransferField Field { get; init; }

    public required Func<TEntity, object?> Value { get; init; }
}

public interface IArchiveRecordDefinition
{
    string Key { get; }

    string FileName { get; }

    TransferRecordType RecordType { get; }
}

public sealed class ArchiveRecordDefinition<TEntity> : IArchiveRecordDefinition
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string FileName { get; init; }

    public required Func<TEntity, EntityRef> Ref { get; init; }

    public required IReadOnlyList<ArchiveFieldBinding<TEntity>> Bindings { get; init; }

    public TransferRecordType RecordType => BuildRecordType();

    public ExportRecord ToRecord(TEntity entity)
    {
        var values = new Dictionary<string, object?>(Bindings.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var binding in Bindings)
        {
            values[binding.Field.Key] = binding.Value(entity);
        }

        return new ExportRecord
        {
            Ref = Ref(entity),
            Values = values,
        };
    }

    private TransferRecordType BuildRecordType()
    {
        return new TransferRecordType
        {
            Key = Key,
            Name = Name,
            Fields = Bindings.Select(binding => binding.Field).ToList(),
        };
    }
}

public static class ArchiveField
{
    public static ArchiveFieldBinding<TEntity> Text<TEntity>(
        string recordType,
        string name,
        string title,
        Func<TEntity, object?> value,
        TransferValueType valueType = TransferValueType.Text)
    {
        return Build(recordType, name, title, value, valueType);
    }

    public static ArchiveFieldBinding<TEntity> Reference<TEntity>(
        string recordType,
        string name,
        string title,
        string refType,
        Func<TEntity, object?> value,
        bool isCollection = false)
    {
        return new ArchiveFieldBinding<TEntity>
        {
            Field = new TransferField
            {
                Key = $"{recordType}.{name}",
                Name = title,
                ValueType = TransferValueType.Ref,
                RefType = refType,
                IsCollection = isCollection,
                IsExportedByDefault = true,
            },
            Value = value,
        };
    }

    private static ArchiveFieldBinding<TEntity> Build<TEntity>(
        string recordType,
        string name,
        string title,
        Func<TEntity, object?> value,
        TransferValueType valueType)
    {
        return new ArchiveFieldBinding<TEntity>
        {
            Field = new TransferField
            {
                Key = $"{recordType}.{name}",
                Name = title,
                ValueType = valueType,
                IsExportedByDefault = true,
            },
            Value = value,
        };
    }
}
