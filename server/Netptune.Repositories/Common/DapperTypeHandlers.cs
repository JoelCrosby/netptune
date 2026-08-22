using System.Data;

using Dapper;

namespace Netptune.Repositories.Common;

public static class DapperTypeHandlers
{
    private static bool registered;

    public static void Register()
    {
        if (registered)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

        registered = true;
    }

    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value;
        }

        public override DateOnly Parse(object value)
        {
            return value is DateOnly date ? date : DateOnly.FromDateTime((DateTime)value);
        }
    }

    private sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value)
        {
            parameter.Value = value;
        }

        public override TimeOnly Parse(object value)
        {
            return value is TimeOnly time ? time : TimeOnly.FromDateTime((DateTime)value);
        }
    }
}
