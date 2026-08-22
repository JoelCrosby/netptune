using Dapper;

namespace Netptune.Query.Compilation;

public sealed class QueryParameterBag
{
    private readonly DynamicParameters Values = new();

    private int Counter;

    public string Add(object? value)
    {
        var name = $"q{Counter}";

        Counter++;

        Values.Add(name, value);

        return $"@{name}";
    }

    public DynamicParameters Build()
    {
        return Values;
    }
}
